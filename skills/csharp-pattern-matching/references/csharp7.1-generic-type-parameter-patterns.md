# Pattern Matching on Generic Type Parameters (C# 7.1)

C# 7.1 shipped August 2017 alongside .NET Core 2.0 — the first of C#'s "point releases." Among its
three small language additions, it relaxed the pattern-matching rules from C# 7.0 so that the
pattern expression in an `is`-expression or a `switch` case can have the type of a **generic type
parameter**, not just a concrete or open type known at the call site. In C# 7.0, writing `case A
a:` inside a method generic over `T` didn't compile when `T` itself was the thing being matched
against; C# 7.1 fixed that by allowing a match to succeed via an identity conversion, implicit
reference conversion, boxing/unboxing conversion, or explicit reference conversion between the
open type `T` and the pattern's type — the same set of conversions non-generic pattern matching
already relied on, just extended to cover the case where either side of the conversion is an open
type.

## Syntax

```csharp
public static string Describe<T>(T value)
{
    switch (value)
    {
        case int i:
            return $"int {i}";
        case string s:
            return $"string \"{s}\"";
        case null:
            return "null";
        default:
            return $"other: {value}";
    }
}
```

## Basic use case

```csharp
public static bool TryGetAsCircle<T>(T shape, out Circle circle)
{
    if (shape is Circle c)
    {
        circle = c;
        return true;
    }

    circle = null;
    return false;
}
```

`shape is Circle c` compiles here even though `shape`'s static type is the open type parameter
`T` — the compiler defers the actual type test to the run-time type of whatever `T` turns out to
be at the call site, exactly as it would if `shape` were declared `object`.

## Advanced use case: dispatching inside a generic algorithm over `T`

```csharp
public static TResult Fold<T, TResult>(
    IEnumerable<T> source,
    TResult seed,
    Func<TResult, T, TResult> accumulate)
{
    TResult result = seed;
    foreach (T item in source)
    {
        switch (item)
        {
            case IComparable<T> comparable when comparable.CompareTo(default) < 0:
                continue; // skip negative-ish elements when T supports comparison
            case null:
                continue;
            default:
                result = accumulate(result, item);
                break;
        }
    }

    return result;
}
```

Matching `item` (statically typed `T`) against `IComparable<T>` inside a generic method is exactly
the scenario C# 7.1 unlocked: the pattern's type (`IComparable<T>`) can itself mention the
enclosing method's type parameter, and the match still resolves against `item`'s run-time type.

## Requirements and restrictions

- The relaxed rule applies to the pattern *expression's* type being open (a type parameter, or a
  constructed type that mentions one) — it doesn't add any new pattern *kind*. Only declaration,
  type, `var`, and constant patterns exist at this point in the timeline; see
  [csharp7-is-and-switch-patterns.md](csharp7-is-and-switch-patterns.md) for what those are.
  Generic pattern matching gets deeper worked examples (`List<T>`, constrained type parameters) in
  [../specialized/pattern-combinators-nesting-and-when-clauses.md](../specialized/pattern-combinators-nesting-and-when-clauses.md).
- Requires `<LangVersion>7.1</LangVersion>` or higher (or a target framework whose default
  language version is 7.1+, i.e. .NET Core 2.0 and later SDKs).

## Fallback

Below C# 7.1, pattern-matching a value whose static type is an open generic type parameter against
a concrete or interface type doesn't compile — the C# 7.0 compiler only pattern-matches against
expressions with a closed static type. Cast to `object` first, then match against `object`:

```csharp
public static string Describe<T>(T value)
{
    object boxed = value;
    switch (boxed)
    {
        case int i:
            return $"int {i}";
        case string s:
            return $"string \"{s}\"";
        default:
            return "other";
    }
}
```

The explicit cast to `object` sidesteps the open-type restriction at the cost of boxing value
types on every call; everything else about the pattern syntax is unchanged from
[csharp7-is-and-switch-patterns.md](csharp7-is-and-switch-patterns.md).
