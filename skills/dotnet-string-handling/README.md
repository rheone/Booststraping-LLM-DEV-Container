# String Handling

This skill covers string handling in .NET: `System.String` immutability and its performance cost,
`StringBuilder`, string interpolation and custom interpolated string handlers, raw string literals,
allocation-free span-based parsing, and culture-aware vs. ordinal comparison.

## When to reach for it

- Deciding between string concatenation and `StringBuilder` in a loop or hot path.
- Tuning a `StringBuilder`'s initial capacity to avoid repeated internal reallocation.
- Writing a performance-sensitive interpolated string, or a custom interpolated string handler for
  a hot logging or formatting path.
- Choosing raw string literals for text with embedded quotes, JSON, or regex patterns.
- Parsing or slicing a string without allocating substrings, using `ReadOnlySpan<char>`.
- Picking the right `StringComparison`/culture for an equality check or sort, and avoiding a
  culture-sensitive bug like the Turkish-I problem.

## Using it

This skill fires automatically when your request involves string performance, `StringBuilder`
usage, span-based parsing, or string comparison in .NET. You can also invoke it directly with
`/dotnet-string-handling`.

## What it covers

| Topic | Reference |
| --- | --- |
| Why every string operation allocates, and its cost in a hot path | [references/string-immutability-and-performance.md](references/string-immutability-and-performance.md) |
| `StringBuilder` usage, chaining, capacity tuning | [references/stringbuilder.md](references/stringbuilder.md) |
| `$"..."` interpolation, custom `InterpolatedStringHandlerAttribute` types | [references/string-interpolation-and-handlers.md](references/string-interpolation-and-handlers.md) |
| `"""..."""` raw string literals for quote-heavy/JSON/regex text | [references/raw-string-literals.md](references/raw-string-literals.md) |
| `ReadOnlySpan<char>` slicing/parsing without `Substring` allocations | [references/span-based-string-parsing.md](references/span-based-string-parsing.md) |
| `StringComparison`, `CultureInfo`, case-insensitive comparison pitfalls | [references/culture-and-ordinal-comparison.md](references/culture-and-ordinal-comparison.md) |
| Testing string-building code and span-based parsers | [references/testing.md](references/testing.md) |

## Example prompts

- "This method concatenates strings in a loop thousands of times. Should I switch to
  `StringBuilder`?"
- "Parse this comma-separated line into fields without allocating a substring per field."
- "Why does comparing these two strings give a different result on a Turkish locale machine?"
