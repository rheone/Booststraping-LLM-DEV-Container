# C# Delegates and Lambdas

Helps you write, review, or port delegates, anonymous methods, and lambda expressions in C#,
from the original `delegate` keyword and multicast invocation lists through modern lambda syntax
refinements. Does not cover `dynamic` typing or expression trees.

## When to reach for it

- Choosing between `Func<>`/`Action<>`/`Predicate<T>` and a hand-declared custom delegate
- Diagnosing a closure or variable-capture bug in a lambda
- Reasoning about invocation order or return values on a multicast delegate
- Deciding whether a lambda needs to be `static` to avoid an unwanted capture
- Gating delegate or lambda syntax by C# language version when porting older code

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting a
delegate declaration, anonymous method, or lambda expression.

## What it covers

| Topic | Reference |
| --- | --- |
| The `delegate` keyword and multicast invocation (the universal baseline) | [references/csharp1-delegates-and-multicast.md](references/csharp1-delegates-and-multicast.md) |
| Anonymous methods and the first generic BCL delegates | [references/csharp2-anonymous-methods-and-generic-delegates.md](references/csharp2-anonymous-methods-and-generic-delegates.md) |
| Lambda expression syntax, `Func<>`/`Action<>` | [references/csharp3-lambdas-and-func-action.md](references/csharp3-lambdas-and-func-action.md) |
| `in`/`out` variance, extended `Func<>`/`Action<>` arities | [references/csharp4-variance-and-extended-func-action.md](references/csharp4-variance-and-extended-func-action.md) |
| Lambda parameter shadowing | [references/csharp8-lambda-parameter-shadowing.md](references/csharp8-lambda-parameter-shadowing.md) |
| Static lambdas and discard parameters | [references/csharp9-static-lambdas-and-discard-parameters.md](references/csharp9-static-lambdas-and-discard-parameters.md) |
| Natural type inference, explicit lambda return types | [references/csharp10-natural-type-and-lambda-annotations.md](references/csharp10-natural-type-and-lambda-annotations.md) |
| Default lambda parameter values | [references/csharp12-default-lambda-parameters.md](references/csharp12-default-lambda-parameters.md) |
| Untyped modifier parameters (`scoped`/`ref`/`in`/`out`) | [references/csharp14-lambda-parameter-modifiers.md](references/csharp14-lambda-parameter-modifiers.md) |

## Example prompts

- "Should I declare a custom delegate here or just use `Func<int, bool>`?"
- "Why does this lambda keep capturing the loop variable from the previous iteration?"
- "Does this multicast delegate return the last handler's value, or something else?"
