# List and Slice Patterns in Depth

Builds on [csharp11-list-and-slice-patterns.md](../references/csharp11-list-and-slice-patterns.md)
for the base syntax and the countable/indexable/sliceable requirements. This file works through
less commonly known combinations: nesting other pattern kinds per element, slicing with a nested
shape constraint, and jagged/nested list patterns.

## Basic: element-wise patterns other than a literal or `var`

```csharp
public static string ClassifyScores(int[] scores) => scores switch
{
    [] => "no scores",
    [< 60] => "single failing score",
    [>= 60, >= 60] => "two passing scores",
    [.. var all] when all.All(s => s >= 90) => "all honors",
    _ => "mixed",
};
```

Every position inside `[...]` accepts any pattern this skill covers, not just a literal or `var` —
relational patterns (`< 60`), property patterns, nested positional patterns, and further nested
list patterns are all valid per-element patterns, exactly as they'd be valid anywhere else a
pattern is expected.

## Advanced: slicing with a shape constraint on the captured middle

```csharp
void Validate(int[] numbers)
{
    var result = numbers is [< 0, .. { Length: 2 or 4 }, > 0] ? "valid" : "not valid";
    Console.WriteLine(result);
}

Validate(new[] { -1, 0, 1 });     // "not valid" — middle slice has Length 1
Validate(new[] { -1, 0, 0, 1 });  // "valid" — middle slice has Length 2
```

`.. { Length: 2 or 4 }` combines a slice pattern with a *property* pattern on the slice itself — the
captured middle portion is tested against `{ Length: 2 or 4 }` without giving it a name, so this
reads as "some middle section whose length is 2 or 4," not "capture the middle section into a
variable." Nesting a property pattern directly on a slice like this is often clearer than capturing
the slice into a `var` and checking its length in a separate `when` guard.

## Advanced: nested list patterns for jagged data

```csharp
public static string DescribeGrid(int[][] rows) => rows switch
{
    [] => "empty grid",
    [[var only]] => $"single cell: {only}",
    [[var a, var b], [var c, var d]] => $"2x2 grid: {a} {b} / {c} {d}",
    [var first, ..] when first.Length == 0 => "first row is empty",
    _ => "irregular grid",
};
```

A list pattern's nested patterns can themselves be list patterns — `[[var a, var b], [var c, var
d]]` matches a two-row, two-column jagged array in one pattern, recursing the same way a property
pattern can nest another property pattern.

## Advanced: list patterns as a `when`-guard-free replacement for manual bounds checks

```csharp
public static (int Verb, int[] Args) ParseTokens(string[] tokens) => tokens switch
{
    [var verb, .. var args] when int.TryParse(verb, out int v) => (v, Array.ConvertAll(args, int.Parse)),
    _ => throw new FormatException($"Unrecognized token sequence: {string.Join(' ', tokens)}"),
};
```

`[var verb, .. var args]` first guarantees the array has at least one element (matching zero
elements against this pattern fails outright — no `IndexOutOfRangeException` risk), *then* the
`when` guard runs `int.TryParse` only once bounds safety is already established by the pattern
itself. This ordering — pattern for shape, `when` for anything the pattern syntax can't express —
is the general division of labor between list patterns and guards; see
[pattern-combinators-nesting-and-when-clauses.md](pattern-combinators-nesting-and-when-clauses.md)
for more on that split.

## Fallback

Every example in this file needs C# 11+ and a .NET Standard 2.1+/.NET Core 3.0+ target (for
`System.Index`/`System.Range`), per
[csharp11-list-and-slice-patterns.md](../references/csharp11-list-and-slice-patterns.md#requirements-and-restrictions).
Below that, replace list/slice patterns with explicit `Length`/`Count` checks, indexed access, and
`Array.Copy`/`LINQ Skip`/`Take` for the "captured middle slice" scenarios — there's no compact
syntax equivalent, just more verbose manually-written bounds checks.
