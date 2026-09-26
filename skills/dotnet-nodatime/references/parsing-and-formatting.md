# Parsing and formatting

`NodaTime.Text` provides an immutable `*Pattern` type per core type, each built from a pattern text
and reused across many parse/format calls — construct a pattern once (e.g. as a `static readonly`
field) rather than rebuilding it per call.

## Pattern types

| Type | Pattern class |
| --- | --- |
| `LocalDate` | `LocalDatePattern` |
| `LocalTime` | `LocalTimePattern` |
| `LocalDateTime` | `LocalDateTimePattern` |
| `Instant` | `InstantPattern` |
| `ZonedDateTime` | `ZonedDateTimePattern` |
| `Offset` | `OffsetPattern` |
| `Duration` | `DurationPattern` |
| `Period` | (use `Period.ToString()`/`PeriodPattern.NormalizingIso` for ISO-8601 period text) |

## Formatting

Every core type also implements `ToString(string patternText, IFormatProvider? formatProvider)`
directly, which is convenient for one-off formatting, but a named `*Pattern` instance is cheaper to
reuse and reads more clearly at a call site that formats the same shape repeatedly.

```csharp
LocalDatePattern isoDate = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");
string text = isoDate.Format(dueDate);
```

```csharp
ZonedDateTimePattern pattern = ZonedDateTimePattern.CreateWithInvariantCulture(
    "yyyy-MM-dd HH:mm:ss x", DateTimeZoneProviders.Tzdb);
string text = pattern.Format(meeting);
```

## Parsing: `ParseResult<T>` instead of exceptions

`Pattern.Parse(text)` returns a `ParseResult<T>` — a value the caller inspects rather than a value
that throws on malformed input. This makes "was this input valid" an explicit branch rather than a
try/catch around a library call.

```csharp
ParseResult<LocalDate> result = isoDate.Parse(userInput);
if (result.Success)
{
    LocalDate parsed = result.Value;
}
else
{
    // result.Exception describes what went wrong, without having been thrown.
    LogInvalidInput(userInput, result.Exception.Message);
}
```

Use `.GetValueOrThrow()` on the result only at a boundary where malformed input truly is a bug (an
internal round-trip you control end-to-end), not for input arriving from outside the process.

## `ZonedDateTimePattern` needs a zone provider

Parsing a `ZonedDateTime` from text that names a zone by identifier requires the pattern to know
which provider resolves that identifier — pass the same provider used elsewhere in the application
(usually `DateTimeZoneProviders.Tzdb`) into `ZonedDateTimePattern.Create`/
`CreateWithInvariantCulture`, or parsing text with an unfamiliar zone identifier fails even though
the identifier is valid globally.

## Culture-sensitive vs. invariant patterns

`CreateWithInvariantCulture` fixes month/day names and separators to the invariant culture — the
right default for machine-readable formats (log lines, stored values, wire formats). Use
`Pattern.Create(patternText, cultureInfo)` only when the text is genuinely meant for display to a
user in their own locale, and be explicit about which `CultureInfo` that is rather than relying on
the current thread's culture, which can vary by environment in ways a formatting bug won't
surface until it hits a different machine.
