# Parameter Modifiers on Untyped Lambda Parameters (C# 14 / .NET 10)

C# 14 (November 2025) removed a requirement that had persisted since parameter modifiers on lambdas
first became legal: a lambda parameter with `scoped`, `ref`, `in`, `out`, or `ref readonly` no
longer needs an explicit parameter type alongside the modifier.

## Syntax

```csharp
delegate bool TryParse<T>(string text, out T result);

TryParse<int> parse = (text, out result) => int.TryParse(text, out result); // no types needed
```

## Basic use case

```csharp
delegate void Updater<T>(ref T value);

Updater<int> increment = (ref value) => value++; // modifier without an explicit type, C# 14+
```

Before C# 14, the same lambda required every parameter's type spelled out as soon as any modifier
appeared on any parameter:

```csharp
Updater<int> incrementOld = (ref int value) => value++; // required on C# 13 and earlier
```

## Advanced use case: generic `out` parameter matching a generic delegate

```csharp
public delegate bool Validator<T>(T candidate, out string error);

Validator<int> isPositive = (candidate, out error) =>
{
    error = candidate > 0 ? "" : "must be positive";
    return candidate > 0;
};
```

The modifier-without-type form works the same way against a generic delegate's type-parameter-typed
`out`/`ref` positions as it does against a concrete type — the lambda doesn't need to restate `T`,
it's inferred from the target delegate type exactly as an untyped lambda parameter always has been.

## Requirements and restrictions

- `params` is the one modifier this relaxation doesn't cover — a `params` lambda parameter still
  requires its type to be written explicitly.
- Once *any* parameter in the list has an explicit type, every parameter in that list must (this
  rule predates C# 14 and is unchanged); the C# 14 change is specifically about a modifier no longer
  *forcing* that requirement by itself.

## Fallback

On C# 13 and earlier, add the explicit parameter type alongside the modifier —
`(ref int value) => value++` instead of `(ref value) => value++` — everything else about the lambda
is unchanged. See
[csharp12-default-lambda-parameters.md](csharp12-default-lambda-parameters.md) for the nearest older
tier.
