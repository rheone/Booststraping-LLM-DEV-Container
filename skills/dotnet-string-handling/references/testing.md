# Testing

String-building and span-parsing code has an observable, easily-asserted-on output, but a few
gotchas specific to this domain are easy to miss in an otherwise ordinary unit test.

## Testing StringBuilder-based construction

Assert on the final `ToString()` output directly — there's no need to inspect `StringBuilder`
internals or capacity:

```csharp
[Fact]
public void BuildSummary_MultipleItems_JoinsWithCommaSeparator()
{
    var items = new[] { "apple", "banana", "cherry" };

    string result = BuildSummary(items);

    result.Should().Be("apple, banana, cherry");
}
```

Add a boundary case for zero items and exactly one item — a hand-rolled loop appending a trailing
separator after every item (`sb.Append(item).Append(", ")`) is a common source of an off-by-one bug
that only shows up as a trailing separator on the last element, easy to miss if the test suite only
ever exercises the multi-item case.

## Testing culture-sensitive code under more than one culture

Any test asserting on culture-aware formatting or comparison behavior should explicitly set
(and restore) `CultureInfo.CurrentCulture` rather than relying on whatever culture the test runner
happens to execute under — a test that passes locally under `en-US` and fails in CI running under
`InvariantCulture` (or vice versa) is usually this exact gap:

```csharp
[Fact]
public void ToUpperInvariant_TurkishI_DoesNotProduceDottedCapital()
{
    var original = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
    try
    {
        "file".ToUpperInvariant().Should().Be("FILE"); // invariant, so unaffected by tr-TR
    }
    finally
    {
        CultureInfo.CurrentCulture = original;
    }
}
```

This is exactly the test shape that catches a culture-aware `ToUpper()`/ordinal-comparison mistake
(see [culture-and-ordinal-comparison.md](culture-and-ordinal-comparison.md)) before it reaches a
machine actually running under a non-`en-US`/non-invariant culture in production.

## Testing span-based parsing

Test a span-based parser the same way as a `string`-based one — pass ordinary `string` arguments
(which convert implicitly to `ReadOnlySpan<char>`) and assert on the parsed result — plus add cases
specific to slicing correctness: an empty input, a delimiter at position 0, a delimiter at the very
end, and no delimiter present at all, since off-by-one slicing errors on `Span<char>` ranges fail
silently (an incorrect range still often produces *some* valid substring, just the wrong one) rather
than throwing:

```csharp
[Theory]
[InlineData("a,b,c", 0, "a")]
[InlineData(",b,c", 0, "")]
[InlineData("a", -1, "a")] // no delimiter present
public void FirstField_ReturnsExpectedSlice(string input, int expectedCommaIndex, string expected)
{
    FirstField(input).Should().Be(expected);
}
```

## Testing raw string literal content

A raw string literal's leading-whitespace-stripping behavior is a compile-time transformation —
by the time the test runs, `"""..."""` is already an ordinary `string` value, so no special test
technique is needed beyond asserting on the resulting string's exact content, including verifying
the indentation stripping produced what was intended (a common mistake is a closing delimiter
indented inconsistently with the content, which changes how much leading whitespace gets stripped):

```csharp
[Fact]
public void JsonTemplate_HasNoLeadingIndentation()
{
    string json = """
        { "id": 1 }
        """;

    json.Should().Be("{ \"id\": 1 }");
}
```
