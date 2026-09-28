# Span-Based String Parsing

`ReadOnlySpan<char>` lets code slice, search, and parse a string's characters without allocating a
new `string` for every intermediate substring — `string` itself converts implicitly to
`ReadOnlySpan<char>`, so existing string-producing code can feed directly into span-based parsing
with no extra ceremony at the boundary.

## Slicing without `Substring`

```csharp
string input = "2026-09-25T14:30:00";
ReadOnlySpan<char> span = input;

ReadOnlySpan<char> datePart = span[..10];      // "2026-09-25" — no allocation
ReadOnlySpan<char> timePart = span[11..];      // "14:30:00" — no allocation
```

`string.Substring` allocates a brand-new string for every call; the range-indexer slice above
(`span[..10]`) produces a `ReadOnlySpan<char>` that just points into the original string's existing
memory with an adjusted start/length — no copy, no allocation, at the cost of the slice being a
`ref struct` that can't outlive the original string's accessibility or be stored on the heap (in a
field of a non-ref-struct class, captured in a lambda closure, etc.).

## Searching and splitting

`IndexOf`, `LastIndexOf`, `Contains`, `StartsWith`, `EndsWith`, and `SequenceEqual` are all
available directly on spans with the same semantics as their `string` counterparts:

```csharp
ReadOnlySpan<char> csvLine = "id,name,email";
int firstComma = csvLine.IndexOf(',');
ReadOnlySpan<char> firstField = csvLine[..firstComma];
```

For repeated field-by-field parsing, `MemoryExtensions.Split`/the `SpanSplitEnumerator` returned by
`span.Split(',')` walks fields one at a time without allocating an array of substrings the way
`string.Split` does:

```csharp
foreach (var range in csvLine.Split(','))
{
    ReadOnlySpan<char> field = csvLine[range];
    // process field without ever allocating a separate string for it
}
```

## Parsing numbers and other values directly from a span

`int.Parse`/`TryParse` (and the equivalent members on other numeric and `DateTime`/`Guid` types)
have `ReadOnlySpan<char>` overloads that parse directly from a slice with no intermediate substring
allocation:

```csharp
ReadOnlySpan<char> priceText = line[priceStart..priceEnd];
if (decimal.TryParse(priceText, NumberStyles.Currency, CultureInfo.InvariantCulture, out var price))
{
    // ...
}
```

This is the single most common win in a hot parsing loop: replacing
`decimal.Parse(line.Substring(priceStart, priceEnd - priceStart))` (one allocation per field, per
line) with a span slice plus a span-accepting `TryParse` overload (zero allocations).

## `stackalloc` for small, short-lived working buffers

For a small, fixed-upper-bound working buffer needed only for the duration of one method call
(formatting a small number of characters, building a short key), `stackalloc char[N]` avoids a heap
allocation entirely:

```csharp
Span<char> buffer = stackalloc char[16];
if (value.TryFormat(buffer, out int written, "D8"))
{
    ReadOnlySpan<char> formatted = buffer[..written];
}
```

Keep `stackalloc` sizes small and bounded by a compile-time-known or safely-capped constant — an
unbounded or attacker-influenced size risks a stack overflow, which is why every BCL `TryFormat`-
style API is designed around a caller-supplied, caller-owned buffer rather than allocating its own.

## When a span-based rewrite isn't worth it

A parsing routine that runs rarely, or handles small enough input that a handful of `Substring`
allocations are immaterial, doesn't need this treatment — reach for span-based parsing when
profiling or a known-hot path (a tight parsing loop over a large file, a request-path parser
running per request at high throughput) actually shows string-allocation pressure, not as a
reflexive first draft for every string-parsing method.
