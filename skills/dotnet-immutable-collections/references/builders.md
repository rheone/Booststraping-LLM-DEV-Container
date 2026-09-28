# Builders

## The problem builders solve

Calling `Add` in a loop against an immutable collection allocates a new instance on every single
call:

```csharp
// Avoid: allocates a new ImmutableList<T> instance on every iteration.
ImmutableList<int> list = ImmutableList<int>.Empty;
foreach (int value in source)
{
    list = list.Add(value); // O(log n) allocation, n times over
}
```

A `Builder` gives you a mutable staging area with the same logical contents, lets you perform every
step of a batch mutation against it directly, and produces one final immutable instance only when
you're done:

```csharp
ImmutableList<int>.Builder builder = ImmutableList.CreateBuilder<int>();
foreach (int value in source)
{
    builder.Add(value); // mutates the builder in place — no new immutable instance yet
}
ImmutableList<int> list = builder.ToImmutable();
```

## Getting a builder

Every immutable type exposes both a static factory and an instance method:

```csharp
ImmutableList<int>.Builder fromScratch = ImmutableList.CreateBuilder<int>();
ImmutableList<int>.Builder fromExisting = existingList.ToBuilder(); // seeds the builder with existing contents
```

`ToBuilder()`'s cost differs by type: on `ImmutableList<T>` it's O(1) — the builder can share the
existing tree structure and copy-on-write only the parts you actually change. On `ImmutableArray<T>`
it's O(n) — it copies every element into a fresh mutable array up front, since there's no tree to
share pieces of.

## Finalizing

`Builder.ToImmutable()` produces the finished immutable instance. Call it exactly once you're done
mutating — calling it mid-sequence and continuing to mutate the builder afterward is legal (the
builder keeps working) but produces a separate, independent immutable snapshot each time, which is
occasionally useful for taking periodic snapshots of in-progress work but wasteful if done every
iteration instead of once at the end.

## When a builder earns its cost

Reach for a builder specifically when doing **more than a couple of sequential mutations** to the
same logical collection — one or two `Add` calls directly against the immutable type is simpler code
and the allocation cost is negligible. The builder pays off once you're looping, batching, or
otherwise performing enough discrete mutation steps that per-step allocation would show up as real
overhead:

```csharp
// A single addition: no builder needed, the direct call is clearer.
ImmutableList<int> updated = list.Add(newValue);

// A batch of many additions: use a builder.
ImmutableList<int>.Builder builder = list.ToBuilder();
foreach (int value in manyNewValues)
{
    builder.Add(value);
}
ImmutableList<int> updated = builder.ToImmutable();
```

## Builders are not thread-safe

Unlike the immutable types themselves, a `Builder` is a genuinely mutable object with no thread-safety
guarantee — it's meant to be built up by a single thread (or under external synchronization) and then
handed off as a finished, safe-to-share immutable instance via `ToImmutable()`. Never share a builder
instance across threads without your own locking around it.
