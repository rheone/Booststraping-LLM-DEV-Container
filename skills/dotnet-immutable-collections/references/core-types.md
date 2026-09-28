# Core Types and Structural Sharing

## The four collections

| Type | Shape | Backing structure |
| --- | --- | --- |
| `ImmutableList<T>` | Ordered, index-accessible list | Balanced binary (AVL) tree |
| `ImmutableArray<T>` | Ordered, index-accessible list | A single wrapped array (a struct wrapping a `T[]`) |
| `ImmutableDictionary<TKey,TValue>` | Key/value map | Balanced binary tree keyed by hash |
| `ImmutableHashSet<T>` | Unique-value set | Balanced binary tree keyed by hash |

All four live in `System.Collections.Immutable` and implement the standard read-only collection
interfaces (`IReadOnlyList<T>`, `IReadOnlyDictionary<TKey,TValue>`, `IReadOnlyCollection<T>`) alongside
their own immutable-specific interfaces (`IImmutableList<T>`, `IImmutableDictionary<TKey,TValue>`,
`IImmutableSet<T>`).

## Creating one

Every type exposes a static `Create`/`CreateRange` factory plus a `.Empty` singleton to start from:

```csharp
ImmutableList<int> list = ImmutableList.Create(1, 2, 3);
ImmutableList<int> fromSeq = ImmutableList.CreateRange(sourceSequence);
ImmutableList<int> empty = ImmutableList<int>.Empty;

ImmutableArray<int> array = ImmutableArray.Create(1, 2, 3);
ImmutableDictionary<string, int> dict = ImmutableDictionary.Create<string, int>();
ImmutableHashSet<int> set = ImmutableHashSet.Create(1, 2, 3);
```

LINQ also provides `.ToImmutableList()`, `.ToImmutableArray()`, `.ToImmutableDictionary()`, and
`.ToImmutableHashSet()` extension methods over any `IEnumerable<T>`.

## Every mutating-looking call returns a new instance

`Add`, `Remove`, `SetItem`, `Insert`, and similar methods never modify the instance they're called on
— they return a new instance reflecting the change, leaving the original completely unchanged and
still valid to use:

```csharp
ImmutableList<int> original = ImmutableList.Create(1, 2, 3);
ImmutableList<int> withFour = original.Add(4);

// original is still { 1, 2, 3 } — Add did not mutate it.
Console.WriteLine(original.Count);  // 3
Console.WriteLine(withFour.Count);  // 4
```

Forgetting to capture the return value is the single most common mistake with these types — a bare
`list.Add(item);` statement silently does nothing observable, since the new list it produced was never
assigned anywhere.

## Structural sharing

For the tree-based types (`ImmutableList<T>`, `ImmutableDictionary<TKey,TValue>`,
`ImmutableHashSet<T>`), a mutating-looking operation only rebuilds the tree nodes on the path from the
root down to the changed node — every other subtree is shared by reference with the original
instance. This makes a single `Add`/`Remove`/`SetItem` call roughly O(log n) in both time and new
allocations, rather than O(n) to copy the whole collection.

`ImmutableArray<T>` has no tree to share — it wraps a single array, so any add/remove/set operation
allocates and copies an entirely new backing array (O(n)). Its efficiency instead comes from having
no per-element wrapper or tree-node overhead at all when the collection is read far more often than
it's changed (see [choosing-a-collection.md](choosing-a-collection.md)).

## Read performance

Indexed access on `ImmutableArray<T>` is a direct array index — as fast as a regular `T[]`, and
faster to iterate than any of the tree-based types, which pay tree-traversal cost per element.
Indexed access on `ImmutableList<T>` requires walking the tree to the nth node, making it O(log n)
rather than O(1) — a meaningful difference for tight loops doing repeated indexed access rather than
enumeration.
