# Humanizer

Guidance on Humanizer, the .NET library for turning raw values (numbers, dates, TimeSpans, enum
members, PascalCase identifiers) into readable display text through a set of extension methods.

## When to reach for it

- Turning a number, date, `TimeSpan`, or enum member into human-readable display text with
  `Humanize()`.
- Converting a number into written-out words, or into an ordinal like "1st".
- Making a noun plural or singular, or producing "1 item" vs. "2 items" from a count.
- Truncating display text to a length without an awkward cutoff.
- Producing output in a language other than English, or deciding when to pass an explicit
  `CultureInfo` instead of relying on the current thread culture.

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or debugging
code that converts a raw value into human-readable text through Humanizer's extension methods. You
can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| Humanize() on strings, numbers, DateTime/DateTimeOffset, TimeSpan, enums | [references/humanizing-values.md](references/humanizing-values.md) |
| ToWords(), ToOrdinalWords() | [references/numbers-to-words.md](references/numbers-to-words.md) |
| Pluralize(), Singularize(), ToQuantity() | [references/pluralization.md](references/pluralization.md) |
| Ordinalize(), Truncate() and its truncator strategies | [references/ordinalize-and-truncate.md](references/ordinalize-and-truncate.md) |
| Explicit CultureInfo vs. current thread culture, locale-specific packages | [references/localization.md](references/localization.md) |
| Asserting on Humanize() output deterministically, pinning culture, relative-time tests | [references/testing-with-humanizer.md](references/testing-with-humanizer.md) |

## Example prompts

- "Turn this enum value into a friendly display label instead of showing the raw name."
- "Show '1 item' vs. '5 items' based on this count."
- "Make this Humanize() call deterministic in a unit test regardless of the machine's culture."
