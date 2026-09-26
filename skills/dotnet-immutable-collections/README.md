# Immutable Collections

Guidance on `System.Collections.Immutable` — the routing table (by situation) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per concern

| File | Covers |
| --- | --- |
| `core-types.md` | `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableDictionary<TKey,TValue>`, `ImmutableHashSet<T>`; creation; structural sharing and its performance characteristics |
| `builders.md` | `ToBuilder()`/the `Builder` pattern for efficient batch mutation before finalizing with `ToImmutable()` |
| `choosing-a-collection.md` | `ImmutableArray<T>` vs `ImmutableList<T>` — value-type-like/small/infrequently-mutated vs tree-based/larger/frequently-mutated |
| `thread-safety.md` | what the immutability guarantee actually covers, `Interlocked`/`ImmutableInterlocked` for a shared mutable reference to an immutable instance |
| `interop-with-mutable-collections.md` | converting to/from `List<T>`, arrays, `Dictionary<TKey,TValue>`; API boundary design |
| `testing.md` | asserting original-instance immutability, testing a wrapping type's copy-on-write behavior, testing builder-based construction |

## Scope

`System.Collections.Immutable` (current stable release **10.0.9**, shipping alongside **.NET 10**,
part of the [dotnet/runtime](https://github.com/dotnet/runtime) repository). Covers the four core
immutable collection types, structural sharing and its performance implications, the builder pattern
for batch mutation, choosing between `ImmutableArray<T>` and `ImmutableList<T>`, thread-safety
guarantees, interop with mutable collections, and testing code built on these types.

Out of scope: read-only *views* over a still-mutable backing collection (`ReadOnlyCollection<T>`,
`.AsReadOnly()`), and `System.Collections.Frozen` (`FrozenDictionary<TKey,TValue>`/`FrozenSet<T>`) — a
separate, read-only-after-construction collection family optimized for lookup speed rather than
structural sharing. See [SKILL.md](SKILL.md) for the full out-of-scope list and rationale.

This skill is self-contained: it does not assume any other skill is installed.
