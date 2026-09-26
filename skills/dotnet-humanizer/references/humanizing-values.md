# Humanizing values

`Humanize()` is an extension method with a different overload — and different output shape —
depending on the type it's called on.

## Strings

Splits an identifier-style string (PascalCase, camelCase, or one already separated by underscores)
into normally-spaced, capitalized words.

```csharp
"PascalCaseValue".Humanize();       // "Pascal case value"
"a_snake_case_value".Humanize();    // "a snake case value"
"HTML".Humanize();                  // "HTML" (all-caps acronym left intact)
```

`Humanize(LetterCasing.Title)` (and the other `LetterCasing` values) controls the resulting casing
explicitly rather than accepting whatever casing the split naturally produces:

```csharp
"PascalCaseValue".Humanize(LetterCasing.Title); // "Pascal Case Value"
```

## Numbers

`Humanize()` on an `int` produces a written-out cardinal number using the same underlying
number-to-words engine as `ToWords()` (see
[references/numbers-to-words.md](numbers-to-words.md)) — the two exist because `Humanize()` fits
the same verb used across every type this file covers, while `ToWords()` reads clearly at a call
site that's specifically about spelling out a number.

## `TimeSpan`

Produces a relative, rounded-to-the-largest-unit phrase rather than an exact duration string.

```csharp
TimeSpan.FromSeconds(1).Humanize();     // "1 second"
TimeSpan.FromMinutes(90).Humanize();    // "an hour"
TimeSpan.Zero.Humanize();               // "0 seconds"
```

`Humanize(precision: 2)` includes more than one unit in the output when the value doesn't reduce
cleanly to a single one:

```csharp
TimeSpan.FromMinutes(95).Humanize(precision: 2); // "1 hour, 35 minutes"
```

`TimeSpan.Humanize()` describes a *duration* ("an hour"), not a point in time relative to now — for
"how long ago," use `DateTime`/`DateTimeOffset.Humanize()` below.

## `DateTime` / `DateTimeOffset`

Produces relative-to-now phrasing by comparing the value against the current moment at the time
`Humanize()` is called.

```csharp
DateTime.UtcNow.AddDays(-1).Humanize();     // "yesterday"
DateTime.UtcNow.AddHours(-3).Humanize();    // "3 hours ago"
DateTime.UtcNow.AddDays(2).Humanize();      // "2 days from now"
```

An overload accepts an explicit reference point instead of "now," which matters for both testability
(see [references/testing-with-humanizer.md](testing-with-humanizer.md)) and correctness when the
comparison needs to be relative to something other than the wall-clock moment the call happens to
execute at:

```csharp
someTimestamp.Humanize(dateToCompareAgainst: referenceInstant);
```

## Enums

Produces spaced-out, capitalized text from the enum member's name, or the text from a
`[Description]` attribute on the member when one is present — giving a display-friendly override
without renaming the member itself.

```csharp
public enum OrderStatus
{
    [System.ComponentModel.Description("Awaiting payment")]
    PendingPayment,
    Shipped,
}

OrderStatus.PendingPayment.Humanize(); // "Awaiting payment"
OrderStatus.Shipped.Humanize();        // "Shipped"
```

Use the `[Description]` override for any enum member whose display text needs to diverge from what
splitting its C# identifier would naturally produce, rather than renaming the member (which changes
its wire/serialized name too) just to change its humanized text.
