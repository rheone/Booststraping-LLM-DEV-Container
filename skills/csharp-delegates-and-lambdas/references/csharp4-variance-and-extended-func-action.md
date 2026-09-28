# Variance and Extended `Func<>`/`Action<>` Arities (C# 4.0 / .NET Framework 4.0)

C# 4.0 (.NET Framework 4.0, April 2010) added two independent things relevant here: declaration-site
`in`/`out` variance annotations on generic interfaces and delegates, and the BCL extended
`Func<>`/`Action<>` from 4 input parameters up to 16
(`Func<T1, ..., T16, TResult>`/`Action<T1, ..., T16>`), replacing the need for a hand-rolled generic
delegate once a callback needed a 5th parameter.

## Syntax

```csharp
public delegate TResult Transformer<in TInput, out TResult>(TInput input);
```

`in` marks a type parameter contravariant (usable only in input/parameter positions); `out` marks
one covariant (usable only in output/return positions). Only interfaces and delegates can declare
variance — classes, structs, and generic methods cannot.

## Basic use case: the BCL's own variance

`Func<in T1, ..., in Tn, out TResult>` and `Action<in T1, ..., in Tn>` are declared with variance in
the BCL itself:

```csharp
Func<object, string> describeAnything = o => o.ToString() ?? "";

// legal without a cast, purely because of variance:
// Func<in T, out TResult> lets a Func<object, string> stand in for a Func<string, object>-shaped
// target as long as the target's TInput is more derived (string) and its TResult less derived
// than string would require — here TResult stays string either way, so only TInput varies.
Func<string, string> describeString = describeAnything;
```

Full rules, the input/output-position check, and worked custom-delegate examples are in
[specialized/variance-in-generic-delegates.md](../specialized/variance-in-generic-delegates.md) —
this file covers only that the annotations exist and where they came from.

## Advanced use case: a 5+ parameter callback without a custom delegate

```csharp
Func<string, int, int, StringComparison, bool, int> customIndexOf = (text, start, count, comparison, reverse) =>
    reverse
        ? text.Substring(0, start + count).LastIndexOf(text, comparison)
        : text.IndexOf(text, start, count, comparison);
```

Before C# 4.0/.NET 4.0, a callback shape with 5+ parameters needed a hand-declared generic delegate
(the `Transformer<TInput, TResult>`-style types from earlier tiers only cover 1–2 parameters by
convention, and there's no `Func`/`Action` with more than 4 inputs to reach for). From .NET 4.0
onward, `Func<>`/`Action<>` cover up to 16 parameters, so a custom delegate is worth declaring only
when the name itself documents intent — not merely to work around an arity limit.

## Requirements and restrictions

- Variance applies only to the delegate/interface's own type parameters, checked at the
  declaration, not per use — `Func<in T, out TResult>`'s `T` may never appear in a `TResult`-typed
  position inside the BCL type itself (which is why the BCL got to declare it that way safely).
- A type parameter with no `in`/`out` is invariant, same as every earlier tier.
- `Func<>`/`Action<>` at every arity (1–16) were declared variant from their introduction; a
  hand-rolled generic delegate must add `in`/`out` explicitly to get the same conversions — it
  isn't automatic just by being generic.

## Fallback

On .NET Framework 2.0/3.5 (C# 2.0/3.0), a callback needing 5+ parameters requires a hand-declared
generic delegate (no `Func`/`Action` above 4 inputs exists yet), and drop the `in`/`out` annotations
from any custom delegate — it still compiles and works, just without the implicit variance
conversions; callers need an explicit re-wrap or cast where the conversion used to be implicit. See
[csharp3-lambdas-and-func-action.md](csharp3-lambdas-and-func-action.md).
