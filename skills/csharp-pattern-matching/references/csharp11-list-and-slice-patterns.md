# List and Slice Patterns (C# 11.0)

C# 11.0 shipped November 2022 with .NET 7 and added **list patterns** — matching an array or
list-like value against a sequence of nested patterns, one per element — plus the **slice
pattern** (`..`), which matches zero or more elements in the middle of a list pattern and can
optionally capture them. This tier also lets a constant `string` be matched against a
`Span<char>`/`ReadOnlySpan<char>` input, a related but separate small addition.

## Syntax

```csharp
int[] numbers = { 1, 2, 3 };

bool exact = numbers is [1, 2, 3];              // every element pinned
bool startsWith = numbers is [1, ..];            // first element pinned, rest ignored
bool captured = numbers is [var first, .., var last]; // first/last captured, middle ignored
```

## Basic use case

```csharp
public static string DescribeSequence(int[] values) => values switch
{
    [] => "empty",
    [var single] => $"single value {single}",
    [var first, var second] => $"pair {first}, {second}",
    [var first, .., var last] => $"starts {first}, ends {last}",
};
```

Each `[...]` list pattern matches the whole input sequence: `[]` matches a zero-length sequence,
`[var single]` matches exactly one element, and `[var first, .., var last]` matches three or more
elements while ignoring everything between the first and last capture.

## Advanced use case: capturing a slice and nesting patterns per element

```csharp
public static string ParseCommand(string[] tokens) => tokens switch
{
    ["move", var direction] => $"move {direction}",
    ["give", var item, "to", var target] => $"give {item} to {target}",
    ["look", .. var extras] when extras.Length > 0 => $"look at {string.Join(" ", extras)}",
    ["look"] => "look around",
    _ => "unrecognized command",
};

public static bool IsPalindromeShapedRun(int[] values) =>
    values is [var edge, .. var middle, var otherEdge] && edge == otherEdge;
```

`.. var extras` is a *slice pattern* that captures the matched middle elements into a new array
(`extras`), usable in a `when` guard or the arm's body. A slice pattern can appear at most once per
list pattern, but nested element patterns (a literal like `"move"`, a `var` capture, a relational
or property pattern per element) compose freely around it — list patterns are recursive patterns
like property and positional patterns.

## Requirements and restrictions

- The matched type must be *countable* (an accessible `Length` or `Count` property) and *indexable*
  (an accessible indexer taking `int` or `System.Index`); a slice pattern additionally needs either
  an indexer taking `System.Range` or an accessible `Slice(int, int)` method. Arrays, `List<T>`,
  and `string` all satisfy this out of the box.
- List patterns rely on `System.Index`/`System.Range`, which ship in .NET Standard 2.1 / .NET Core
  3.0 and later — **.NET Framework and classic .NET Standard 2.0 targets can't use list patterns
  even with `<LangVersion>11</LangVersion>` set**, unless a polyfill package supplies those types.
  Verify the target framework, not just the language version, before relying on this tier.
- At most one slice pattern per list pattern; the compiler resolves fixed-position patterns before
  and after it against the corresponding ends of the input sequence, with everything else absorbed
  into the slice.
- Deeper worked examples — combining list patterns with property patterns per element, and the
  test-assertion use of list patterns on collection results — are in
  [../specialized/list-and-slice-patterns-in-depth.md](../specialized/list-and-slice-patterns-in-depth.md).

## Fallback

Below C# 11.0, there's no list or slice pattern — use explicit `Length`/`Count` checks and indexed
access instead:

```csharp
public static string DescribeSequence(int[] values)
{
    if (values.Length == 0)
    {
        return "empty";
    }

    if (values.Length == 1)
    {
        return $"single value {values[0]}";
    }

    if (values.Length == 2)
    {
        return $"pair {values[0]}, {values[1]}";
    }

    return $"starts {values[0]}, ends {values[^1]}";
}
```

`values[^1]` (the C# 8 index-from-end operator) still works here even without list patterns — only
the pattern-matching *syntax* around it is unavailable below C# 11, not `System.Index` itself
(which needs .NET Standard 2.1+/.NET Core 3.0+ regardless of language version, per above). Every
other pattern kind is unchanged from
[csharp10-extended-property-patterns.md](csharp10-extended-property-patterns.md).
