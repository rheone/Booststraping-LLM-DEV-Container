# Entry points: slices behind every edge

A slice's handler does not care how the request arrived. The edge binds input from its transport, calls
the handler, and maps the outcome back. One handler can serve several edges (an HTTP endpoint and a CLI
command for the same use case); that is still one slice.

For every edge type below: the slice folder holds the edge mapping, the edge is registered from the area's
registration method, and the slice gets a test that drives the edge or the handler.

## HTTP: Minimal APIs

The default edge in this skill; see [slice-anatomy.md](slice-anatomy.md) for the full example.

- One endpoint per slice; one `MapGroup` per area for the route prefix, tags, and authorization.
- Return `Results<...>` from `TypedResults`.
- Test through `WebApplicationFactory` (HTTP) or by resolving the handler from the factory's services.

## HTTP: controllers

When the repo uses controllers, keep them, and make each one thin:

- One controller per slice (`CreateOrderController` with one action), or one controller per area whose
  actions each delegate to one handler. Follow the repo.
- The controller lives in the slice folder (`Features/Orders/CreateOrder/CreateOrderController.cs`). MVC
  discovers controllers anywhere in the assembly.
- Validation runs through MVC model validation; put attributes on record constructor parameters.
- Map outcomes with the same exhaustive switch, returning `ActionResult<T>` or `IActionResult`.

## Razor Pages

The page model is the edge, and the page folder is the slice (Layout C). Nest `Query`, `Command`,
validators, and handlers in the page's `.cshtml.cs`, as ContosoUniversity does. `OnGetAsync` sends the
query; `OnPostAsync` sends the command and redirects on success. A page filter can own validation and the
transaction for every page.

## Blazor

No primary source shows Blazor components organized as slices; treat this as community practice.

- Server-side (Blazor Server, interactive server, or prerendered): a routable component per use case in
  the slice folder (`Features/Orders/CreateOrder/CreateOrderPage.razor`). The component injects the
  handler and calls it directly.
- WebAssembly: the component runs in the browser and cannot call server handlers. Put the HTTP endpoint
  slice on the server, and keep the client component in a client project under a mirrored folder
  (`Client/Features/Orders/CreateOrder/`). Share only the request and response records, through a
  contracts project.
- Keep component state and UI logic in the component; keep use-case logic in the handler.

## Message and event consumers

A consumer is an edge. Its slice holds the message record (or references the publisher's contract), the
handler, and the consumer adapter for the repo's messaging library.

```text
Features/Billing/RefundOnOrderCancelled/
├── RefundOnOrderCancelledConsumer.cs   # adapter: library consumer interface -> handler
├── RefundOnOrderCancelledHandler.cs
└── RefundOnOrderCancelledTests.cs      # (in the test project, mirrored)
```

- Name the slice for what it does in reaction (`RefundOnOrderCancelled`), not for the message alone.
- Map outcomes: success acknowledges; a transient failure (timeout) lets the library retry; a permanent
  failure (the referenced order does not exist) acknowledges and logs, or dead-letters, per the repo's
  policy. Do not retry an outcome that will never change.
- Make the handler idempotent; see [messaging-and-consistency.md](messaging-and-consistency.md).
- Open a DI scope per message so the ORM session and unit of work are per message.
- Test the handler with a real database; test the adapter only for mapping.

## Background and scheduled jobs

A job is an edge triggered by time instead of a request.

```csharp
internal sealed class ExpireStaleCartsJob(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<ExpireStaleCartsJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(5), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            AsyncServiceScope scope = scopes.CreateAsyncScope();
            await using ConfiguredAsyncDisposable scopeLifetime = scope.ConfigureAwait(false);
            ExpireStaleCartsHandler handler = scope.ServiceProvider.GetRequiredService<ExpireStaleCartsHandler>();
            int expired = await handler.HandleAsync(stoppingToken).ConfigureAwait(false);
            logger.LogInformation("Expired {Count} stale carts", expired);
        }
    }
}
```

- The job class and its handler live in the slice folder (`Features/Carts/ExpireStaleCarts/`).
- Create a scope per run; never inject a scoped `DbContext` or `ISession` into a singleton hosted
  service.
- When the repo uses a scheduler library, the library's job class is the edge and calls the handler.
- Test the handler directly; test the schedule only in the scheduler's own configuration tests.

## gRPC

The generated service base class is the edge. Keep one service class per area, and make each RPC method
a thin adapter to one slice's handler:

```csharp
internal sealed class OrdersGrpcService(IServiceProvider services) : Orders.OrdersBase
{
    public override async Task<CreateOrderReply> CreateOrder(CreateOrderMessage request, ServerCallContext context)
    {
        CreateOrderHandler handler = services.GetRequiredService<CreateOrderHandler>();
        CreateOrderResult result = await handler.HandleAsync(request.ToRequest(), context.CancellationToken)
            .ConfigureAwait(false);
        return result switch
        {
            OrderCreated created => new CreateOrderReply { OrderId = created.OrderId.ToString() },
            CustomerMissing missing => throw new RpcException(new Status(StatusCode.NotFound, $"Customer {missing.CustomerId}")),
            ProductsUnavailable unavailable => throw new RpcException(new Status(StatusCode.FailedPrecondition, string.Join(",", unavailable.Skus))),
            null => throw new UnreachableException(),
        };
    }
}
```

gRPC signals non-OK status codes through `RpcException`; that is the transport's error channel, not
control flow inside the use case. The handler still returns outcomes. Keep `.proto` files per area
(`Protos/orders.proto`), and the `ToRequest()` mapping in the slice folder.

## CLI commands

A console app can share slices with the API (Layout D). Each command is an edge that parses arguments,
calls the handler, and maps the outcome to output and an exit code.

```csharp
Command archive = new("archive-order", "Archive one order.");
Argument<Guid> id = new("id");
archive.Arguments.Add(id);
archive.SetAction(async (ParseResult parse, CancellationToken ct) =>
{
    AsyncServiceScope scope = host.Services.CreateAsyncScope();
    await using ConfiguredAsyncDisposable scopeLifetime = scope.ConfigureAwait(false);
    ArchiveOrderHandler handler = scope.ServiceProvider.GetRequiredService<ArchiveOrderHandler>();
    ArchiveOrderResult result = await handler.HandleAsync(parse.GetValue(id), ct).ConfigureAwait(false);
    return result switch
    {
        OrderArchived => 0,
        OrderNotFound => 2,
        null => 1,
    };
});
```

The sample uses System.CommandLine 2.x; use the repo's CLI library when it has one. Map each outcome to a
distinct, documented exit code.

## One handler, several edges

When the same use case has an HTTP endpoint and a consumer (for example, "import product" from an upload
and from a message), keep one slice folder with one handler and two edge files. When the two edges need
different rules, they are two slices.
