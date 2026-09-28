# Classic Interface-Based Strategy

The Strategy pattern separates an algorithm from the object that uses it. You define an interface
that names one operation, write one class per algorithm variant, and hand the object that needs the
algorithm a reference to whichever implementation it should run. The consuming object — the
*context* — never branches on which variant it holds; it just calls the interface member.

## Shape

```csharp
public interface IShippingCostStrategy
{
    decimal CalculateCost(Order order);
}

public sealed class StandardShippingStrategy : IShippingCostStrategy
{
    public decimal CalculateCost(Order order) => order.Weight * 0.5m;
}

public sealed class ExpressShippingStrategy : IShippingCostStrategy
{
    public decimal CalculateCost(Order order) => order.Weight * 1.5m + 10m;
}

public sealed class FreeShippingStrategy : IShippingCostStrategy
{
    public decimal CalculateCost(Order order) => 0m;
}
```

The context holds the interface, not any concrete strategy, and takes it through its constructor:

```csharp
public sealed class OrderProcessor
{
    private readonly IShippingCostStrategy _shippingStrategy;

    public OrderProcessor(IShippingCostStrategy shippingStrategy)
    {
        _shippingStrategy = shippingStrategy;
    }

    public OrderReceipt Process(Order order)
    {
        var shippingCost = _shippingStrategy.CalculateCost(order);
        return new OrderReceipt(order, shippingCost);
    }
}
```

`OrderProcessor` never mentions `StandardShippingStrategy` or `ExpressShippingStrategy` by name.
Swapping the shipping calculation for a given order is a matter of constructing `OrderProcessor`
with a different strategy instance — no edit to `OrderProcessor` itself.

## When the interface earns its cost

Reach for the interface form, rather than a delegate, when:

- **The algorithm needs more than one operation.** A strategy with `CalculateCost`,
  `EstimateDeliveryDate`, and `GetCarrierName` groups those three related members behind one
  substitutable type. A `Func<>` can only carry a single method signature.
- **A variant needs its own state or dependencies.** `ExpressShippingStrategy` might take an
  `ICarrierRateClient` in its constructor; a bare delegate has nowhere natural to hold that beyond
  a captured closure, which gets harder to read and test as the dependency list grows.
- **You want the variant's identity to show up in a stack trace, a DI registration, or a log line.**
  `typeof(ExpressShippingStrategy).Name` is a stable, meaningful label. A delegate's target method
  name is not something you want callers relying on.

## Naming convention

Name the interface for the *role* the algorithm plays (`IShippingCostStrategy`,
`IDiscountStrategy`, `ITaxCalculationStrategy`), not for the pattern itself in every case — the
`Strategy` suffix is a helpful signal when several interchangeable families exist side by side in
the same codebase, but the interface name should always describe what the algorithm computes or
does.
