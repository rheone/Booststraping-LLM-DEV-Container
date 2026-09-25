# Switch Expressions, Property, Tuple, and Positional Patterns (C# 8.0)

C# 8.0 shipped September 2019 as the first C# release targeting .NET Core specifically, and it's
the biggest single expansion of pattern matching after the C# 7.0 baseline. It adds the **switch
expression** (a compact, value-producing alternative to the `switch` statement) plus three new
*recursive* pattern kinds that can nest inside each other and inside a switch: **property
patterns**, **tuple patterns**, and **positional patterns**.

## Syntax

```csharp
decimal cost = shape switch
{
    Circle c => Math.PI * c.Radius * c.Radius,
    Rectangle r => r.Width * r.Height,
    null => throw new ArgumentNullException(nameof(shape)),
    _ => throw new ArgumentException("Unknown shape.", nameof(shape)),
};
```

## Basic use case: switch expression replacing a switch statement

```csharp
public static decimal GetShippingCost(Order order) => order switch
{
    null => throw new ArgumentNullException(nameof(order)),
    { IsInternational: true } => 24.99m,
    Order o when o.Total >= 200m => 0m,
    _ => 9.99m,
};
```

The `{ IsInternational: true }` arm is a **property pattern**: it tests a named property of the
matched value against a nested pattern (here, a constant-equality check against `true`). C# 8
alone has no relational pattern yet, so "total at least 200" still needs a `when` guard on a type
pattern (`Order o when o.Total >= 200m`) rather than a bare `{ Total: >= 200m }` property pattern —
that shorthand needs the relational pattern from
[csharp9-relational-and-logical-patterns.md](csharp9-relational-and-logical-patterns.md). A switch
*expression* has no `case`/`break` ceremony: each arm is `pattern => result`, arms are separated by
commas, and the whole thing evaluates to a value.

## Advanced use case: positional and tuple patterns for multi-value dispatch

```csharp
public record Point(int X, int Y);

public static string Classify(Point point) => point switch
{
    (0, 0) => "origin",
    (var x, 0) => $"on the x-axis at {x}",
    (0, var y) => $"on the y-axis at {y}",
    (var x, var y) when x == y => "on the diagonal",
    _ => "elsewhere",
};
```

```csharp
public static decimal GetDiscount(int groupSize, DayOfWeek day) => (groupSize, day) switch
{
    (10, DayOfWeek.Monday) => 0.30m,
    (_, DayOfWeek.Saturday) => 0m,
    (_, DayOfWeek.Sunday) => 0m,
    _ when groupSize >= 5 => 0.20m,
    _ => 0.05m,
};
```

A **positional pattern** like `(0, 0)` deconstructs the matched value via a `Deconstruct` method
(records synthesize one automatically per positional parameter) and matches each resulting value
against a nested pattern. A **tuple pattern** — matching a literal `(groupSize, day)` tuple
expression against `(pattern, pattern)` — is the same mechanism applied to a `ValueTuple` built
right at the switch site instead of deconstructing a single object; it's the standard way to
dispatch on more than one input value at once. C# 8 alone has no relational pattern (`>= 10`) or
logical `or` pattern yet — matching "any group size of 5 or more" needs a `when` guard, and
matching two alternative days needs two separate arms, as above. Once
[csharp9-relational-and-logical-patterns.md](csharp9-relational-and-logical-patterns.md) is
available, `( >= 5, DayOfWeek.Saturday or DayOfWeek.Sunday)` collapses both into the pattern
itself.

## Requirements and restrictions

- A positional pattern needs either a `Deconstruct` method (instance or extension) with a matching
  parameter count, or — for tuple patterns specifically — a literal tuple expression as the switch
  governing expression/`is` operand.
- A property pattern only matches non-`null` values; `{ }` alone (an empty property pattern) is a
  non-null check, equivalent to `is not null`, and can carry a variable: `shape is { } notNull`.
- A switch *expression* must be exhaustive or end in a discard arm (`_ => ...`); an unhandled input
  at run time throws. The compiler warns (not errors) on a non-exhaustive switch expression. Full
  exhaustiveness rules, the discard arm, and switch-expression-vs-statement tradeoffs are in
  [../specialized/switch-expressions-vs-statements.md](../specialized/switch-expressions-vs-statements.md).
- Property, positional, and tuple patterns are all *recursive*: any pattern (including another
  property or positional pattern) can nest inside one. Deep nesting and `when` clauses combined
  with recursive patterns are covered in
  [../specialized/pattern-combinators-nesting-and-when-clauses.md](../specialized/pattern-combinators-nesting-and-when-clauses.md).

## Fallback

Below C# 8.0, there's no switch expression — use the `switch` statement with `case`/`return`/
`throw` per case, as shown in
[csharp7-is-and-switch-patterns.md](csharp7-is-and-switch-patterns.md). There's also no property,
tuple, or positional pattern; deconstruct manually and test properties with `&&`-chained
conditions instead:

```csharp
public static string Classify(Point point)
{
    if (point.X == 0 && point.Y == 0)
    {
        return "origin";
    }

    if (point.Y == 0)
    {
        return $"on the x-axis at {point.X}";
    }

    return "elsewhere";
}
```
