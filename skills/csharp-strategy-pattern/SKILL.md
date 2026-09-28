---
name: csharp-strategy-pattern
description: Reference for the Strategy design pattern in C# — the classic interface-based form (an IStrategy abstraction with interchangeable implementations selected at runtime), the delegate/Func<>-based lightweight alternative for simple cases, a generic IStrategy<TInput, TOutput> interface for hosting many unrelated algorithm families through one contract, choosing an implementation via DI registration (single registration, keyed resolution, or resolving all implementations by an identifying key), and the judgment call between a named strategy and an inline lambda. Use when writing or reviewing code that swaps an algorithm at runtime, deciding whether a decision point needs a Strategy interface or is fine as a lambda, designing a generic strategy contract for infrastructure code, or wiring strategy selection through a DI container.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Strategy Pattern

Strategy separates an algorithm from the code that uses it: a context class depends on an
abstraction for "the operation," and any number of interchangeable implementations can be
substituted behind that abstraction without changing the context.

## Quick start

```csharp
public interface IShippingCostStrategy
{
    decimal CalculateCost(Order order);
}

public sealed class OrderProcessor
{
    private readonly IShippingCostStrategy _shippingStrategy;

    public OrderProcessor(IShippingCostStrategy shippingStrategy) => _shippingStrategy = shippingStrategy;

    public OrderReceipt Process(Order order) =>
        new OrderReceipt(order, _shippingStrategy.CalculateCost(order));
}
```

For a single-operation algorithm with no independent state, a `Func<Order, decimal>` constructor
parameter does the same job without the interface — see
[references/delegate-based-strategy.md](references/delegate-based-strategy.md).

## Pick your reference file

| Situation | Reference file |
| --- | --- |
| Writing the classic interface-based form | [references/classic-interface-strategy.md](references/classic-interface-strategy.md) |
| A single-operation algorithm with no state of its own | [references/delegate-based-strategy.md](references/delegate-based-strategy.md) |
| Hosting many unrelated algorithm families through one generic contract | [references/generic-strategy-interface.md](references/generic-strategy-interface.md) |
| Choosing which implementation a context gets, from a DI container | [references/strategy-selection-via-di.md](references/strategy-selection-via-di.md) |
| Deciding whether a decision point needs a named strategy or is fine as an inline lambda | [references/strategy-vs-inline-lambda.md](references/strategy-vs-inline-lambda.md) |
| Testing a strategy implementation or the context that consumes one | [references/testing-strategies.md](references/testing-strategies.md) |
| Adding a new strategy implementation without touching existing code | [references/extending-strategies.md](references/extending-strategies.md) |
