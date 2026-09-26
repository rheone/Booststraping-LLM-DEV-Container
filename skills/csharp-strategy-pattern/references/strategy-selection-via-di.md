# Choosing a Strategy via DI Registration

A dependency injection container can select which strategy implementation a context receives, so
the choice of algorithm becomes a registration-time decision instead of a call-site decision spread
across the codebase.

## Registering a single strategy

The simplest case: one strategy implementation is active for the whole application, and the
container wires it to the interface once at startup.

```csharp
services.AddSingleton<IShippingCostStrategy, StandardShippingStrategy>();
services.AddScoped<OrderProcessor>();
```

`OrderProcessor` still depends on `IShippingCostStrategy` through its constructor; nothing about the
context changes when the registration changes. Swapping `StandardShippingStrategy` for
`ExpressShippingStrategy` application-wide is a one-line change at the composition root.

## Selecting a strategy at runtime by key

When the strategy to use depends on something known only at request time (a customer's shipping
tier, a feature flag, a tenant setting), register every implementation and resolve the right one
through a small selector rather than branching in the context itself:

```csharp
services.AddKeyedSingleton<IShippingCostStrategy, StandardShippingStrategy>("standard");
services.AddKeyedSingleton<IShippingCostStrategy, ExpressShippingStrategy>("express");
services.AddKeyedSingleton<IShippingCostStrategy, FreeShippingStrategy>("free");
```

```csharp
public sealed class ShippingStrategySelector
{
    private readonly IServiceProvider _serviceProvider;

    public ShippingStrategySelector(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IShippingCostStrategy Select(string tier) =>
        _serviceProvider.GetRequiredKeyedService<IShippingCostStrategy>(tier);
}
```

The context depends on the selector, not on `IServiceProvider` directly — resolving from the raw
container inside business logic (the service locator shape) defeats constructor injection's ability
to make a class's dependencies visible from its constructor signature alone. The selector is the one
place that shape is acceptable, because selecting-by-key genuinely is its entire job.

## Selecting a strategy by injecting all implementations

An alternative to keyed resolution: inject every registered strategy as a collection and let the
consumer pick by an identifying property on each implementation.

```csharp
services.AddSingleton<IShippingCostStrategy, StandardShippingStrategy>();
services.AddSingleton<IShippingCostStrategy, ExpressShippingStrategy>();
services.AddSingleton<IShippingCostStrategy, FreeShippingStrategy>();
```

```csharp
public interface IShippingCostStrategy
{
    string Tier { get; }
    decimal CalculateCost(Order order);
}

public sealed class ShippingStrategySelector
{
    private readonly IReadOnlyDictionary<string, IShippingCostStrategy> _strategiesByTier;

    public ShippingStrategySelector(IEnumerable<IShippingCostStrategy> strategies)
    {
        _strategiesByTier = strategies.ToDictionary(s => s.Tier);
    }

    public IShippingCostStrategy Select(string tier) => _strategiesByTier[tier];
}
```

This avoids a container-specific keyed-registration API entirely — it works with any container that
supports resolving `IEnumerable<TService>` from multiple registrations of the same interface, which
is the common baseline across containers. Prefer it when you want the selection logic itself to stay
container-agnostic; prefer keyed registration when the container's own keyed-resolution API is
already in use elsewhere and consistency with it matters more.

## A registration mistake to avoid

Registering a strategy as the concrete type instead of the interface (`services
.AddSingleton<StandardShippingStrategy>()`) silently breaks substitutability — anything depending on
`IShippingCostStrategy` won't resolve, and anything depending on the concrete type directly has
defeated the pattern's whole point. Always register against the interface (or the generic strategy
interface, closed over its type arguments) that consuming code actually depends on.
