# Lambda Parameter Shadowing (C# 8.0 / .NET Core 3.0)

C# 8.0 (September 2019) relaxed a naming restriction: a lambda (or anonymous method, or local
function) parameter may now shadow a local variable or parameter from its enclosing scope. Under
C# 7.3 and earlier, doing so was a compile error.

## Syntax

```csharp
int items = 5;

Func<int, int> countLabel = items => items + 1; // `items` parameter shadows the outer `items` local
```

No new syntax — the change is purely that the compiler stops rejecting a name collision it used to
flag as an error.

## Basic use case

```csharp
int total = orders.Count;

Func<Order, bool> isRecent = total => total.PlacedOn > DateTime.UtcNow.AddDays(-7);
// `total` inside the lambda refers to the PARAMETER, never the outer `int total` —
// this compiles under C# 8.0+; under C# 7.3 it was CS0136 "a local ... cannot be
// declared in this scope because that name is used in an enclosing scope"
```

## Advanced use case: generic method with a shadowing parameter name

```csharp
public static IEnumerable<TResult> Select<TSource, TResult>(
    this IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    foreach (TSource source in source) // shadows the outer parameter `source` with the loop variable
    {
        yield return selector(source);
    }
}
```

This example is deliberately confusing to read — shadowing being *legal* doesn't make it *advisable*
inside a single method body. It's most defensible when a lambda parameter name is the natural,
narrow-scope name for a value that happens to collide with something declared far away in a large
enclosing method, not as a routine choice.

## Requirements and restrictions

- Shadowing a name from the *immediately* enclosing scope inside the *same* lambda or local
  function is still an error — this relaxation applies to the outer method's locals/parameters, not
  to a name already used earlier in the same lambda's own parameter list.
- Applies identically to anonymous-method parameters and local-function parameters, not just lambda
  parameters — all three were updated together in C# 8.0.

## Fallback

On C# 7.3 and earlier, rename the lambda parameter to avoid the collision — the compiler error is
the only signal; there's no attribute or syntax to opt into the old or new behavior per-lambda, it's
purely a `<LangVersion>` gate. See
[csharp3-lambdas-and-func-action.md](csharp3-lambdas-and-func-action.md) for the lambda syntax this
tier doesn't otherwise change.
