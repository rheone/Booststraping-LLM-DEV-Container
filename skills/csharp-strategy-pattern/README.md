# C# Strategy Pattern

Reference for the Strategy design pattern in C#: the classic interface-based form, the
delegate/`Func<>`-based lightweight alternative, a generic `IStrategy<TInput, TOutput>` contract,
choosing an implementation via DI registration, and the judgment call between a named strategy and
an inline lambda. The routing table is in [SKILL.md](SKILL.md).

**`references/`**

| File | Covers |
| --- | --- |
| `classic-interface-strategy.md` | the interface-based form: one interface, one class per variant, a context that depends only on the interface |
| `delegate-based-strategy.md` | `Func<>`/`Action<>` fields as a lighter alternative; named delegate variants; delegate bundles for multi-operation cases |
| `generic-strategy-interface.md` | `IStrategy<TInput, TOutput>`, variance, and when the generic form beats a purpose-named interface |
| `strategy-selection-via-di.md` | single registration, keyed resolution, and resolving all implementations by an identifying property |
| `strategy-vs-inline-lambda.md` | the signals that justify promoting a lambda to a named strategy, and when the lambda is already correct |
| `testing-strategies.md` | testing a strategy implementation directly, testing a context with a fake/test-double strategy, testing selection logic |
| `extending-strategies.md` | adding a new strategy implementation without touching the context, the interface segregation trap, retiring a strategy |
