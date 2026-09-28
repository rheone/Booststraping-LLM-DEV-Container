# Adding a New Strategy Without Breaking Existing Code

The entire value proposition of Strategy is that adding a new algorithm variant touches nothing that
already exists — no context class changes, no existing strategy changes, no switch statement grows
a new case. This file covers how to keep that promise as a codebase grows.

## Adding an interface-based strategy

1. **Implement the interface.** Write the new class against the existing `IShippingCostStrategy` (or
   `IStrategy<TInput, TOutput>`) contract. Nothing about the interface itself changes.

   ```csharp
   public sealed class RegionalFlatRateShippingStrategy : IShippingCostStrategy
   {
       private readonly IReadOnlyDictionary<string, decimal> _ratesByRegion;

       public RegionalFlatRateShippingStrategy(IReadOnlyDictionary<string, decimal> ratesByRegion)
       {
           _ratesByRegion = ratesByRegion;
       }

       public decimal CalculateCost(Order order) => _ratesByRegion[order.Region];
   }
   ```

2. **Register it** alongside the existing strategies (see the DI-registration reference for keyed
   registration or the collection-injection form) without editing any existing registration line.

3. **Write its own unit tests** (see the testing reference) — no existing test needs to change,
   because no existing type changed.

No file that referenced `IShippingCostStrategy` before this addition needs to be touched. That is
the test for whether an extension was done correctly: if `OrderProcessor`, the selector, or any
other existing strategy implementation needed an edit to accommodate the new one, something broke
the pattern's substitutability — most commonly, the interface grew a method that not every existing
implementation actually needs (see "the interface segregation trap" below).

## Adding a delegate-based strategy

Nothing to implement — construct the context (or call the method) with a new lambda or method
reference. If the new variant is meant to be reusable and discoverable, add it as a named
`static readonly Func<>` field or static method alongside the others; this is purely additive.

## Adding a value to a generic strategy family

For `IStrategy<TInput, TOutput>` consumers, a new variant is a new closed implementation of the
existing open interface — the generic interface itself never needs a new type parameter or method
to support another algorithm, which is the main advantage of routing many unrelated algorithm
families through the same generic shape.

## The interface segregation trap

The most common way a Strategy hierarchy stops being extensible without breaking existing code is
an interface that accumulates members only some implementations can meaningfully support:

```csharp
public interface IShippingCostStrategy
{
    decimal CalculateCost(Order order);
    bool SupportsInternationalShipping { get; } // added later for one specific strategy
}
```

Every existing implementation now needs an edit to add `SupportsInternationalShipping`, even
implementations with nothing meaningful to say about it. Avoid this by keeping the interface to the
operation(s) every variant genuinely implements, and modeling a capability only some variants have
as a separate, optional interface that a context checks for via a type test:

```csharp
public interface ISupportsInternationalShipping
{
    decimal CalculateInternationalSurcharge(Order order);
}

if (strategy is ISupportsInternationalShipping international)
{
    cost += international.CalculateInternationalSurcharge(order);
}
```

This keeps the base contract stable for every existing and future implementation, and lets only the
variants that need the extra capability opt into it.

## Replacing a strategy instead of adding one

Retiring a strategy implementation is symmetric: delete the class, remove its registration, delete
its tests. Nothing in the context or the interface needs to change, provided no other code
constructs that concrete type directly instead of depending on the interface — which is exactly why
consuming code should never hold a concrete strategy type in a field or variable declaration, only
the interface or delegate type.
