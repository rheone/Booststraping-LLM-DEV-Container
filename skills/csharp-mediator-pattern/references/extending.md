# Extending the Mediator

## Adding a new request/handler pair

The generic `IMediator.Send<TResponse>` interface never changes when a new request type is added —
that's the point of keying dispatch by the request's own type instead of by a named method per
request. Adding capability is purely additive:

```csharp
public sealed class CancelOrderCommand : IRequest<Unit>
{
    public int OrderId { get; init; }
}

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Unit>
{
    public Unit Handle(CancelOrderCommand request)
    {
        // cancel the order
        return Unit.Value;
    }
}
```

```csharp
services.AddScoped<IRequestHandler<CancelOrderCommand, Unit>, CancelOrderCommandHandler>();
```

No existing caller, handler, or dispatcher code changes. If handler registration is automated via
an assembly scan (see [hand-rolled-dispatcher.md](hand-rolled-dispatcher.md)), even the registration
line above is unnecessary — the new handler is picked up the next time the scan runs.

## Adding a second handler for the same request (fan-out)

`Send` assumes exactly one handler per request/response pair — deliberately, since `Send` returns a
single value and "which of two conflicting return values wins" has no good general answer. A
request that legitimately needs more than one independent reaction is a notification, not a
request/response — see [generic-mediator.md](generic-mediator.md)'s `Publish`/`INotificationHandler`
shape, which supports registering as many handlers as needed for the same notification type without
any of them returning a value the caller depends on.

## Adding a pipeline behavior

Introducing cross-cutting behavior (logging, validation) around every `Send` call without touching
any existing handler is the scenario
[when-to-hand-roll-vs-framework.md](when-to-hand-roll-vs-framework.md) covers in full, including the
`IPipelineBehavior<TRequest, TResponse>` shape — each new behavior registers alongside existing ones
and wraps the same handler-invocation call, without any handler needing to know a pipeline exists.

## Swapping the resolution strategy

Moving from a dictionary-based mediator (see
[handler-resolution-strategies.md](handler-resolution-strategies.md)) to DI-container resolution, or
the reverse, only changes the body of `Send` and how handlers get registered — the `IMediator`,
`IRequest<TResponse>`, and `IRequestHandler<TRequest, TResponse>` contracts stay identical, so every
caller and every handler written against those contracts keeps compiling and behaving the same way
regardless of which resolution strategy backs them.

## What breaks existing callers

Two changes are genuinely breaking and need every call site checked: changing an existing request's
`TResponse` type (every `Send` call site that captured the old return type as a specific type now
needs updating), and removing a request type or its handler registration entirely while callers
still construct and send that request. Adding new requests, new handlers, new notification
subscribers, or new pipeline behaviors are never breaking on their own.
