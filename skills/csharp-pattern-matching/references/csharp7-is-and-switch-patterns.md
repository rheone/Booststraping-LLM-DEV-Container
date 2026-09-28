# `is`-Expressions and Switch-Statement Patterns (C# 7.0)

C# 7.0 shipped March 2017 alongside Visual Studio 2017 and introduced pattern matching to the
language for the first time: the *declaration pattern* (`is Type variable`), the *constant
pattern* (`is 5`, `is null`), the *var pattern* (`is var x`), and the ability to use all of these
as `case` labels in a `switch` statement, with an optional `when` guard on each case. This is the
baseline every later tier in this skill builds on.

## Syntax

```csharp
if (shape is Circle circle)
{
    // circle is typed as Circle here
}
```

```csharp
switch (shape)
{
    case Circle circle when circle.Radius <= 0:
        throw new ArgumentException("Radius must be positive.");
    case Circle circle:
        return Math.PI * circle.Radius * circle.Radius;
    case Rectangle rectangle:
        return rectangle.Width * rectangle.Height;
    case null:
        throw new ArgumentNullException(nameof(shape));
    default:
        throw new ArgumentException("Unknown shape.", nameof(shape));
}
```

## Basic use case

```csharp
public static double GetArea(object shape)
{
    switch (shape)
    {
        case Circle c:
            return Math.PI * c.Radius * c.Radius;
        case Rectangle r:
            return r.Width * r.Height;
        default:
            throw new ArgumentException("Unknown shape.", nameof(shape));
    }
}
```

The declaration pattern (`Circle c`) folds the old "`is` check, then cast" two-step from
[pre-csharp7-manual-type-checks.md](pre-csharp7-manual-type-checks.md) into one: `c` is only in
scope, and only definitely assigned, inside the branch where the match succeeded.

## Advanced use case: `when` guards ordered by specificity

```csharp
public static decimal GetShippingCost(Order order)
{
    switch (order)
    {
        case Order o when o.Total >= 200m:
            return 0m;
        case Order o when o.Total >= 50m:
            return 5.99m;
        case Order o when o.IsInternational:
            return 24.99m;
        case Order o:
            return 9.99m;
        case null:
            throw new ArgumentNullException(nameof(order));
    }
}
```

`switch`-statement cases are evaluated top to bottom, and a `when` guard is just a boolean
expression tacked onto a pattern — this reads like an `if`/`else if` chain, but the shared `case
Order o` prefix plus the compiler-enforced pattern syntax is what a bare `if` chain doesn't give
you. Guard order matters: a `case Order o:` with no guard placed above the guarded cases would
swallow every order before its more specific siblings ever ran.

## Requirements and restrictions

- `switch`-statement cases still require `break`, `return`, `throw`, or another way out of each
  reachable case — this tier only adds patterns to case labels, it doesn't change fallthrough
  rules.
- A pattern variable declared in one `case` (e.g. `Circle c`) is scoped to that case block only;
  it isn't visible in sibling cases or after the switch.
- `case null:` and a `when`-guarded case together let you order a null check ahead of, or mixed
  among, type checks — but each `case` label's pattern still has to be distinguishable from every
  other reachable case, or the compiler flags it as unreachable.
- There is no `switch` *expression* yet — every `switch` here is a statement. See
  [csharp8-switch-expressions-and-recursive-patterns.md](csharp8-switch-expressions-and-recursive-patterns.md).
- Pattern matching on a variable typed as a generic type parameter isn't allowed yet in C# 7.0 —
  see [csharp7.1-generic-type-parameter-patterns.md](csharp7.1-generic-type-parameter-patterns.md).

## Fallback

Below C# 7.0, there's no declaration pattern and no pattern-based `switch` case — use the
`as`-plus-null-check idiom and a plain `if`/`else if` chain (or a constant-only `switch` for the
parts that are genuinely constant matches) as shown in
[pre-csharp7-manual-type-checks.md](pre-csharp7-manual-type-checks.md).
