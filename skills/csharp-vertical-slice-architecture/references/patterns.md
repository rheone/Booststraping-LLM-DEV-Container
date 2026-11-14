# Patterns: dispatch, CQRS, pipelines, REPR

## Dispatch: how an edge reaches its handler

Follow the repo's existing style. In a new project, present these options with their trade-offs and ask
the user. If the repo already references a mediator library, keep it.

| Option | Shape | Strengths | Costs |
| --- | --- | --- | --- |
| **Direct injection** | The endpoint takes the handler class as a parameter; or the handler is a static method the endpoint calls | No indirection; "go to definition" works; AOT-safe; no package | Cross-cutting steps per edge type (endpoint filters for HTTP, wrappers for consumers) |
| **Hand-rolled mediator** | A small `ISender` that resolves `IHandler<TRequest, TResult>` from DI and wraps it in pipeline steps | One pipeline for every edge type (HTTP, messages, jobs, CLI); no license | You own ~50–100 lines; generic DI resolution needs care under AOT |
| **Source-generated mediator** | For example `Mediator` (martinothamar, MIT): handlers found at compile time | Uniform pipeline, AOT-safe, fast | A package; it does not support generic requests or notifications |
| **Reflection mediator** | For example MediatR | Mature, large ecosystem | v13+ is dual-licensed (RPL-1.5 or commercial; a free Community tier exists under $5M revenue); dispatch uses runtime reflection, so AOT and trimming are not claimed |
| **Framework with built-in handlers** | For example Wolverine (HTTP + messaging), FastEndpoints (REPR endpoints) | Handlers and edges in one model | The framework shapes the whole app |

Facts behind the table:

- MediatR licensing applies from v13.0; earlier versions keep their original license. The current
  release is 15.0.0. (<https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/>,
  <https://mediatr.io/>)
- `Mediator` claims "Full Native AOT support without reflection or runtime code generation" and
  "doesn't support generic requests/notifications" (<https://github.com/martinothamar/Mediator>).
- Bogard uses a mediator "only on the edges": a `Send` happens in a controller, page, or consumer, never
  inside a handler (<https://jimmybogard.com/you-probably-dont-need-to-worry-about-mediatr/>).

### Direct injection (Minimal APIs)

The `CreateOrderEndpoint` in [slice-anatomy.md](slice-anatomy.md) is direct injection: the endpoint
method takes `CreateOrderHandler` as a parameter and Minimal APIs resolve it from DI. A smaller slice can
use a static handler method and inject its dependencies straight into the endpoint:

```csharp
internal static class ArchiveOrder
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder orders) =>
        orders.MapPost("/{id:guid}/archive", HandleAsync);

    private static async Task<Results<NoContent, NotFound>> HandleAsync(
        Guid id, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        int updated = await db.Orders
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ArchivedAt, clock.GetUtcNow()), ct)
            .ConfigureAwait(false);
        return updated == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
```

Register handlers explicitly (`services.AddScoped<CreateOrderHandler>()`), or with an area-level
`AddOrdersFeature()` extension that lists them. Explicit registration is trim-safe.

### Hand-rolled mediator

Use it when several edge types (HTTP, consumers, jobs) must share one pipeline. Keep it small:

```csharp
public interface IRequest<TResult>;

public interface IHandler<in TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> HandleAsync(TRequest request, CancellationToken ct = default);
}

public interface IPipelineStep<TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> HandleAsync(TRequest request, Func<Task<TResult>> next, CancellationToken ct = default);
}

public sealed class Sender(IServiceProvider services)
{
    public Task<TResult> SendAsync<TRequest, TResult>(TRequest request, CancellationToken ct = default)
        where TRequest : IRequest<TResult>
    {
        IHandler<TRequest, TResult> handler = services.GetRequiredService<IHandler<TRequest, TResult>>();
        Func<Task<TResult>> invoke = () => handler.HandleAsync(request, ct);

        // Steps registered first run outermost.
        foreach (IPipelineStep<TRequest, TResult> step in services.GetServices<IPipelineStep<TRequest, TResult>>().Reverse())
        {
            Func<Task<TResult>> next = invoke;
            invoke = () => step.HandleAsync(request, next, ct);
        }

        return invoke();
    }
}
```

Taking both type arguments in `SendAsync` avoids runtime `MakeGenericType`, so the dispatcher is
trim-safe when handlers and steps are registered with closed generic types. Open-generic step
registration (`services.AddScoped(typeof(IPipelineStep<,>), typeof(LoggingStep<,>))`) works in the default
container; check AOT warnings if you publish with Native AOT.

## CQRS

Command Query Responsibility Segregation separates the code that changes state from the code that reads
it. Slicing gives you the simplest level for free: each slice is either a command or a query.

| Level | What changes | When to go there |
| --- | --- | --- |
| 1. Separate handlers, same model and store | Commands load entities and save; queries project with no tracking | Default for VSA |
| 2. Separate read models in the same store | Queries read views, raw SQL, or a micro-ORM | Read performance or shape diverges from the write model |
| 3. Separate read store | Commands publish events; projections update a read database | Read scale or shape demands it; you accept eventual consistency |

Microsoft warns that CQRS with separate models does not suit simple domains or CRUD UIs
(<https://learn.microsoft.com/azure/architecture/patterns/cqrs>). Levels 2 and 3 are per slice: one slow
query can move to level 2 without touching the others.

Rules:

- A command returns the minimum the caller needs (an ID, a version, an outcome), not the full entity.
- A query never changes state. Use no-tracking reads.
- Command and query naming: `CreateOrder`, `GetOrderById`. Suffixes `Command`/`Query` are optional; follow
  the repo.

## Pipelines, filters, and decorators

Cross-cutting steps wrap the handler. Pick the mechanism by scope:

| Mechanism | Scope | Notes |
| --- | --- | --- |
| Middleware | Whole app, every request | Exception handling, CORS, rate limiting, authentication |
| Endpoint filter (`IEndpointFilter`) | One endpoint, or every endpoint in a `MapGroup` | Before-`next` code runs in registration order (FIFO); after-`next` code in reverse (FILO) |
| Route group conventions | A whole area | `RequireAuthorization`, `WithTags`, `CacheOutput`, `AddEndpointFilter` |
| MVC / page filters | Controllers, Razor Pages | ContosoUniversity commits transactions in a page filter |
| Mediator pipeline step | Every request through the mediator, any edge type | Microsoft: "explicit and applied only in the context of the mediator, so debugging is much more predictable" |
| Decorator | One handler type | The default Microsoft DI container has no decorator registration API; register the decorator manually with a factory, or use the repo's container |

Sources: <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters>,
<https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api>

Steps stay generic: a step works for any request type and knows no slice. A slice opts in or out through
metadata (`.AddEndpointFilter<T>()`, `.DisableValidation()`, a marker interface), not through a type
check inside the step.

## REPR

Request–Endpoint–Response: an API endpoint is one request type, one endpoint, one response type, with no
multi-action controllers (<https://deviq.com/design-patterns/repr-design-pattern>). REPR is the HTTP-edge
form of a slice. A Minimal API endpoint per slice, or a FastEndpoints endpoint class, gives you REPR.

## Notifications

A notification (in-process event) has zero or more handlers. Use it for side effects owned by another
area. Domain events and integration events are covered in
[messaging-and-consistency.md](messaging-and-consistency.md).

## Abstractions: keep and avoid

Keep, because they already exist or earn their place:

- The ORM's unit of work (`DbContext`, NHibernate `ISession`).
- `TimeProvider` for time, `IHttpClientFactory` / typed clients for HTTP, `ILogger<T>`, `IOptions<T>`.
- A seam for an external system you must fake in tests (payment gateway, email).
- A dispatcher, only when a shared pipeline across edge types needs it.

Avoid by default, and add only for a named reason:

- `IXxxService` with one implementation that no test fakes.
- A generic `IRepository<T>` over an ORM (see [data-access.md](data-access.md)).
- A mapping layer for shapes that are already one-to-one.
- A base handler class for all slices. A base class for one area's handlers is acceptable when it removes
  real duplication (Bogard, 2016).
