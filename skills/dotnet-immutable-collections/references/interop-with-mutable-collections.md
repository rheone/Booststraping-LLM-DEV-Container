# Interop with Mutable Collections

## Converting from a mutable collection

LINQ-style extension methods build an immutable snapshot from any `IEnumerable<T>`:

```csharp
List<int> mutable = new() { 1, 2, 3 };

ImmutableList<int> list = mutable.ToImmutableList();
ImmutableArray<int> array = mutable.ToImmutableArray();
ImmutableDictionary<string, int> dict = source.ToImmutableDictionary(x => x.Key, x => x.Value);
ImmutableHashSet<int> set = mutable.ToImmutableHashSet();
```

Each of these copies the source's current contents at the moment of the call — later changes to the
original mutable collection have no effect on the immutable snapshot already taken.

## Converting back to a mutable collection

Every immutable type implements `IEnumerable<T>` (or `IEnumerable<KeyValuePair<TKey,TValue>>` for the
dictionary), so the standard LINQ/collection constructors work directly:

```csharp
List<int> backToMutable = immutableList.ToList();
int[] backToArray = immutableArray.ToArray(); // ImmutableArray<T> also exposes this directly, no LINQ needed
Dictionary<string, int> backToDict = new(immutableDictionary);
```

## Accepting either shape at an API boundary

For a method parameter, accept the narrowest interface that fits the actual usage rather than a
concrete immutable type, so callers aren't forced to convert a mutable collection they already have:

```csharp
// Accepts an ImmutableList<T>, a List<T>, an array, or anything else read-only-list-shaped.
public void Process(IReadOnlyList<Order> orders) { /* ... */ }
```

Reserve a concrete immutable-type parameter (`ImmutableList<Order>` specifically, rather than
`IReadOnlyList<Order>`) for the case where the method's own contract genuinely depends on the caller's
value never changing later — e.g. storing the reference for a background operation that must see a
frozen snapshot, not whatever the caller's mutable list contains by the time the background work
actually runs.

## Returning an immutable collection from a public member

Expose an immutable type (or its read-only interface) from a public property or return value instead
of a mutable collection when the point is to stop callers from mutating your internal state directly
— this replaces the older pattern of returning a defensive copy of a mutable collection (which costs
an O(n) copy on every single access) or a read-only wrapper like `.AsReadOnly()` (which still lets the
underlying collection change out from under a caller holding the wrapper, since the wrapper only
hides mutation from its own API surface, not from the original collection it wraps).
