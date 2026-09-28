# Testing Code That Uses Immutable Collections

## Assert the original instance is unaffected

The single most valuable assertion specific to these types: after a mutating-looking call, verify
the *original* reference is unchanged, alongside asserting the new result is correct. This is exactly
the property that's easy to accidentally rely on incorrectly (assuming `Add` mutates in place) and
easy to verify directly:

```csharp
[Fact]
public void Add_ReturnsNewInstance_LeavesOriginalUnchanged()
{
    ImmutableList<int> original = ImmutableList.Create(1, 2, 3);

    ImmutableList<int> updated = original.Add(4);

    Assert.Equal(new[] { 1, 2, 3 }, original);   // original is untouched
    Assert.Equal(new[] { 1, 2, 3, 4 }, updated); // updated reflects the change
    Assert.NotSame(original, updated);
}
```

## Assert on contents, not reference identity, for equal-but-distinct instances

Two immutable collections with the same elements are generally *not* reference-equal (structural
sharing means shared substructure, not a global "same contents means same instance" cache), so
compare contents with sequence-equality assertions (`Assert.Equal` on a collection, or your test
framework's collection-equality helper) rather than `Assert.Same`/reference equality unless the test
is specifically about instance identity:

```csharp
ImmutableList<int> a = ImmutableList.Create(1, 2, 3);
ImmutableList<int> b = ImmutableList.Create(1, 2, 3);

Assert.Equal(a, b);      // true: same contents
Assert.NotSame(a, b);    // true: distinct instances
```

## Testing a type that exposes an immutable snapshot

For a class that exposes its internal state as an immutable collection (the `OrderBatch` shape from
the quick-start example in [SKILL.md](../SKILL.md)), test that mutating methods return a new instance
of the *containing* type and that earlier instances remain valid and unaffected — the same
copy-on-write contract extends one level up to the wrapping type:

```csharp
[Fact]
public void WithOrder_ReturnsNewBatch_LeavesOriginalBatchUnaffected()
{
    OrderBatch original = OrderBatch.Create(new[] { order1 });

    OrderBatch updated = original.WithOrder(order2);

    Assert.Single(original.Orders);
    Assert.Equal(2, updated.Orders.Count);
}
```

## Testing builder-based batch construction

Assert on the finished `ToImmutable()` result's contents — the builder itself is an implementation
detail of how the result was constructed, not something a caller-facing test should need to inspect
directly:

```csharp
[Fact]
public void Create_BuildsListMatchingAllSeedItems()
{
    OrderBatch batch = OrderBatch.Create(new[] { order1, order2, order3 });

    Assert.Equal(3, batch.Orders.Count);
    Assert.Contains(order2, batch.Orders);
}
```

## Gotchas specific to testing this kind of code

- **A forgotten return-value assignment produces a passing-looking test for the wrong reason.**
  `list.Add(item);` without capturing the result compiles fine and the test can still pass if it then
  asserts against a variable that was reassigned elsewhere — always assert against the exact reference
  the method under test actually returned, not a variable you assume was updated in place.
- **`default(ImmutableArray<T>)` is not the same as `ImmutableArray<T>.Empty`.** A test fixture that
  declares an `ImmutableArray<T>` field without initializing it gets the uninitialized default (a
  `null`-backed struct), which throws on most member access — initialize test fixtures explicitly with
  `.Empty` or a `Create` call.

## Most likely scenarios

1. **Verifying a mutating-looking operation didn't mutate the original** — the core immutability
   contract itself, worth a direct test wherever a codebase relies on it for correctness (e.g. safe
   concurrent reads).
2. **Verifying a wrapping type's own copy-on-write behavior** — that a domain type built around an
   immutable collection correctly produces new instances of itself rather than mutating shared state.
3. **Verifying batch-construction logic via a builder** produces the expected final contents,
   independent of how many intermediate steps the builder went through to get there.
