# Delegate-Based Strategy

When the algorithm you're swapping out is a single operation with no extra state of its own, a
`Func<>` or `Action<>` field does the same job as an interface with one method, without the
interface, the implementing class, or the constructor boilerplate.

## Shape

```csharp
public sealed class OrderProcessor
{
    private readonly Func<Order, decimal> _shippingCostStrategy;

    public OrderProcessor(Func<Order, decimal> shippingCostStrategy)
    {
        _shippingCostStrategy = shippingCostStrategy;
    }

    public OrderReceipt Process(Order order)
    {
        var shippingCost = _shippingCostStrategy(order);
        return new OrderReceipt(order, shippingCost);
    }
}
```

Callers supply a lambda, a local function reference, or a static method group directly:

```csharp
var standard = new OrderProcessor(order => order.Weight * 0.5m);

var express = new OrderProcessor(order => order.Weight * 1.5m + 10m);

var free = new OrderProcessor(static _ => 0m);
```

Named strategies still work when you want a reusable, discoverable variant without an interface —
declare them as `static readonly Func<>` fields or as methods referenced by method group:

```csharp
public static class ShippingStrategies
{
    public static decimal Standard(Order order) => order.Weight * 0.5m;
    public static decimal Express(Order order) => order.Weight * 1.5m + 10m;
    public static decimal Free(Order order) => 0m;
}

var processor = new OrderProcessor(ShippingStrategies.Express);
```

This keeps named, testable, reusable variants while avoiding a class per variant — you get most of
the discoverability of the interface form's named implementations without the extra type.

## Multi-operation strategies via a delegate bundle

A strategy that genuinely needs more than one related operation can still avoid an interface by
grouping delegates into a record instead of a class hierarchy:

```csharp
public sealed record ShippingStrategy(
    Func<Order, decimal> CalculateCost,
    Func<Order, DateTime> EstimateDeliveryDate);

var express = new ShippingStrategy(
    CalculateCost: order => order.Weight * 1.5m + 10m,
    EstimateDeliveryDate: order => DateTime.UtcNow.AddDays(1));
```

This is a reasonable middle ground for two or three related operations. Once a strategy accumulates
enough operations that the record constructor becomes unwieldy, or once a variant needs
constructor-injected dependencies rather than captured closures, move to the interface form instead
— the delegate bundle stops paying for itself past that point.

## Capturing state safely

A lambda strategy that captures a mutable local or a loop variable is a common source of bugs when
the captured reference outlives the scope that created it, or when several strategies close over
the same mutable variable. Prefer capturing immutable data (a `readonly` field, a parameter, a
value copied into a local before the lambda) and avoid capturing `this` from a type with mutable
state unless the strategy is genuinely meant to observe that state's later changes.
