# Core Concepts

MediatR implements the mediator pattern in-process: instead of a caller depending directly on a
handler class, it depends on `IMediator`/`ISender`/`IPublisher` and hands the mediator a message
object. The mediator resolves and invokes the right handler(s) via DI. Nothing here crosses a
process boundary — this is not a message queue or service bus.

## Requests: one handler, optionally a response

`IRequest<TResponse>` marks a message that expects exactly **one** handler and returns a value.
`IRequest` (no type parameter) is the fire-and-forget-with-no-return-value shorthand, equivalent
to `IRequest<Unit>`... in older versions; on current MediatR, `IRequest` without a response type
still exists as a convenience but request/response is the primary, recommended shape.

```csharp
public sealed record GetOrderById(Guid OrderId) : IRequest<OrderDto?>;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderById, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderById request, CancellationToken cancellationToken)
    {
        // fetch and map...
        return orderDto;
    }
}
```

**Exactly one handler must be registered per request type.** Registering zero handlers throws at
dispatch time; registering more than one handler for the same `IRequest<T>` is a DI registration
error MediatR will surface (it does not silently pick one) — this is the key difference from
notifications, which allow (and expect) many handlers.

## Notifications: zero-to-many handlers, no response

`INotification` marks an event-style message: any number of handlers (including zero) can
subscribe, and none of them return a value back to the publisher.

```csharp
public sealed record OrderCreated(Guid OrderId, string CustomerId) : INotification;

public sealed class SendOrderConfirmationEmail : INotificationHandler<OrderCreated>
{
    public Task Handle(OrderCreated notification, CancellationToken cancellationToken)
    {
        // send email...
        return Task.CompletedTask;
    }
}

public sealed class UpdateInventoryProjection : INotificationHandler<OrderCreated>
{
    public Task Handle(OrderCreated notification, CancellationToken cancellationToken)
    {
        // update read model...
        return Task.CompletedTask;
    }
}
```

Both handlers above run when `OrderCreated` is published — no coordination or priority ordering
is implied by default beyond registration order in the default sequential publisher (see
[notifications-and-publishing.md](notifications-and-publishing.md)).

## The dispatch interfaces: IMediator, ISender, IPublisher

- **`ISender`** — exposes only `Send(...)` (dispatch a request to its single handler) and
  `CreateStream(...)` (dispatch a stream request). Depend on this when a class only ever sends
  requests and never publishes notifications — it's the narrower, more honest dependency.
- **`IPublisher`** — exposes only `Publish(...)` (fan out a notification to zero-or-more
  handlers). Depend on this when a class only ever raises events.
- **`IMediator`** — extends both `ISender` and `IPublisher`; use it when a class genuinely needs
  both capabilities. Reaching for `IMediator` everywhere by default, rather than the narrower
  `ISender`/`IPublisher`, is a common habit worth resisting: a constructor asking only for
  `ISender` documents "this class sends commands/queries" more precisely than one asking for the
  combined interface.

```csharp
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateOrder command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}
```

All three interfaces, and the concrete `Mediator` class that implements them, are resolved from
the DI container once `AddMediatR` has registered them — see
[registration.md](registration.md).

## Handler resolution and cardinality summary

| Message type | Handler interface | Handler count | Return value |
| --- | --- | --- | --- |
| `IRequest<TResponse>` | `IRequestHandler<TRequest, TResponse>` | Exactly 1 | Yes |
| `IRequest` (no response) | `IRequestHandler<TRequest>` | Exactly 1 | No |
| `INotification` | `INotificationHandler<TNotification>` | 0 or more | No |
| `IStreamRequest<TResponse>` | `IStreamRequestHandler<TRequest, TResponse>` | Exactly 1 | `IAsyncEnumerable<TResponse>` (see [streaming.md](streaming.md)) |

## Handler resolution failures are your responsibility to design around

Because handler resolution happens at `Send`/`Publish` time (not compile time), a request type
with no registered handler compiles cleanly and throws only when actually dispatched. There's no
compiler safety net here the way there is with a direct method call — see
[pitfalls-and-tradeoffs.md](pitfalls-and-tradeoffs.md) for the broader indirection tradeoff this
implies for traceability and refactoring.
