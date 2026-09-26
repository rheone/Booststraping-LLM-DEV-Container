# Culture and Ordinal Comparison

Every string comparison, sort, and case-conversion API in .NET has both a culture-aware and an
ordinal (raw UTF-16 code-unit) form, and picking the wrong one is a recurring, hard-to-notice
source of bugs — it usually still "works" in the developer's own locale and only breaks for users
running a different culture, or breaks silently in a way tests running under the invariant/en-US
culture never catch.

## The default is culture-aware, and that's usually wrong for non-linguistic text

```csharp
"file.TXT".Equals("file.txt", StringComparison.OrdinalIgnoreCase); // correct: file extension, not language text
string.Compare(userInputA, userInputB);                            // culture-aware by default — often wrong here
```

Use **ordinal** comparison (`StringComparison.Ordinal`/`OrdinalIgnoreCase`) for anything that isn't
natural-language text a human reads: identifiers, file paths, URLs, protocol strings, dictionary
keys, security-sensitive comparisons (tokens, hashes, passwords), file extensions, and enum-like
string constants. Use **culture-aware** comparison only for text actually meant to sort or compare
the way a human reader of a specific language/locale would expect (sorting a list of display names
for that locale's users, for instance).

## The Turkish-I problem

The canonical example of why culture-aware `ToUpper`/`ToLower` is dangerous for non-linguistic
text: under the Turkish (`tr-TR`) culture, lowercase `i` uppercases to `İ` (dotted capital I, not
`I`), and uppercase `I` lowercases to `ı` (dotless lowercase i, not `i`) — because Turkish has two
distinct pairs of I/i characters. Code that does `id.ToUpper() == "FILE"` for a non-linguistic
identifier comparison breaks specifically on a machine running under the Turkish culture, and
nowhere else — exactly the kind of bug that survives testing in one locale and fails in production
for a specific subset of users or servers.

```csharp
"file".ToUpper();               // culture-aware — "FİLE" under tr-TR, not "FILE"
"file".ToUpperInvariant();      // always "FILE", regardless of the running culture
```

Use `ToUpperInvariant`/`ToLowerInvariant` (or, more directly, `StringComparison.OrdinalIgnoreCase`
for the comparison itself rather than normalizing case first) for any casing operation on
non-linguistic text.

## `StringComparison.Ordinal` vs. `InvariantCulture` vs. the current culture

| Comparison | Behavior | Use for |
| --- | --- | --- |
| `Ordinal`/`OrdinalIgnoreCase` | Raw UTF-16 code-unit comparison, fastest, culture-independent | Identifiers, paths, URLs, protocol strings, security-sensitive values |
| `InvariantCulture`/`InvariantCultureIgnoreCase` | Culture-aware rules, but fixed regardless of the running machine's culture | Persisted/round-tripped data that needs consistent linguistic sort order across machines (rare) |
| `CurrentCulture`/`CurrentCultureIgnoreCase` (the default for `==`, `CompareTo`, `string.Compare` with no explicit comparison) | Culture-aware, varies by the running thread's culture | User-facing display sorting/searching, where matching the current user's locale expectations is the actual goal |

`==` on two strings and `string.Equals(a, b)` with no explicit `StringComparison` both use ordinal
comparison already (an exception to the "culture-aware by default" framing above, and worth knowing
explicitly) — the culture-aware default applies to `string.Compare`, `CompareTo`, and sorting APis,
not to plain equality via `==`. Still pass `StringComparison.Ordinal` explicitly at any equality
call site where the intent needs to be unambiguous to a future reader, even though `==` already
behaves that way.

## Sorting

`Array.Sort`/`List<T>.Sort`/`OrderBy` on strings use `Comparer<string>.Default`, which is
culture-aware (current-culture) unless told otherwise — for a stable, machine-consistent sort order
(a persisted index, a cache key ordering, anything that must sort identically regardless of the
process's culture), sort with `StringComparer.Ordinal` explicitly:

```csharp
names.Sort(StringComparer.Ordinal);
var sorted = items.OrderBy(i => i.Key, StringComparer.Ordinal);
```

## Dictionary/HashSet keys

`Dictionary<string, T>`/`HashSet<string>` use ordinary `GetHashCode`/`Equals` by default, which for
`string` is ordinal — this is almost always the right default for keys, and is called out here only
because passing an explicit `StringComparer.OrdinalIgnoreCase` (or, more rarely, a culture-aware
comparer) as the constructor's `IEqualityComparer<string>` argument is the correct way to get
case-insensitive dictionary lookups, rather than normalizing every key's case by hand before every
insert/lookup.
