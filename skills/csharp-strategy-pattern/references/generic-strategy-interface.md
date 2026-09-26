# Generic Strategy Interface

A generic strategy interface parameterizes the algorithm over its input and output types instead of
hard-coding them, so the same interface shape serves every algorithm family in a codebase rather
than one interface per family.

## Shape

```csharp
public interface IStrategy<in TInput, out TOutput>
{
    TOutput Execute(TInput input);
}
```

Each concrete strategy closes the generic parameters over its own types:

```csharp
public sealed class StandardShippingStrategy : IStrategy<Order, decimal>
{
    public decimal Execute(Order input) => input.Weight * 0.5m;
}

public sealed class OrderValidationStrategy : IStrategy<Order, ValidationResult>
{
    public ValidationResult Execute(Order input) =>
        input.Items.Count == 0
            ? ValidationResult.Fail("Order has no items")
            : ValidationResult.Success();
}
```

A generic context can depend on `IStrategy<TInput, TOutput>` without knowing the concrete types at
compile time, which is useful for infrastructure code (pipelines, dispatchers, generic base
classes) that hosts strategies it never names directly:

```csharp
public sealed class StrategyRunner<TInput, TOutput>
{
    private readonly IStrategy<TInput, TOutput> _strategy;

    public StrategyRunner(IStrategy<TInput, TOutput> strategy)
    {
        _strategy = strategy;
    }

    public TOutput Run(TInput input) => _strategy.Execute(input);
}
```

## Variance

`in TInput` and `out TOutput` let a strategy typed for a base class satisfy a request for a more
derived input, and a strategy that returns a derived type satisfy a request for a base return type:

```csharp
IStrategy<Order, decimal> discountStrategy = new PercentageDiscountStrategy();
// PercentageDiscountStrategy : IStrategy<PriorityOrder, decimal> would NOT satisfy this
// without variance being applicable to the exact parameter — variance applies to reference
// conversions between compatible generic arguments, not to swapping unrelated input types.
```

Keep `TInput` contravariant (`in`) and `TOutput` covariant (`out`) only when every implementation
genuinely only *consumes* `TInput` and only *produces* `TOutput` — a strategy that also exposes
`TInput` as a return type or `TOutput` as a parameter (rare, but happens with bidirectional
transforms) cannot use variance and should drop the `in`/`out` modifiers rather than fight the
compiler.

## When the generic form is worth it

Use `IStrategy<TInput, TOutput>` over a purpose-named interface (`IShippingCostStrategy`) when:

- You're building infrastructure — a pipeline stage, a rule engine, a generic caching or retry
  wrapper — that hosts many unrelated strategy families through the same generic contract.
- The strategies genuinely have no operation beyond "take one input, produce one output"; a
  purpose-named interface with a purpose-named method (`CalculateCost(Order)`) still reads better
  at the call site for a single, well-known algorithm family and is worth the extra interface
  declaration.

A purpose-named interface that happens to have one method is not a worse choice than the generic
form — it is more self-documenting at every call site that depends on it. Reach for the generic
form specifically when the *number* of unrelated strategy families you need to host through one
mechanism is the actual problem you're solving.
