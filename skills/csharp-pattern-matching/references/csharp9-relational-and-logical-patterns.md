# Relational, Logical, and Bare Type Patterns (C# 9.0)

C# 9.0 shipped November 2020 with .NET 5 and added three pattern kinds that let a single pattern
express what previously needed a `when` guard or several switch arms: **relational patterns**
(`< 100`, `>= 0`), **logical patterns** (`and`, `or`, `not`), and a **bare type pattern** — a type
name alone in a `case`/switch arm, with no variable and no `_` discard, as a way to test only the
run-time type. Parenthesized patterns (`(... )`) also arrive here, purely to control precedence
among the new combinators.

## Syntax

```csharp
string Classify(double measurement) => measurement switch
{
    < 0.0 => "negative",
    0.0 => "zero",
    > 0.0 and <= 100.0 => "in range",
    > 100.0 => "too high",
    double.NaN => "not a number",
    _ => "unreachable", // double is never exhaustively covered without a discard
};
```

## Basic use case: relational patterns replacing `when` guards

```csharp
public static decimal GetShippingCost(Order order) => order switch
{
    null => throw new ArgumentNullException(nameof(order)),
    { Total: >= 200m } => 0m,
    { Total: >= 50m } => 5.99m,
    _ => 9.99m,
};
```

Compare this to the C# 8-only version in
[csharp8-switch-expressions-and-recursive-patterns.md](csharp8-switch-expressions-and-recursive-patterns.md),
which needed `Order o when o.Total >= 200m` because a property pattern couldn't nest a relational
pattern yet — `{ Total: >= 200m }` folds the comparison directly into the property pattern's nested
pattern, no `when` and no throwaway variable required.

## Advanced use case: `and`/`or`/`not` combinators and the bare type pattern

```csharp
public static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u'
    or 'A' or 'E' or 'I' or 'O' or 'U';

public static string Grade(int score) => score switch
{
    < 0 or > 100 => throw new ArgumentOutOfRangeException(nameof(score)),
    >= 90 => "A",
    >= 80 and < 90 => "B",
    >= 70 and < 80 => "C",
    _ => "F",
};

public static decimal CalculateToll(Vehicle vehicle) => vehicle switch
{
    Car => 2.00m,   // bare type pattern: type test only, no variable needed
    Truck => 7.50m,
    null => throw new ArgumentNullException(nameof(vehicle)),
    _ => throw new ArgumentException("Unknown vehicle type.", nameof(vehicle)),
};

public static bool IsNotBlank(string? text) => text is not (null or "");
```

`Car =>` and `Truck =>` are bare type patterns — before C# 9, the equivalent required a throwaway
variable (`Car _ =>`) even when nothing inside the arm needed it. `not (null or "")` shows `not`
applied to a parenthesized `or` pattern; without the parentheses, `not` binds to only `null`
(see the operator-precedence note below), which is a common mistake worth calling out explicitly.

## Requirements and restrictions

- A relational pattern's right-hand side must be a constant expression of an integral,
  floating-point, `char`, or `enum` type — not an arbitrary runtime expression, and not `string`.
- Pattern combinator precedence, low to high: `or` binds *loosest*, then `and`, then `not` binds
  *tightest* to its immediate operand. `not >= 'a' and <= 'z'` parses as `(not >= 'a') and <= 'z'`,
  not `not (>= 'a' and <= 'z')` — almost never what's intended. Parenthesize deliberately rather
  than relying on default precedence once `not` and `and`/`or` combine; more worked examples
  (including this exact pitfall) are in
  [../specialized/pattern-combinators-nesting-and-when-clauses.md](../specialized/pattern-combinators-nesting-and-when-clauses.md).
- The bare type pattern and the discard pattern (`_`) are different: `_` matches everything
  including `null`; a bare type pattern (and every other C# pattern kind except `var` and `_`)
  requires the matched value to be non-`null`.

## Fallback

Below C# 9.0, there's no relational or logical pattern and no bare type pattern — use `when`
guards for comparisons, chain `case`/switch arms (or `||`/`&&` inside a `when` guard) for
alternatives, and always declare a discard variable (`Car _ =>`) rather than a bare type name, as
shown in
[csharp8-switch-expressions-and-recursive-patterns.md](csharp8-switch-expressions-and-recursive-patterns.md)
and [csharp7-is-and-switch-patterns.md](csharp7-is-and-switch-patterns.md).
