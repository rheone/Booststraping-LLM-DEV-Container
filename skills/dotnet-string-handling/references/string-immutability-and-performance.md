# String Immutability and Performance

Every `System.String` instance is immutable once constructed — no API mutates a string in place.
Every operation that looks like it modifies a string (`+`, `Replace`, `Substring`, `ToUpper`,
`Trim`, ...) allocates and returns a new string, leaving the original untouched.

## What this costs

```csharp
string result = "";
foreach (var item in items) // items.Count == 10,000
{
    result += item.Name + ", "; // allocates a brand-new string on every iteration
}
```

Each `+=` allocates a new string sized to hold the entire accumulated result so far, then copies
the previous content into it plus the new piece — an O(n) copy on every iteration, making the whole
loop O(n²) in the number of items. This is the single most common string-performance mistake: it
compiles, it's correct, and it silently gets quadratically slower as the collection grows. Reach
for `StringBuilder` (see [stringbuilder.md](stringbuilder.md)) for any accumulation happening
inside a loop or recursive call.

## When plain concatenation is fine

A fixed, small number of concatenations — not repeated in a loop — is not a performance concern and
reads more clearly than a `StringBuilder` would:

```csharp
string fullName = $"{firstName} {lastName}"; // one concatenation, not looped — fine as-is
```

The compiler also constant-folds concatenation of compile-time-constant string literals into a
single literal with no runtime allocation at all (`"a" + "b"` becomes `"ab"` at compile time) — this
optimization does not apply once any operand is a runtime value.

## String interning

String literals are interned by the runtime — two identical literal strings appearing anywhere in
a compiled assembly typically refer to the same underlying string instance, so
`ReferenceEquals("abc", "abc")` returns `true` for literals. This is a runtime implementation detail
useful for understanding memory behavior, not something to rely on for equality checks —
always compare string content with `==`/`string.Equals`/`StringComparison`, never assume reference
equality holds for a computed (non-literal) string, even if it happens to hold in a specific build.

`string.Intern` explicitly interns a runtime-computed string into the intern pool; reach for it only
when profiling shows a genuinely large number of duplicate long-lived strings (e.g. deduplicating a
large in-memory set of repeated category names) — interning has a real cost of its own (the pool is
never garbage collected in the same way ordinary strings are) and is not a general-purpose
optimization to apply by default.

## `string.Create` for allocate-once, fill-in-place construction

For a hot path building a string of a known, computed length from pieces that aren't already
strings, `string.Create<TState>(length, state, Action<Span<char>, TState> action)` allocates the
final string exactly once and hands the callback a writable `Span<char>` to fill directly — no
intermediate `StringBuilder`, no intermediate substrings:

```csharp
string FormatCoordinate(int x, int y) =>
    string.Create(CultureInfo.InvariantCulture, stackalloc char[32], $"{x},{y}").ToString();
```

Reach for `StringBuilder` for anything where the final length isn't cheaply known up front or the
construction logic is non-trivial to express against a raw `Span<char>` callback; reach for
`string.Create` only once profiling identifies a specific hot path worth the extra complexity.

## Measuring before optimizing

String-performance intuition is frequently wrong at small scale — `+=` inside a loop that runs 5
times is not a problem worth restructuring, while the same pattern inside a loop over an unbounded
or large collection is. Profile or reason about actual iteration counts before reaching for
`StringBuilder`/`Span<char>` techniques that trade readability for performance; apply them where
the loop bound is large or unbounded, not reflexively everywhere a string is built.
