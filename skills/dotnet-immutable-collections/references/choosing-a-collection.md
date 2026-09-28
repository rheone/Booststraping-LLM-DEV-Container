# Choosing Between ImmutableArray<T> and ImmutableList<T>

Both implement the same ordered, index-accessible list shape, so the choice between them is purely
about the performance profile of how the collection will actually be used — not about API surface,
which is nearly identical between the two.

## ImmutableArray<T> — value-type-like, best for infrequent mutation

`ImmutableArray<T>` is a struct wrapping a single `T[]`. That gives it:

- **Fastest possible iteration and indexed read** — no tree traversal, no per-node overhead; behaves
  like a regular array for reads.
- **Lower memory footprint per collection** — one array allocation, no tree-node wrapper objects.
- **O(n) mutation** — every `Add`/`Remove`/`SetItem` call copies the entire backing array into a new
  one, because there's no shareable substructure smaller than the whole array.
- **A default (uninitialized) value that behaves differently from empty** — `default(ImmutableArray<T>)`
  wraps a `null` array internally and throws on most operations; always initialize with
  `ImmutableArray<T>.Empty` or a `Create`/`CreateRange` call rather than relying on a field's default.

Best fit: collections that are read far more often than they're changed, and collections that are
small or change infrequently enough that the O(n) copy cost of a mutation never becomes a hot path —
a fixed set of configuration values, a small lookup table computed once and read repeatedly, a
public API surface where read performance matters more than mutation convenience.

## ImmutableList<T> — tree-based, better for frequent or larger-scale mutation

`ImmutableList<T>` is a balanced binary tree. That gives it:

- **O(log n) mutation** via structural sharing — an `Add`/`Remove`/`SetItem` call only rebuilds the
  tree path to the changed node, sharing every other subtree with the original instance (see
  [core-types.md](core-types.md)).
- **O(1) `ToBuilder()`** — a builder can share the existing tree and only copy-on-write the parts it
  actually touches (see [builders.md](builders.md)), unlike `ImmutableArray<T>.ToBuilder()`'s O(n)
  full copy.
- **Slower iteration and indexed read than `ImmutableArray<T>`** — every element access pays
  tree-traversal cost.

Best fit: collections that change often (an evolving list built up across many operations, a
collection passed through a chain of transformations that each add or remove a handful of items), or
collections large enough that `ImmutableArray<T>`'s O(n) copy-per-mutation would become measurably
expensive.

## The decision in one line

If mutation is rare and the collection is small, reach for `ImmutableArray<T>` for its faster reads
and lower footprint. If mutation is frequent or the collection is large, reach for `ImmutableList<T>`
for its O(log n) mutation via structural sharing — and reach for a `Builder` on either type once a
single logical update involves more than a couple of sequential mutation steps.
