# Decorators and interceptors

Both wrap a component with cross-cutting behavior (logging, caching, validation, retries) without
modifying the component itself — the difference is *how* the wrapping happens.

## `RegisterDecorator` — plain-object decoration

`RegisterDecorator` wraps a resolved service with another implementation of the *same* service
interface, calling the decorator's constructor with the inner instance injected as a parameter of
that same interface type. No dynamic proxying, no interception library — just an ordinary object
wrapping another ordinary object, fully debuggable and steppable.

```csharp
public interface IOrderService { void Place(Order order); }

public class OrderService : IOrderService { /* real implementation */ }

public class LoggingOrderServiceDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger _logger;
    public LoggingOrderServiceDecorator(IOrderService inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public void Place(Order order)
    {
        _logger.LogInformation("Placing order {OrderId}", order.Id);
        _inner.Place(order);
    }
}

builder.RegisterType<OrderService>().As<IOrderService>();
builder.RegisterDecorator<LoggingOrderServiceDecorator, IOrderService>();
// or, generically: builder.RegisterDecorator<IOrderService>((ctx, parameters, inner) => new LoggingOrderServiceDecorator(inner, ctx.Resolve<ILogger>()));
```

Multiple decorators registered for the same service compose in registration order, each wrapping
the previous — a form of the decorator pattern the container assembles for you instead of you
hand-nesting `new` calls.

Open-generic decorators (`RegisterGenericDecorator`) apply the same idea across a whole family of
closed generic services, e.g. decorating every `IHandler<T>` with a common
`LoggingHandlerDecorator<T>`.

## Interceptors — dynamic proxying (`Autofac.Extras.DynamicProxy`)

Interceptors use runtime proxy generation (via Castle DynamicProxy, brought in through the separate
`Autofac.Extras.DynamicProxy` package) to inject behavior around *every* method call without
writing a decorator class per interface. Useful when the same cross-cutting concern (e.g. logging
every call, retrying every call) needs to apply generically across many members/types without
hand-writing a forwarding method per member.

```csharp
public class LoggingInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        Console.WriteLine($"Calling {invocation.Method.Name}");
        invocation.Proceed();
        Console.WriteLine($"Called {invocation.Method.Name}");
    }
}

builder.RegisterType<LoggingInterceptor>();
builder.RegisterType<OrderService>()
    .As<IOrderService>()
    .EnableInterfaceInterceptors()          // proxy the interface (requires all members virtual-equivalent, i.e. interface members)
    .InterceptedBy(typeof(LoggingInterceptor));
```

`EnableClassInterceptors()` is the alternative for intercepting a concrete class directly (members
being intercepted must be `virtual`, since Castle DynamicProxy subclasses the type).

## Choosing between them

- Prefer **decorators** by default: they're plain C#, require no extra package, are trivially
  unit-testable on their own, and keep the "what runs, in what order" fully visible in registration
  code and stack traces.
- Reach for **interceptors** only when the cross-cutting concern is genuinely generic across many
  members/types and hand-writing a decorator per interface would be pure boilerplate — accept the
  cost of an extra dependency (`Autofac.Extras.DynamicProxy` + Castle.Core) and slightly murkier
  stack traces (calls go through a generated proxy type) in exchange for not repeating the same
  forwarding logic dozens of times.
