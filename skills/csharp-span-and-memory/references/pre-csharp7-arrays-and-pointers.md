# Before `ref struct`: arrays, `ArraySegment<T>`, and `unsafe` pointers

Before C# 7.2, the language had no `ref struct` and the runtime had no `Span<T>`. A method that
needed to work on a contiguous slice of memory without copying it had exactly three options, each
with a real cost `Span<T>` later removed.

## The workaround patterns

**Pass the whole array plus offset/length by convention.**

```csharp
public static int Sum(byte[] buffer, int offset, int count)
{
    int total = 0;
    for (int i = offset; i < offset + count; i++)
    {
        total += buffer[i];
    }
    return total;
}
```

Nothing stops a caller from passing an `offset`/`count` pair that doesn't match `buffer`, and the
method can't express "a view over part of an array" as a single parameter type — every API that
wants slicing has to invent its own `offset, count` convention.

**`ArraySegment<T>`** (.NET Framework 2.0) packages that convention into a type:

```csharp
public static int Sum(ArraySegment<byte> segment)
{
    int total = 0;
    for (int i = segment.Offset; i < segment.Offset + segment.Count; i++)
    {
        total += segment.Array![i];
    }
    return total;
}
```

This is a real improvement — one parameter instead of three, and slicing (`new ArraySegment<byte>(buffer, 10, 20)`) is a constructor call. But `ArraySegment<T>` only ever wraps an array. It can't represent a slice of a `stackalloc` buffer, unmanaged memory, or a `string`'s characters, and being an
ordinary struct, it still has to be boxed or copied like any other value type has no special
compiler-enforced lifetime — nothing stops it outliving the array it points into if the array is
later resized... which arrays never are, but nothing in the *type* enforces the intent either.

**`unsafe` pointers** are the only way to touch stack memory or unmanaged memory directly, and the
only way to avoid `ArraySegment<T>`'s array-only limitation:

```csharp
public static unsafe int Sum(byte* buffer, int length)
{
    int total = 0;
    for (int i = 0; i < length; i++)
    {
        total += buffer[i];
    }
    return total;
}

// caller
unsafe
{
    byte* stackBuffer = stackalloc byte[256];
    int sum = Sum(stackBuffer, 256);
}
```

This works over any contiguous memory — stack, unmanaged, pinned managed — but requires an
`unsafe` context (and often `/unsafe` at the project level, which most codebases avoid enabling
project-wide), gives up all bounds checking, and can't safely represent a slice of a *movable*
managed array at all (the GC can relocate it mid-method unless it's pinned with `fixed`).

## What `Span<T>` later unified

`Span<T>` (see [csharp7.2-ref-struct-and-span.md](csharp7.2-ref-struct-and-span.md)) is the single
type that replaced all three: it wraps arrays, stack memory, and unmanaged memory behind one API,
carries its own bounds-checked length, and the compiler enforces its stack-only lifetime instead of
trusting convention.

## Fallback

This *is* the fallback — there is no earlier tier. On a target below C# 7.2 (or a runtime without
`Span<T>`/`Memory<T>` available, see the BCL-version note in
[csharp7.2-ref-struct-and-span.md](csharp7.2-ref-struct-and-span.md)), pick among these three based
on what the memory actually is: `ArraySegment<T>` for a slice of a managed array with no `unsafe`,
raw `offset`/`count` parameters when the type can't carry a struct, or `unsafe` pointers when the
memory isn't a movable managed array at all.
