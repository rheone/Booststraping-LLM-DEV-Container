# String Handling

Guidance on `System.String`, `StringBuilder`, and allocation-free span-based string APIs in .NET —
the routing table (by task, not C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per C# version

| File | Covers |
| --- | --- |
| `string-immutability-and-performance.md` | Why every string operation allocates, and what that costs in a loop/hot path |
| `stringbuilder.md` | `StringBuilder` usage, chaining, capacity tuning, `ToString()` cost |
| `string-interpolation-and-handlers.md` | `$"..."` syntax, `DefaultInterpolatedStringHandler`, writing a custom `InterpolatedStringHandlerAttribute` type |
| `raw-string-literals.md` | `"""..."""` syntax, quote-heavy/JSON/regex text, interpolated raw strings |
| `span-based-string-parsing.md` | `ReadOnlySpan<char>` slicing/parsing without `Substring` allocations |
| `culture-and-ordinal-comparison.md` | `StringComparison`, `CultureInfo`, the Turkish-I problem, case-insensitive pitfalls |
| `testing.md` | Testing string-building code and span-based parsers |

## Scope

`System.String`/`StringBuilder`/`Span<char>` string handling only. Out of scope: regular
expression syntax/engine behavior beyond how raw string literals affect a pattern's textual form,
byte-level text encoding APIs, and globalization/resource-localization strategy beyond
comparison/casing pitfalls.

Each reference file notes a version-introduced fact inline (custom interpolated string handlers,
raw string literals); version is not the file-splitting axis for this skill (see
[SKILL.md](SKILL.md) for why).
