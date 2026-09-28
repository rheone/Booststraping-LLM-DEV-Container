---
name: dotnet-humanizer
description: Guidance on Humanizer, a third-party string/number/date/enum readability library for C#/.NET (current stable release 3.0.10, targeting net10.0/net8.0/net48/netstandard2.0). Covers Humanize() overloads for strings, numbers, TimeSpan, DateTime/DateTimeOffset, and enums, ToWords()/ToOrdinalWords() for numbers, pluralization/singularization via Pluralize()/Singularize(), ordinalizing with Ordinalize(), string truncation with Truncate(), and localization via an explicit CultureInfo parameter or the current thread culture. Use when writing, reviewing, or debugging code that turns a raw value (a number, a date, a TimeSpan, an enum member, a PascalCase/camelCase identifier) into human-readable display text through Humanizer's extension methods.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Humanizer

Guidance on Humanizer, a third-party library for turning raw values into human-readable text in
.NET. Current stable release as of this writing: **3.0.10**, targeting
**net10.0**, **net8.0**, **net48**, and **netstandard2.0** (the last covering older target
frameworks and Roslyn analyzer/MSBuild task scenarios). The main `Humanizer` package bundles every
supported language's resources; `Humanizer.Core` is the English-only base package, with additional
`Humanizer.Core.<locale>` packages available individually. Organized by concern/topic — each
reference file notes a version-introduced fact inline rather than splitting files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Turning a number, date, TimeSpan, or enum member into display text | `Humanize()` overloads on `int`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Enum` | [references/humanizing-values.md](references/humanizing-values.md) |
| Converting a number to written-out words | `ToWords()`, `ToOrdinalWords()` | [references/numbers-to-words.md](references/numbers-to-words.md) |
| Making a noun plural/singular, or turning "1" into "one item" vs. "2 items" | `Pluralize()`, `Singularize()`, `ToQuantity()` | [references/pluralization.md](references/pluralization.md) |
| Producing an ordinal ("1st", "2nd") or truncating display text to a length | `Ordinalize()`, `Truncate()` | [references/ordinalize-and-truncate.md](references/ordinalize-and-truncate.md) |
| Producing output in a language other than English, or controlling which culture's rules apply | Explicit `CultureInfo` parameter vs. current thread culture, installing a locale-specific package | [references/localization.md](references/localization.md) |
| Unit testing code that produces human-readable output | Asserting on `Humanize()` output deterministically, pinning culture in tests, testing relative-time output against a fixed "now" | [references/testing-with-humanizer.md](references/testing-with-humanizer.md) |

## Quick start

```csharp
using Humanizer;

"PascalCaseValue".Humanize();                    // "Pascal case value"
1337.ToWords();                                   // "one thousand three hundred and thirty-seven"
TimeSpan.FromMinutes(90).Humanize();              // "an hour"
DateTime.UtcNow.AddDays(-1).Humanize();           // "yesterday"
DayOfWeek.Monday.Humanize();                      // "Monday"

"item".Pluralize();                               // "items"
5.ToQuantity("item");                             // "5 items"
1.Ordinalize();                                    // "1st"
"A long sentence that needs shortening".Truncate(10); // "A long...
```

Every `Humanize()`/`ToWords()`/`Ordinalize()`/`Pluralize()` call that involves language-specific
text accepts (directly or via an overload) an explicit `CultureInfo`; omitting it falls back to
`Thread.CurrentThread.CurrentUICulture` — see
[references/localization.md](references/localization.md) for when to rely on that default versus
passing a culture explicitly.

## Out of scope

- Roslyn analyzer/source-generator packages Humanizer ships for compile-time use (its
  `netstandard2.0` target covers this internally) — an implementation detail of the package's build
  tooling, not part of the runtime API this skill documents.
- General .NET globalization/ICU configuration (e.g. `System.Globalization.Invariant` mode, ICU vs.
  NLS data sources) beyond the specific interaction Humanizer's localization has with the running
  culture — a platform-level concern broader than this library's own API.
