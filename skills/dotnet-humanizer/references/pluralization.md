# Pluralization and singularization

## `Pluralize()` / `Singularize()`

Convert an English (or active-culture) noun between singular and plural form, handling irregular
forms the naive "add an s" rule gets wrong.

```csharp
"item".Pluralize();      // "items"
"person".Pluralize();    // "people"
"analysis".Pluralize();  // "analyses"

"items".Singularize();   // "item"
"people".Singularize();  // "person"
```

Both methods assume the input is already unambiguously singular or plural respectively — calling
`Pluralize()` on a word that's already plural (or vice versa) can produce an incorrect double
transformation for some irregular words, so use these on a known-singular or known-cardinality
source (a type name, a known-singular configuration key), not on arbitrary free-text input of
unknown grammatical number.

## `PluralizeToVerb()`

Handles the parallel case for verbs agreeing with subject count, where relevant to generated text
("is" vs. "are", "has" vs. "have" style agreement) — a narrower, less commonly needed operation than
noun pluralization, reached for specifically when generating a full sentence whose verb must agree
with a pluralized subject.

## `ToQuantity()`

Combines a word with a count into properly pluralized display text in one call, instead of manually
branching on whether the count is exactly one.

```csharp
1.ToQuantity("item");   // "1 item"
5.ToQuantity("item");   // "5 items"
0.ToQuantity("item");   // "0 items"
```

`ToQuantity(word, ShowQuantityAs.Words)` spells out the count instead of using digits:

```csharp
3.ToQuantity("item", ShowQuantityAs.Words); // "three items"
```

Prefer `ToQuantity()` over manually pluralizing and interpolating the count — it centralizes the
"is the count exactly 1" branch that's otherwise easy to get wrong for the zero case (many naive
implementations incorrectly special-case only "1 item" vs. "N items" and mishandle "0 items" as a
result; `ToQuantity()` treats 0 as plural correctly by default).

## Type-name pluralization for generated identifiers

`Pluralize()`/`Singularize()` on a class or table name is a common source of an ORM/scaffolding
tool's table- or collection-naming convention (e.g. deriving a table name `"Orders"` from an entity
type `Order`) — when using it for this purpose, verify the specific irregular nouns your domain
actually contains (words like "data," "series," or domain-specific acronyms) produce the naming
convention you actually want, since a generic pluralization rule can't know a domain-specific
naming preference that intentionally departs from standard English grammar.
