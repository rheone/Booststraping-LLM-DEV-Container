---
name: dotnet-string-handling
description: 'Guidance on string handling and StringBuilder in .NET — System.String immutability and its performance implications, StringBuilder usage and capacity tuning, string interpolation and custom interpolated string handlers (verified: custom interpolated string handlers via InterpolatedStringHandlerAttribute shipped in C# 10 / .NET 6), raw string literals (verified: C# 11 / .NET 7), Span<char>/ReadOnlySpan<char>-based parsing and slicing without allocation, and culture-aware vs. ordinal string comparison pitfalls. Use when choosing between string concatenation and StringBuilder, tuning a StringBuilder''s initial capacity, writing a performance-sensitive interpolated string or a custom interpolated string handler, choosing raw string literals for embedded quotes/JSON/regex text, parsing or slicing a string without allocating substrings, or picking StringComparison/culture for an equality check or sort.'
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# String Handling

Guidance on `System.String`, `StringBuilder`, and the allocation-free `Span<char>`/
`ReadOnlySpan<char>` APIs for string-heavy code in .NET. Organized by task, not by C# version —
each reference file notes a version-introduced fact inline where it matters (custom interpolated
string handlers, raw string literals).

## Quick start

```csharp
// Immutable: every operation on a string produces a new string.
string greeting = "Hello, " + name + "!"; // fine for a one-off, occasional concatenation

// Many concatenations in a loop: use StringBuilder, sized up front when the size is known.
var sb = new StringBuilder(capacity: expectedLength);
foreach (var item in items)
{
    sb.Append(item.Name).Append(", ");
}

// Allocation-free parsing over a span instead of Substring.
ReadOnlySpan<char> span = input;
var firstComma = span.IndexOf(',');
ReadOnlySpan<char> firstField = span[..firstComma];

// Ordinal, not culture-aware, for anything that isn't user-facing text.
if (string.Equals(userId, storedId, StringComparison.Ordinal)) { /* ... */ }
```

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Deciding whether immutability makes a piece of string code slower than it needs to be | [references/string-immutability-and-performance.md](references/string-immutability-and-performance.md) |
| Building a string incrementally, sizing `StringBuilder`'s capacity, chaining `Append` | [references/stringbuilder.md](references/stringbuilder.md) |
| Writing an interpolated string, or a custom interpolated string handler for a hot path | [references/string-interpolation-and-handlers.md](references/string-interpolation-and-handlers.md) |
| Embedding quotes, JSON, regex, or other quote-heavy text as a literal | [references/raw-string-literals.md](references/raw-string-literals.md) |
| Parsing or slicing a string without allocating substrings | [references/span-based-string-parsing.md](references/span-based-string-parsing.md) |
| Choosing `StringComparison`/culture for equality, sorting, or case conversion | [references/culture-and-ordinal-comparison.md](references/culture-and-ordinal-comparison.md) |
| Testing string-building or span-parsing code | [references/testing.md](references/testing.md) |

## Out of scope

- Regular expressions (`System.Text.RegularExpressions`) as a technique in their own right —
  [references/raw-string-literals.md](references/raw-string-literals.md) covers only how raw
  string literals make a regex pattern's textual representation less escape-heavy, not regex syntax
  or engine behavior itself.
- Text encoding conversion (`Encoding`, UTF-8 byte-level APIs) beyond what
  [references/span-based-string-parsing.md](references/span-based-string-parsing.md) touches for
  `char`-based spans — byte-oriented UTF-8 parsing/formatting APIs are a distinct surface not
  covered here.
- Globalization/resource-localization strategy (`.resx`, `IStringLocalizer`) — out of scope beyond
  the comparison/casing pitfalls in
  [references/culture-and-ordinal-comparison.md](references/culture-and-ordinal-comparison.md).
