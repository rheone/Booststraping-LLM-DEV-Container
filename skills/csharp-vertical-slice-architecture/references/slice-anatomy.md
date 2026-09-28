# Anatomy of a slice

This file describes the request/handler/response shape generically. Real codebases wire this shape
up with a specific handler-dispatch mechanism, DI container, or validation library — none of that
is required by the pattern itself, and none is named here. Substitute whatever your project
already uses; the shape below is what stays constant regardless.

## The three pieces

1. **Request (input DTO)** — a small, immutable type that represents exactly what this one use
   case needs as input. Not a shared "Order" domain entity, not a generic `Dictionary<string,
   object>` — a purpose-built shape scoped to this slice alone.
2. **Handler** — a class (or, in simpler slices, a single static method) with essentially one
   public entry point that takes the request and produces the response. This is where the slice's
   actual logic lives: validation orchestration, business rules, calls to persistence or other
   infrastructure.
3. **Response (output DTO)** — a small type representing exactly what this use case returns.
   Again, purpose-built to the slice, not a reused "OrderDto" shared across every slice that
   happens to touch orders.

```csharp
namespace MyApp.Features.Orders.CreateOrder;

// 1. Request — shaped for exactly this use case's inputs
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineItem> Items);

// 3. Response — shaped for exactly this use case's output
public sealed record CreateOrderResponse(Guid OrderId, decimal Total, DateTimeOffset CreatedAt);

// 2. Handler — one entry point, owns this use case's logic end to end
public sealed class CreateOrderHandler
{
    private readonly IOrderRepository _orders; // any persistence abstraction the project uses

    public CreateOrderHandler(IOrderRepository orders) => _orders = orders;

    public async Task<CreateOrderResponse> HandleAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        // validation, business rules, and persistence all live here —
        // not spread across a separate service class and repository class
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one item.");
        }

        var order = Order.Create(request.CustomerId, request.Items);
        await _orders.AddAsync(order, cancellationToken);

        return new CreateOrderResponse(order.Id, order.Total, order.CreatedAt);
    }
}
```

Everything a reader needs to understand "what happens when someone creates an order" is in this
one file. There is no separate `OrderService.CreateOrder(...)` to trace into, and no separate DTO
project holding `CreateOrderRequest` while the handler lives somewhere else.

## Why request/response are per-slice, not shared

A common early mistake when adopting VSA is to keep passing a shared `OrderDto` (or the domain
entity itself) in and out of every slice that touches orders, because that type already exists.
This quietly reintroduces layer-style coupling: every slice that uses the shared DTO now breaks
when any *other* slice's needs change that DTO's shape. A dedicated `CreateOrderRequest` can add a
field without touching `GetOrderByIdResponse` or `CancelOrderRequest` at all, because they are
different types. The duplication this creates across slices is intentional — see
[pitfalls.md](pitfalls.md) for when that duplication is actually the right tradeoff versus when it
signals a real shared concept worth extracting.

## How a slice connects to the outside world

A slice needs *something* to invoke its handler from an inbound entry point (an HTTP endpoint, a
message consumer, a scheduled job) and to return its response outward. Two structurally different
approaches are common, and both are compatible with the request/handler/response shape above:

- **Direct invocation** — the entry point (e.g., an HTTP endpoint) constructs or resolves the
  handler directly (via DI) and calls its method inline. No intermediary; the entry point and the
  handler are directly coupled by type.
- **Dispatch-mediated invocation** — the entry point hands the request object to a generic
  dispatch mechanism (resolved by the request's type) that locates and invokes the matching
  handler, decoupling the entry point from the handler's concrete type. This is the shape that
  handler-dispatch libraries in the .NET ecosystem commonly provide, but the *pattern* — "resolve a
  handler for this request type and invoke it" — is what matters, not any particular
  implementation of it.

Either approach satisfies VSA; the dispatch mechanism (or its absence) is an implementation detail
of how a slice is wired to the outside world, not part of the pattern's definition.

## Sizing a slice correctly

A slice should correspond to **one use case**, not one HTTP verb and not one entity. `CreateOrder`
and `CancelOrder` on the same `Order` entity are two separate slices, even though a layered CRUD
controller would put both methods on one `OrdersController`. Conversely, a single slice should not
try to handle multiple distinct use cases behind conditional branches inside one handler (e.g., a
`HandleAsync` that behaves completely differently depending on a `mode` field) — that is a sign the
slice boundary was drawn too coarsely and should be split. See
[pitfalls.md](pitfalls.md#inconsistent-slice-granularity) for the granularity failure modes in
both directions.
