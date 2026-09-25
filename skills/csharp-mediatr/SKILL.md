---
name: csharp-mediatr
description: Guidance on the MediatR NuGet package (verified current release 14.2.0) for in-process mediator/CQRS-style messaging in C#/.NET — IMediator/ISender/IPublisher, IRequest<T>/IRequestHandler<T,R>, INotification/INotificationHandler, AddMediatR registration and assembly scanning, IPipelineBehavior<TRequest,TResponse> pipeline behaviors and their ordering, notification publish strategies (sequential ForeachAwaitPublisher vs parallel TaskWhenAllPublisher), IStreamRequest/IStreamRequestHandler streaming, IRequestExceptionHandler/IRequestExceptionAction exception handling, the CQRS-as-convention pattern, and unit/integration testing of handlers and behaviors. Also documents MediatR's current dual-license model (RPL 1.5 community tier / paid commercial subscription as of v13.0+) since this affects whether a project can adopt it. Use when writing or reviewing MediatR requests/handlers/behaviors, deciding whether MediatR is appropriate for a project given its licensing, debugging pipeline ordering or notification fan-out, or setting up AddMediatR registration.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# MediatR

Guidance on MediatR, the in-process mediator library for decoupling request senders from request
handlers in .NET. Organized by task/category, not by C# or MediatR version — the core API surface
(`IRequest<T>`, `IRequestHandler<T,R>`, `INotification`) has been stable across major versions;
each reference file notes a version-introduced fact inline where it matters (e.g. the v13.0
licensing change, the v12.0 notification-publisher extension point).

## Read this first: licensing

MediatR is **no longer unconditionally free** for commercial use. As of **v13.0 (2025)**, new
versions ship under a dual license: free (Reciprocal Public License 1.5) for individuals, small
companies (under $5M annual gross revenue and under $10M outside capital raised), non-profits,
education, and non-production use; a **paid commercial subscription** (tiered by developer count,
via Lucky Penny Software) for everyone else. **MediatR 12.5.0 is the last release under the
original Apache 2.0 license** and remains usable under that license forever, unaffected
retroactively. **Read [references/licensing.md](references/licensing.md) before recommending
MediatR for a project** — this determines whether adoption is even viable, independent of any
technical merits below.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Deciding if MediatR fits, or checking license obligations before adding the package | [references/licensing.md](references/licensing.md) |
| Learning `IRequest<T>`/`IRequestHandler<T,R>`, `INotification`/`INotificationHandler`, `IMediator`/`ISender`/`IPublisher` | [references/core-concepts.md](references/core-concepts.md) |
| Wiring up `AddMediatR`, assembly scanning, or deciding handler service lifetime | [references/registration.md](references/registration.md) |
| Writing cross-cutting logic (logging, validation, transactions) that wraps every request | [references/pipeline-behaviors.md](references/pipeline-behaviors.md) |
| Publishing an `INotification` to multiple handlers, or choosing sequential vs. parallel fan-out | [references/notifications-and-publishing.md](references/notifications-and-publishing.md) |
| Deciding whether a use case should be a request/response or a fire-and-forget notification | [references/request-response-vs-notifications.md](references/request-response-vs-notifications.md) |
| Streaming a sequence of results back from a handler (`IAsyncEnumerable`) | [references/streaming.md](references/streaming.md) |
| Centralizing exception translation/handling across handlers | [references/exception-handling.md](references/exception-handling.md) |
| Structuring commands vs. queries with MediatR (CQRS-as-convention) | [references/cqrs-pattern.md](references/cqrs-pattern.md) |
| Deciding whether MediatR is overkill here, or debugging pipeline/indirection pain | [references/pitfalls-and-tradeoffs.md](references/pitfalls-and-tradeoffs.md) |
| Unit testing a handler, a behavior in isolation, or integration-testing the whole pipeline | [references/testing.md](references/testing.md) |

## Quick start

A minimal command, handler, and registration, current API (v13+ through the verified current
14.2.0):

```csharp
public sealed record CreateOrder(string CustomerId, decimal Total) : IRequest<Guid>;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrder, Guid>
{
    public async Task<Guid> Handle(CreateOrder request, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        // persist order...
        return orderId;
    }
}

// Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderHandler).Assembly);
    cfg.LicenseKey = builder.Configuration["MediatR:LicenseKey"]; // or MEDIATR_LICENSE_KEY env var
});

// Usage (inject ISender or IMediator)
public sealed class OrdersController(ISender sender)
{
    public Task<Guid> Post(CreateOrder command) => sender.Send(command);
}
```

The single most common miss: registering a package-wide `IPipelineBehavior<,>` (validation,
logging) and never checking the **order** behaviors run in — order follows DI **registration**
order, not alphabetical or attribute-based order. See
[references/pipeline-behaviors.md](references/pipeline-behaviors.md).

## Out of scope

- Other in-process mediator/messaging libraries (Mediator.Net, Wolverine, Brighter) — not
  documented here; this skill is MediatR-only.
- Distributed/out-of-process messaging (message brokers, service buses, MassTransit, NServiceBus)
  — MediatR is strictly in-process; nothing here applies to cross-process messaging.
- ASP.NET Core-specific concerns beyond registering MediatR in the DI container (minimal API
  endpoint shaping, model binding, OpenAPI) — out of scope beyond the registration mechanics in
  [references/registration.md](references/registration.md).
