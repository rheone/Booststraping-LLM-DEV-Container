# Humanizer

Guidance on Humanizer, a third-party string/number/date/enum readability library for C#/.NET — the
routing table (by situation, not by Humanizer/C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per Humanizer/C# version

| File | Covers |
| --- | --- |
| `humanizing-values.md` | Humanize() on strings, numbers, DateTime/DateTimeOffset, TimeSpan, enums |
| `numbers-to-words.md` | ToWords(), ToOrdinalWords() |
| `pluralization.md` | Pluralize(), Singularize(), ToQuantity() |
| `ordinalize-and-truncate.md` | Ordinalize(), Truncate() and its truncator strategies |
| `localization.md` | Explicit CultureInfo vs. current thread culture, locale-specific packages |
| `testing-with-humanizer.md` | Asserting on Humanize() output deterministically, pinning culture, relative-time tests |

## Scope

Humanizer's extension-method API (`Humanizer`/`Humanizer.Core` packages) for converting raw values
into human-readable text. Out of scope: Humanizer's internal Roslyn analyzer/build tooling, and
general .NET globalization/ICU configuration beyond its direct effect on Humanizer's output.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
3.0.10, targeting net10.0/net8.0/net48/netstandard2.0.
