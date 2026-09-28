# Default Parameter Values on Lambdas (C# 12 / .NET 8)

C# 12 (November 2023) let a lambda expression declare default parameter values, the same way an
ordinary method or local function already could — closing another gap between lambda syntax and
regular method syntax.

## Syntax

```csharp
var greet = (string name, string greeting = "Hello") => $"{greeting}, {name}!";
```

## Basic use case

```csharp
var power = (int value, int exponent = 2) =>
    Enumerable.Repeat(value, exponent).Aggregate(1, (acc, v) => acc * v);

int squared = power(5);      // 25 — exponent defaults to 2
int cubed = power(5, 3);     // 125 — default overridden
```

A lambda with a default parameter value has no `Func<>`/`Action<>` **natural type** — those BCL
delegates have no notion of an optional parameter, so `Func<int, int, int>` can't represent
"`exponent` defaults to 2." Assigning the lambda to `var` (as above) makes the compiler synthesize
an anonymous delegate type that *does* carry the default, so `power(5)` compiles. Explicitly typing
the variable as `Func<int, int, int> power = (value, exponent = 2) => ...;` still compiles, but the
default becomes unreachable — `Func<int,int,int>.Invoke(int,int)` always requires both arguments,
so every call through that variable must supply `exponent` itself.

## Advanced use case: default value referencing a generic type's default

```csharp
public static TValue GetOrDefault<TKey, TValue>(
    Dictionary<TKey, TValue> map, TKey key, TValue fallback = default!) where TKey : notnull =>
    map.TryGetValue(key, out TValue? value) ? value! : fallback;

var lookup = (Dictionary<string, int> map, string key, int fallback = -1) =>
    map.TryGetValue(key, out int value) ? value : fallback;

int result = lookup(new Dictionary<string, int> { ["a"] = 1 }, "missing"); // -1, default used
```

## Requirements and restrictions

- The same rules govern default values on lambda parameters as on any method: the default must be
  a compile-time constant (or `default`), and every parameter after the first defaulted one must
  also have a default.
- A default value on a lambda parameter does not change the underlying delegate type's signature —
  `Func<int, int, int>` still has exactly two required parameters from the delegate's own
  perspective; the default only affects direct-call sites where the compiler can see the lambda
  declaration itself.

## Fallback

On C# 11 and earlier, give the parameter a fixed value inside the lambda body instead of a default,
or provide two overloaded local functions/lambdas (one with the parameter, one without) — there's no
default-parameter syntax on a lambda before C# 12. See
[csharp10-natural-type-and-lambda-annotations.md](csharp10-natural-type-and-lambda-annotations.md)
for the nearest older tier.
