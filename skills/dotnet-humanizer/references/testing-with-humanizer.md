# Testing code that uses Humanizer

## How to test it

- **Pin `CultureInfo` explicitly in every test that asserts on humanized text.** Pass the culture
  argument directly rather than relying on `Thread.CurrentThread.CurrentUICulture`, whose value
  depends on the test runner's environment and can differ between a developer's machine and CI.

```csharp
[Fact]
public void Humanize_produces_expected_english_text()
{
    string result = "PascalCaseValue".Humanize(LetterCasing.Title);

    Assert.Equal("Pascal Case Value", result);
}
```

- **Pass an explicit reference point when testing relative-time output**, rather than asserting
  against whatever `DateTime.Humanize()` produces relative to the real "now" at test-run time — a
  test asserting `someTimestamp.Humanize() == "3 hours ago"` is only true for the few seconds around
  when it was written and becomes flaky (or silently wrong) afterward.

```csharp
[Fact]
public void Humanize_describes_time_relative_to_a_fixed_reference()
{
    var referenceNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    var timestamp = referenceNow.AddHours(-3);

    string result = timestamp.Humanize(dateToCompareAgainst: referenceNow);

    Assert.Equal("3 hours ago", result);
}
```

- **Assert on exact strings for a fixed culture, not on substrings or "contains" checks**, once the
  culture is pinned — Humanizer's output for a given input and culture is deterministic, so a full
  equality assertion catches a wording regression (e.g. a package upgrade changing "an hour" to "1
  hour") that a loose substring match would silently pass through.
- **Test `ToQuantity()`'s zero case explicitly.** The zero-vs-plural boundary is the case a
  hand-rolled pluralization branch most often gets wrong, and it's the case most worth a dedicated
  assertion when reviewing or writing code that calls `ToQuantity()`.

## Common scenarios

### Testing a display-formatting method that calls `Humanize()` internally

Treat the method under test as a pure function of its inputs plus an injected or parameterized
culture/reference-time — assert the full output string for a small table of representative inputs
(a recent time, a time far in the past, a time in the future, a zero-duration edge case) rather than
one single happy-path input.

### Testing enum-to-display-text mapping

For an enum whose members carry `[Description]` overrides, assert `Humanize()` against every member
in the enum (a theory/parameterized test iterating `Enum.GetValues<T>()`) so a newly added member
without a `[Description]` override doesn't silently fall through to the raw-identifier-splitting
default without anyone noticing.

### Testing pluralization/truncation used in a report or export

Assert on the exact generated line for a representative count (1, 0, and a typical plural value) and
for a string both under and over the truncation length, since both pluralization and truncation have
distinct behavior at their respective boundary values (the singular/plural threshold, the exact
truncation cutoff) that a single mid-range test case won't exercise.
