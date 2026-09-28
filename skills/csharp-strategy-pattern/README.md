# Strategy Pattern

You separate an algorithm from the code that uses it: a context class depends on an abstraction for
"the operation," and any number of interchangeable implementations swap in behind that abstraction
at runtime. It covers the classic interface-based form, a lightweight delegate-based alternative, a
generic strategy contract, choosing an implementation through DI, and when a lambda is enough on
its own.

## When to reach for it

- A piece of code needs to swap an algorithm or calculation at runtime, and you're deciding how to
  structure that swap.
- You're weighing whether a decision point deserves a named strategy interface or is better left as
  an inline lambda.
- You're designing infrastructure code that needs to host many unrelated algorithm families behind
  one shared, generic contract.
- You're wiring which strategy implementation a context receives through a DI container, including
  keyed resolution or resolving all implementations by an identifying key.

## Using it

This skill is model-invoked: it fires automatically when your prompt matches its situation, such as
designing a swappable algorithm or reviewing DI-based strategy selection. You can also invoke it
directly as `/csharp-strategy-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The classic interface-based form | [references/classic-interface-strategy.md](references/classic-interface-strategy.md) |
| A delegate/`Func<>`-based lightweight alternative | [references/delegate-based-strategy.md](references/delegate-based-strategy.md) |
| A generic `IStrategy<TInput, TOutput>` contract | [references/generic-strategy-interface.md](references/generic-strategy-interface.md) |
| Selecting an implementation through DI registration | [references/strategy-selection-via-di.md](references/strategy-selection-via-di.md) |
| Named strategy vs. an inline lambda | [references/strategy-vs-inline-lambda.md](references/strategy-vs-inline-lambda.md) |
| Testing a strategy implementation or its context | [references/testing-strategies.md](references/testing-strategies.md) |
| Adding a new strategy implementation | [references/extending-strategies.md](references/extending-strategies.md) |

## Example prompts

- "I need to calculate shipping cost differently depending on the carrier. How do I make that
  swappable at runtime?"
- "Is this pricing calculation simple enough to stay a lambda, or should I promote it to a named
  strategy?"
- "How do I register three different `IDiscountStrategy` implementations in DI and resolve the
  right one by a key?"
