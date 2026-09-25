# Manual Type Checks and Constant Switches (before C# 7.0)

Before C# 7.0 (March 2017), there was no pattern matching in the language at all. `is` was a
plain boolean type-check operator with no ability to declare a variable from the check, and
`switch` accepted only *constant* case labels over `char`, `string`, `bool`, an integral numeric
type, or an `enum` — never a type test, never a `when`-style guard. Testing "is this object an X,
and if so use it as an X" took two statements: the `is` check, then a separate cast (or `as` plus
a null check) to actually get a typed reference.

## Syntax

```csharp
if (shape is Circle)
{
    Circle circle = (Circle)shape;
    // use circle
}
```

```csharp
Circle circle = shape as Circle;
if (circle != null)
{
    // use circle
}
```

## Basic use case

```csharp
public static double GetArea(object shape)
{
    if (shape is Circle)
    {
        Circle circle = (Circle)shape;
        return Math.PI * circle.Radius * circle.Radius;
    }

    Rectangle rectangle = shape as Rectangle;
    if (rectangle != null)
    {
        return rectangle.Width * rectangle.Height;
    }

    throw new ArgumentException("Unknown shape.", nameof(shape));
}
```

The `as`-then-null-check idiom was preferred over `is`-then-cast in most style guides of the era:
it does one runtime type test instead of two (`is` followed by a redundant cast-time check), at
the cost of only working for reference types and nullable value types.

## Advanced use case: dispatch on a closed set via a chain of checks

```csharp
public static string Describe(object value)
{
    if (value == null)
    {
        return "nothing";
    }

    string text = value as string;
    if (text != null)
    {
        return $"text of length {text.Length}";
    }

    if (value is int)
    {
        return $"integer {(int)value}";
    }

    if (value is DayOfWeek)
    {
        return $"day {(DayOfWeek)value}";
    }

    return "something else";
}
```

Every branch of a type-driven dispatch like this is hand-written: no compiler exhaustiveness
check, no shared syntax between the "is this a particular type" test and the "does this constant
equal that value" test — `switch` could do the latter (on `int`/`string`/`enum`/etc.) but never the
former, so mixed type-and-value dispatch always fell back to a chain of `if`/`else if`.

## Requirements and restrictions

- `switch` case labels must be compile-time constants of `char`, `string`, `bool`, an integral
  type, or an `enum` — no type tests, no ranges, no guards.
- `is` only answers "is the run-time type of this expression compatible with T" as a `bool`; it
  cannot declare a variable, so every positive `is` result is immediately followed by a redundant
  cast.
- There's no equivalent of a `when` guard; extra conditions have to be additional nested `if`s.

## Fallback

This is the first tier — there's nothing older to fall back to. On a project pinned below C# 7.0
(pre-2017 .NET Framework or an explicit `<LangVersion>` below `7`), the `as`-plus-null-check and
`if`/`else if` chain above are what pattern matching becomes. Once the target moves to C# 7.0+, see
[csharp7-is-and-switch-patterns.md](csharp7-is-and-switch-patterns.md) for the replacement.
