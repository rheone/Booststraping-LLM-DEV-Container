# Hand-Rolled Dispatcher

A request/handler dispatcher (the second intent in
[core-concept-and-two-intents.md](core-concept-and-two-intents.md)) needs three things: a request
marker, a handler contract keyed to that request, and a mediator that finds the right handler for
whatever request it's given. This file builds one without a third-party dependency and without the
`dynamic` cast shown in the quick start.

## The contracts

```csharp
public interface IRequest<TResponse> { }

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    TResponse Handle(TRequest request);
}
```

`IRequestHandler<TRequest, TResponse>` is contravariant in `TRequest` (`in`) so a handler for a base
request type can serve a request declared as that base type at the call site, if that's ever useful
— most codebases never need this and can drop the `in`.

## A dispatcher without reflection at call time

Reflection-based dispatch (`MakeGenericType` + `dynamic`, shown in the quick start) works but pays a
reflection cost on every single `Send` call and loses compile-time checking on the handler
invocation. An adapter-per-request-type avoids both:

```csharp
public interface IMediator
{
    TResponse Send<TResponse>(IRequest<TResponse> request);
}

internal interface IRequestHandlerWrapper<TResponse>
{
    TResponse Handle(IRequest<TResponse> request, IServiceProvider services);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public TResponse Handle(IRequest<TResponse> request, IServiceProvider services)
    {
        var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        return handler.Handle((TRequest)request);
    }
}

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _services;
    private static readonly ConcurrentDictionary<Type, object> WrapperCache = new();

    public Mediator(IServiceProvider services) => _services = services;

    public TResponse Send<TResponse>(IRequest<TResponse> request)
    {
        Type requestType = request.GetType();

        var wrapper = (IRequestHandlerWrapper<TResponse>)WrapperCache.GetOrAdd(requestType, type =>
        {
            Type wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(type, typeof(TResponse));
            return Activator.CreateInstance(wrapperType)!;
        });

        return wrapper.Handle(request, _services);
    }
}
```

The reflection cost (`MakeGenericType`/`Activator.CreateInstance`) happens once per distinct request
type, the first time it's sent, because `WrapperCache` remembers the wrapper instance after that.
Every subsequent `Send` for the same request type is a dictionary lookup plus a directly typed
method call — no `dynamic`, no per-call reflection.

## Registering handlers

Each handler registers as its own closed generic interface:

```csharp
services.AddScoped<IRequestHandler<GetOrderQuery, OrderDto>, GetOrderQueryHandler>();
services.AddScoped<IRequestHandler<CreateOrderCommand, int>, CreateOrderCommandHandler>();
services.AddScoped<IMediator, Mediator>();
```

A project with many handlers typically automates this registration with an assembly scan (find
every closed `IRequestHandler<,>` implementation and register it) rather than listing each one by
hand — the scan is an implementation detail of composition-root setup, not part of the dispatcher
itself.

## Void-returning requests

A request with no meaningful response still needs a `TResponse` to satisfy the generic contract.
`Unit` — a type with exactly one value — fills that role without overloading the interface:

```csharp
public readonly struct Unit
{
    public static readonly Unit Value = default;
}

public sealed class DeleteOrderCommand : IRequest<Unit>
{
    public int OrderId { get; init; }
}

public sealed class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, Unit>
{
    public Unit Handle(DeleteOrderCommand request)
    {
        // perform the delete
        return Unit.Value;
    }
}
```
