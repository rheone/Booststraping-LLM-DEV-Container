# Testing Code Built on Strategy

Strategy's whole structure exists to make the algorithm swappable — which means the same structure
makes both the strategy and its context easy to test in isolation, as long as you test each side
through its actual seam rather than through the other.

## Testing a strategy implementation directly

A strategy implementation is a small, self-contained unit: construct it, call its one (or few)
operations, assert on the result. It needs no context, no container, no mocking framework.

```csharp
public class ExpressShippingStrategyTests
{
    [Fact]
    public void CalculateCost_AddsFlatFeeToWeightBasedRate()
    {
        var strategy = new ExpressShippingStrategy();
        var order = new Order(Weight: 10m);

        var cost = strategy.CalculateCost(order);

        Assert.Equal(25m, cost); // 10 * 1.5 + 10
    }
}
```

If a strategy implementation takes a constructor dependency (a rate lookup client, a clock), fake or
mock that dependency the same way you would for any other class under test — the strategy is not
special-cased just because it happens to implement a Strategy interface.

## Testing the context with a test-double strategy

The context's job is to *use* whatever strategy it's given, not to compute the algorithm itself.
Test the context by substituting a strategy whose output you control completely, and assert that the
context did the right thing with that output — never assert on the real algorithm's math from
inside a context test.

```csharp
public class OrderProcessorTests
{
    [Fact]
    public void Process_IncludesShippingCostFromStrategy()
    {
        IShippingCostStrategy fixedCostStrategy = new FixedCostStrategy(costToReturn: 42m);
        var processor = new OrderProcessor(fixedCostStrategy);
        var order = new Order(Weight: 999m); // irrelevant — the fake ignores it

        var receipt = processor.Process(order);

        Assert.Equal(42m, receipt.ShippingCost);
    }

    private sealed class FixedCostStrategy : IShippingCostStrategy
    {
        private readonly decimal _costToReturn;
        public FixedCostStrategy(decimal costToReturn) => _costToReturn = costToReturn;
        public decimal CalculateCost(Order order) => _costToReturn;
    }
}
```

A hand-written fake like `FixedCostStrategy` is usually clearer than a mocking-library setup for a
single-method interface — there is rarely enough behavior to justify the extra ceremony of
verifying call counts or argument matchers. Reach for a mock only when the context's contract with
the strategy includes *how* it's called (call count, argument shape) rather than just *what it
returns*.

## Testing a delegate-based strategy

A `Func<>`-typed dependency is substituted the same way, just without a class to declare:

```csharp
[Fact]
public void Process_IncludesShippingCostFromDelegate()
{
    var processor = new OrderProcessor(shippingCostStrategy: _ => 42m);

    var receipt = processor.Process(new Order(Weight: 999m));

    Assert.Equal(42m, receipt.ShippingCost);
}
```

This is the delegate form's main testing advantage over the interface form: no fake class to write
at all, since the lambda passed to the constructor already is the substitute.

## Testing strategy selection

When a selector resolves a strategy by key (from a dictionary, a keyed DI registration), test the
selector's resolution logic separately from any individual strategy's math — assert that the right
*type* (or a distinguishable fake registered under that key) comes back for a given key, and that an
unknown key fails the way the selector contract promises (an exception type, a `null`, a `Result`
failure), rather than re-testing what each strategy computes.
