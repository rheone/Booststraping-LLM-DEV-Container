# Generic Mediator

The request/handler contracts in
[hand-rolled-dispatcher.md](hand-rolled-dispatcher.md) are already generic in the request and
response types — that genericity is what lets one `IMediator.Send<TResponse>` method serve every
request type a project defines, present or future, without the interface itself changing. This file
covers the generic shape as its own concern: what each type parameter buys you, and a
notification-style variant for requests with more than one handler.

## The generic request/handler pair

```csharp
public interface IRequest<TResponse> { }

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    TResponse Handle(TRequest request);
}

public interface IMediator
{
    TResponse Send<TResponse>(IRequest<TResponse> request);
}
```

`TRequest` ties a handler to exactly one request type at compile time — a project cannot
accidentally register `CreateOrderCommandHandler` against `DeleteOrderCommand`, because the
constraint `where TRequest : IRequest<TResponse>` combined with the DI container's closed-generic
registration key (`IRequestHandler<CreateOrderCommand, int>`) makes that combination simply not
exist as a registrable type. `TResponse` is what lets `Send` return the correct concrete type at
each call site without a cast:

```csharp
OrderDto order = mediator.Send(new GetOrderQuery(id));       // TResponse inferred as OrderDto
int newOrderId = mediator.Send(new CreateOrderCommand(...)); // TResponse inferred as int
```

## Generic notifications (one request, many handlers)

`Send` assumes exactly one handler per request type — the DI container resolves a single
`IRequestHandler<TRequest, TResponse>`, and resolving zero or more than one is a registration error.
Some events genuinely need zero-or-more independent handlers (an `OrderPlaced` notification that an
email sender, an analytics recorder, and an inventory updater all react to independently). That's a
different contract:

```csharp
public interface INotification { }

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task Handle(TNotification notification);
}

public interface IMediator
{
    TResponse Send<TResponse>(IRequest<TResponse> request);
    Task Publish<TNotification>(TNotification notification) where TNotification : INotification;
}
```

```csharp
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _services;
    public Mediator(IServiceProvider services) => _services = services;

    public TResponse Send<TResponse>(IRequest<TResponse> request) => /* as in hand-rolled-dispatcher.md */ default!;

    public async Task Publish<TNotification>(TNotification notification) where TNotification : INotification
    {
        var handlers = _services.GetServices<INotificationHandler<TNotification>>();
        foreach (var handler in handlers)
        {
            await handler.Handle(notification);
        }
    }
}
```

Registering more than one `INotificationHandler<OrderPlaced>` implementation is not an error the way
it would be for `IRequestHandler<,>` — `GetServices` resolves all of them, and `Publish` runs every
one. Keeping `Send` (exactly one handler, returns a value) and `Publish` (zero or more handlers, no
meaningful combined return value) as two separate methods on the same generic `IMediator` keeps
each contract's cardinality explicit rather than overloading one method to mean both.

## Constraining the response type

A project that wants every request to declare a specific kind of response (a `Result<T>` wrapper
carrying success/failure, for example) constrains `TResponse` at the interface level instead of
leaving it fully open:

```csharp
public interface IRequest<TResponse> { }

public interface IMediator
{
    Task<Result<TResponse>> Send<TResponse>(IRequest<Result<TResponse>> request);
}
```

This is a project-specific narrowing, not a requirement of the pattern itself — the fully open
`TResponse` shown above is the more broadly reusable starting point.
