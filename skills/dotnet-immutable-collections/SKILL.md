---
name: dotnet-immutable-collections
description: Guidance on System.Collections.Immutable — ImmutableList<T>, ImmutableArray<T>, ImmutableDictionary<TKey,TValue>, ImmutableHashSet<T>, and their structural-sharing performance characteristics; the Builder pattern (ToBuilder()) for efficient batch mutation before finalizing back to an immutable instance; choosing between ImmutableArray<T> (value-type-like, best for infrequently-mutated small collections) and ImmutableList<T> (tree-based, better for larger/frequently-mutated collections); the thread-safety guarantees these types provide; and interop with regular mutable collections (List<T>, Dictionary<TKey,TValue>, arrays). Use when a type needs a collection field/property that callers can't mutate out from under it, when sharing collection state safely across threads without locking, or when deciding between an immutable collection and a defensive copy or read-only wrapper.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Immutable Collections (System.Collections.Immutable)

Guidance on `System.Collections.Immutable` (current stable release: **10.0.9**, shipping alongside
**.NET 10**; part of the [dotnet/runtime](https://github.com/dotnet/runtime) repository). Every type
in this namespace is a persistent data structure: a mutating-looking operation (`Add`, `Remove`,
`SetItem`) never changes the existing instance — it returns a new instance that shares as much
internal structure as possible with the original, rather than copying the whole collection.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Learning `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableDictionary<TKey,TValue>`, `ImmutableHashSet<T>` and how structural sharing works | [references/core-types.md](references/core-types.md) |
| Doing several mutations in a row and want to avoid allocating a new immutable instance per step | [references/builders.md](references/builders.md) |
| Deciding whether a given collection field/property should be `ImmutableArray<T>` or `ImmutableList<T>` | [references/choosing-a-collection.md](references/choosing-a-collection.md) |
| Confirming what's actually thread-safe about these types (and what still isn't) | [references/thread-safety.md](references/thread-safety.md) |
| Converting to/from `List<T>`, arrays, `Dictionary<TKey,TValue>`, or accepting either an immutable or mutable collection at an API boundary | [references/interop-with-mutable-collections.md](references/interop-with-mutable-collections.md) |
| Testing code that exposes or consumes an immutable collection | [references/testing.md](references/testing.md) |

## Quick start

The most common shape — a type that exposes an immutable snapshot of its internal state so callers
can't mutate it, built up efficiently with a builder before being finalized:

```csharp
public sealed class OrderBatch
{
    private readonly ImmutableList<Order> _orders;

    private OrderBatch(ImmutableList<Order> orders) => _orders = orders;

    public static OrderBatch Create(IEnumerable<Order> seed)
    {
        ImmutableList<Order>.Builder builder = ImmutableList.CreateBuilder<Order>();
        foreach (Order order in seed)
        {
            builder.Add(order);
        }
        return new OrderBatch(builder.ToImmutable());
    }

    public IReadOnlyList<Order> Orders => _orders; // callers can read but never mutate this list

    public OrderBatch WithOrder(Order order) => new(_orders.Add(order)); // returns a new instance
}
```

`_orders` never changes after construction — `WithOrder` returns a brand-new `OrderBatch` wrapping a
new `ImmutableList<Order>`, and the original `OrderBatch` (and anyone still holding a reference to it)
is unaffected.

## Out of scope

- `IReadOnlyList<T>`/`IReadOnlyDictionary<TKey,TValue>` read-only *views* over a still-mutable backing
  collection (`ReadOnlyCollection<T>`, `.AsReadOnly()`) — these only hide mutation from the view's own
  API; the underlying collection can still change out from under a holder of the view, which is
  exactly the guarantee this skill's subject exists to provide instead.
- `System.Collections.Frozen` (`FrozenDictionary<TKey,TValue>`, `FrozenSet<T>`) — a separate,
  read-only-after-construction (not incrementally persistent) collection family optimized for
  build-once, read-many-times lookup speed rather than for structural sharing across successive
  versions.
