# Static Lambdas and Lambda Discard Parameters (C# 9.0 / .NET 5)

C# 9.0 (November 2020) added two independent, small fit-and-finish features for lambdas and
anonymous methods: a `static` modifier that forbids capturing the enclosing scope, and formal
discard parameters (`_`) so an unused parameter no longer needs a throwaway name.

## Syntax

```csharp
Func<int, int, int> add = static (x, y) => x + y;       // static lambda
Func<int, string, int> keyOnly = static (id, _) => id;  // discard parameter
```

## Basic use case: `static` to guarantee zero captures

```csharp
public static class Validators
{
    public static readonly Func<string, bool> IsNonEmpty = static s => !string.IsNullOrEmpty(s);
}
```

`static` on a lambda or anonymous method is a compile-time guarantee, not just documentation: the
compiler rejects any reference to `this`, or to a local/parameter from the enclosing scope, inside a
`static` lambda body. A lambda that captures nothing allocates no closure object; a `static` lambda
makes that property enforced rather than incidental. A lambda may still reference `static` members
and `const`s from the enclosing scope — those aren't captures.

```csharp
public class RateLimiter
{
    private const int MaxPerMinute = 60;

    // legal: MaxPerMinute is a const, not an instance/enclosing-scope capture
    public static readonly Func<int, bool> ExceedsLimit = static count => count > MaxPerMinute;
}
```

## Advanced use case: discards on a generic multi-parameter delegate

```csharp
public static Func<TKey, TValue, TKey> KeySelector<TKey, TValue>() => static (key, _) => key;

Func<int, string, int> idOf = KeySelector<int, string>();
```

Two or more parameters named `_` are each treated as a discard (no name is actually introduced,
so there's no duplicate-name error); a *single* `_` parameter is still an ordinary named parameter,
kept for backward compatibility with code that already used `_` as a real name.

```csharp
// three discards — legal from C# 9.0; under C# 8.0 this needed distinct throwaway names (_, __, ___)
Action<int, string, bool> logCallback = static (_, _, _) => Console.WriteLine("event fired");
```

## Requirements and restrictions

- `static` can be applied to a lambda, an anonymous method, or a local function — same rule across
  all three.
- Discard parameters don't introduce a name into any scope, so they never trigger a "duplicate
  parameter name" error even when there are several in the same parameter list.
- `static` and discard parameters are independent features and compose freely, but neither requires
  the other.

## Fallback

On C# 8.0 and earlier, drop `static` (the lambda still compiles, it just isn't guaranteed
capture-free — verify by inspection instead), and give each unused parameter a distinct throwaway
name (`_`, `__`, `___`, ...) since a repeated single `_` is a duplicate-name error before C# 9.0. See
[csharp8-lambda-parameter-shadowing.md](csharp8-lambda-parameter-shadowing.md) for the nearest older
tier.
