---
name: csharp-delegates-and-lambdas
description: Reference for C# delegates and lambdas — the `delegate` keyword, multicast delegates, and method group conversion (C# 1.0–2.0 / .NET Framework 1.0–2.0), anonymous methods and the first generic BCL delegates (C# 2.0), lambda expression syntax and `Func<>`/`Action<>` (C# 3.0 / .NET Framework 3.5), `in`/`out` variance and extended `Func<>`/`Action<>` arities up to 16 parameters (C# 4.0 / .NET Framework 4.0), lambda parameter shadowing (C# 8.0), static lambdas and discard parameters (C# 9.0), natural type inference and explicit return types (C# 10.0), default lambda parameters (C# 12.0), and untyped modifier parameters (C# 14.0 / .NET 10). Use when writing, reviewing, or porting a delegate declaration, an anonymous method, or a lambda expression; choosing between `Func<>`/`Action<>`/`Predicate<T>` and a custom delegate; diagnosing a closure/variable-capture bug; reasoning about multicast delegate invocation order or return values; or gating delegate/lambda syntax by C# language version. Explicitly does not cover `dynamic` typing or expression trees.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Delegates and Lambdas

Delegates have been in C# since its first release; every later tier adds a way to *write* one more
concisely (anonymous methods, then lambdas) or a way to *shape* one more richly (generic delegate
types, variance, natural typing) without retiring anything earlier. The invocation-list mechanics
in [references/csharp1-delegates-and-multicast.md](references/csharp1-delegates-and-multicast.md)
still underlie every lambda written under the newest tier below. Out of scope: `dynamic` typing, and
the mechanics of `Expression<TDelegate>` (a lambda's *other* possible compiled form).

## Quick start (works everywhere, C# 3.0+)

```csharp
Func<int, int, int> add = (x, y) => x + y;
Action<string> log = message => Console.WriteLine(message);
Predicate<int> isNegative = n => n < 0;

int sum = add(2, 3); // 5
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets.

| Target | C# language version | Reference file |
| --- | --- | --- |
| .NET Framework 1.0+ | C# 1.0+ | [references/csharp1-delegates-and-multicast.md](references/csharp1-delegates-and-multicast.md) — the `delegate` keyword, multicast (`+=`/`-=`) invocation lists; the universal baseline |
| .NET Framework 2.0+ | C# 2.0+ | [references/csharp2-anonymous-methods-and-generic-delegates.md](references/csharp2-anonymous-methods-and-generic-delegates.md) — anonymous methods, `Predicate<T>`/`Comparison<T>`/`Converter<TInput,TResult>`, implicit method group conversion, closures |
| .NET Framework 3.5+ | C# 3.0+ | [references/csharp3-lambdas-and-func-action.md](references/csharp3-lambdas-and-func-action.md) — lambda expression syntax, `Func<>`/`Action<>` (0–4 parameters) |
| .NET Framework 4.0+ | C# 4.0+ | [references/csharp4-variance-and-extended-func-action.md](references/csharp4-variance-and-extended-func-action.md) — `in`/`out` variance on generic delegates, `Func<>`/`Action<>` extended to 16 parameters |
| .NET Core 3.0+ | C# 8.0+ | [references/csharp8-lambda-parameter-shadowing.md](references/csharp8-lambda-parameter-shadowing.md) — a lambda parameter may shadow an enclosing-scope name |
| .NET 5+ | C# 9.0+ | [references/csharp9-static-lambdas-and-discard-parameters.md](references/csharp9-static-lambdas-and-discard-parameters.md) — `static` lambdas/anonymous methods, `_` discard parameters |
| .NET 6+ | C# 10.0+ | [references/csharp10-natural-type-and-lambda-annotations.md](references/csharp10-natural-type-and-lambda-annotations.md) — natural type for `var`-assigned lambdas/method groups, explicit return types, parameter attributes |
| .NET 8+ | C# 12.0+ | [references/csharp12-default-lambda-parameters.md](references/csharp12-default-lambda-parameters.md) — default parameter values on lambdas |
| .NET 10+ | C# 14.0+ | [references/csharp14-lambda-parameter-modifiers.md](references/csharp14-lambda-parameter-modifiers.md) — `scoped`/`ref`/`in`/`out`/`ref readonly` on a lambda parameter without an explicit type |

C# 5–7.3, 11, 13, and 15 (the latest, in .NET 11 preview as of this writing) add nothing
delegate/lambda-specific — no reference file exists for those versions, and code written against
the newest tier above still compiles under whichever of those `<LangVersion>`s came after it.

## Specialized patterns

- [specialized/closures-and-variable-capture.md](specialized/closures-and-variable-capture.md) — capture-by-reference, loop-variable pitfalls (`for` vs. `foreach`), captured-state lifetime, `this` capture keeping a whole instance alive
- [specialized/multicast-delegate-invocation-semantics.md](specialized/multicast-delegate-invocation-semantics.md) — invocation-list order, why only the last entry's return value survives, exception behavior mid-chain
- [specialized/variance-in-generic-delegates.md](specialized/variance-in-generic-delegates.md) — `Func<>`'s covariant `TResult`, `Action<>`'s contravariant parameters, designing a custom variant delegate
- [specialized/generic-methods-as-lambdas-and-method-groups.md](specialized/generic-methods-as-lambdas-and-method-groups.md) — closing a generic method into a delegate, generic methods that accept delegate parameters, when a custom generic delegate earns its keep over `Func<>`
- [specialized/testing-delegates-and-lambdas.md](specialized/testing-delegates-and-lambdas.md) — `Func<T,bool>` assertion helpers, `Action`-based setup/teardown, delegate-based fakes, callback verification via capturing lambdas
