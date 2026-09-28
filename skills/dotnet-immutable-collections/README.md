# Immutable Collections

This skill covers `System.Collections.Immutable`: `ImmutableList<T>`, `ImmutableArray<T>`,
`ImmutableDictionary<TKey,TValue>`, `ImmutableHashSet<T>`, the builder pattern for batch mutation,
and choosing the right immutable type for a given access pattern.

## When to reach for it

- A type needs a collection field or property that callers can't mutate out from under it.
- Sharing collection state safely across threads without taking a lock.
- Deciding between `ImmutableArray<T>` (value-type-like, best for small/infrequently-mutated
  collections) and `ImmutableList<T>` (tree-based, better for larger/frequently-mutated ones).
- Batch-building an immutable collection efficiently instead of paying the copy cost of many
  individual `Add` calls.
- Deciding between an immutable collection and a defensive copy or read-only wrapper at an API
  boundary.

## Using it

This skill fires automatically when your request involves `System.Collections.Immutable` types or
choosing an immutable collection for thread-safe or defensive-copy scenarios. You can also invoke
it directly with `/dotnet-immutable-collections`.

## What it covers

| Topic | Reference |
| --- | --- |
| The four core immutable types, creation, structural sharing | [references/core-types.md](references/core-types.md) |
| `ToBuilder()`/`Builder` pattern for efficient batch mutation | [references/builders.md](references/builders.md) |
| `ImmutableArray<T>` vs. `ImmutableList<T>`: which one fits | [references/choosing-a-collection.md](references/choosing-a-collection.md) |
| What the immutability guarantee actually covers thread-safety-wise | [references/thread-safety.md](references/thread-safety.md) |
| Converting to/from `List<T>`, arrays, `Dictionary<TKey,TValue>` | [references/interop-with-mutable-collections.md](references/interop-with-mutable-collections.md) |
| Testing immutability and copy-on-write/builder-based construction | [references/testing.md](references/testing.md) |

## Example prompts

- "Should this public property expose a `List<T>` or an `ImmutableList<T>` to callers?"
- "I need to build up a large immutable collection in a loop without a full copy on every add."
- "Multiple threads read this shared collection. Do I need a lock if I switch it to
  `ImmutableDictionary`?"
