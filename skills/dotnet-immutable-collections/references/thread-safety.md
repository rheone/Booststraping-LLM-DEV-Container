# Thread Safety

## What's actually guaranteed

Every immutable collection instance is safe to read from any number of threads concurrently, with no
locking required, for exactly one reason: once constructed, its contents never change. There's no
"read while a write is happening" race to guard against, because there's no write that mutates an
existing instance at all — every apparent mutation produces a distinct new instance instead.

This makes immutable collections a natural fit for sharing state across threads without
synchronization: publish an `ImmutableList<T>` (or any of the other three types) to multiple threads
via a field, and every thread can enumerate, index into, or query it freely without a lock.

## What's not guaranteed: the "current" reference itself

The immutability guarantee covers a single collection *instance* — it says nothing about a mutable
field or property that happens to *point at* one. If several threads read and write the same field
holding an `ImmutableList<T>` reference (e.g. implementing a "swap in a new version" pattern), that
field access itself is an ordinary shared-mutable-state problem and needs its own synchronization:

```csharp
private ImmutableList<int> _items = ImmutableList<int>.Empty;

// Not thread-safe on its own: two threads can both read the old _items,
// each add their own item, and one thread's update silently overwrites the other's.
public void Add(int item) => _items = _items.Add(item);
```

Fix this with `Interlocked.CompareExchange` in a retry loop (the standard lock-free pattern for
"swap in a new immutable snapshot"), or with an ordinary lock around the read-modify-write sequence:

```csharp
private ImmutableList<int> _items = ImmutableList<int>.Empty;

public void Add(int item)
{
    ImmutableList<int> before, after;
    do
    {
        before = _items;
        after = before.Add(item);
    }
    while (Interlocked.CompareExchange(ref _items, after, before) != before);
}
```

`ImmutableInterlocked` (in the same `System.Collections.Immutable` namespace) wraps exactly this
pattern for the common single-item operations, so the loop above can often be replaced with a single
call:

```csharp
ImmutableInterlocked.Update(ref _items, (list, item) => list.Add(item), item);
```

## Builders are the exception

A `Builder` (see [builders.md](builders.md)) is a genuinely mutable object and carries none of these
guarantees — it must not be shared across threads without external locking, even though the immutable
instance it eventually produces via `ToImmutable()` is safe to share freely once created.
