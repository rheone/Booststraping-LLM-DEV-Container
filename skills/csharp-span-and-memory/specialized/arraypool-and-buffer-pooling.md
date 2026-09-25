# `ArrayPool<T>` and buffer-pooling patterns

`ArrayPool<T>` (`System.Buffers`) rents and returns arrays instead of allocating a fresh one per
call — the standard partner for `Span<T>`/`Memory<T>` code on a hot path, since a rented array's
backing memory is exactly what `.AsSpan()`/`.AsMemory()` then views without any further allocation.
It predates `Span<T>` itself (available since .NET Core 1.0 via the `System.Buffers` package, part
of the shared framework since .NET Core 3.1) but the two are used together constantly.

## Basic: rent, use, return, in a `try`/`finally`

```csharp
public static int ComputeChecksum(ReadOnlySpan<byte> data)
{
    byte[] scratch = ArrayPool<byte>.Shared.Rent(data.Length);
    try
    {
        data.CopyTo(scratch);
        return Checksum(scratch.AsSpan(0, data.Length));
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(scratch);
    }
}
```

`Rent(minimumLength)` returns an array **at least** `minimumLength` long — often longer, since the
pool buckets by power-of-two sizes. Always slice with the requested length
(`scratch.AsSpan(0, data.Length)`), never assume the rented array's own `.Length` equals what was
asked for.

## Basic: clearing sensitive data on return

```csharp
byte[] key = ArrayPool<byte>.Shared.Rent(32);
try
{
    LoadKeyMaterial(key.AsSpan(0, 32));
    UseKey(key.AsSpan(0, 32));
}
finally
{
    ArrayPool<byte>.Shared.Return(key, clearArray: true); // zero it before it goes back to the pool
}
```

`clearArray: true` is opt-in and costs a memset — reach for it specifically when the buffer held
data (cryptographic key material, credentials, PII) that must not linger in a pooled array another
caller might later rent and read before it's overwritten.

## Advanced: wrapping rent/return in a disposable `ref struct` to make leaks harder

```csharp
public ref struct PooledSpan<T>
{
    private T[] _rented;
    public Span<T> Span { get; }

    public PooledSpan(int minimumLength)
    {
        _rented = ArrayPool<T>.Shared.Rent(minimumLength);
        Span = _rented.AsSpan(0, minimumLength);
    }

    public void Dispose() // pattern-based Dispose — see csharp8-span-foreach-and-ranges.md
    {
        if (_rented is not null)
        {
            ArrayPool<T>.Shared.Return(_rented);
            _rented = null!;
        }
    }
}
```

```csharp
using var buffer = new PooledSpan<byte>(4096);
Fill(buffer.Span);
Process(buffer.Span);
// Dispose() runs automatically at scope exit — the array always goes back, even on an exception
```

Wrapping the rent/return pair in a `using`-compatible `ref struct` turns "remember to call Return in
a finally" into "the compiler enforces disposal the same way it does for any other `using`
resource" — see
[csharp8-span-foreach-and-ranges.md](../references/csharp8-span-foreach-and-ranges.md) for why a
`ref struct` can participate in `using` at all despite never implementing `IDisposable`.

## Advanced: avoiding the classic leak — returning the same array twice, or using it after return

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(256);
ArrayPool<byte>.Shared.Return(buffer);
// ArrayPool<byte>.Shared.Return(buffer); // BUG: double-return corrupts the pool's internal bookkeeping
// buffer[0] = 1; // BUG: use-after-return — another caller may already have rented this same array
```

`ArrayPool<T>` does not detect double-return or use-after-return — both are silent bugs that
corrupt shared state or produce data races with whatever other caller subsequently rents the same
backing array. The `try`/`finally` (or `using`-wrapped `ref struct`) pattern above is the practical
defense: return exactly once, on exactly one code path, and never touch the array or any `Span<T>`
derived from it afterward.

## Custom pools vs. `ArrayPool<T>.Shared`

`ArrayPool<T>.Shared` is a process-wide, thread-safe, general-purpose pool tuned for the common
case. Create a dedicated pool via `ArrayPool<T>.Create(maxArrayLength, maxArraysPerBucket)` instead
of the shared instance when a workload rents unusually large buffers or an unusually high volume
that would otherwise evict smaller, more common allocations from `Shared`'s internal buckets — a
narrow optimization, not a default choice.

## Fallback

`ArrayPool<T>` needs the `System.Buffers` package/shared-framework availability described above —
on a target without it, allocate arrays directly (`new byte[size]`) and accept the GC pressure, or
maintain a hand-rolled free-list of same-sized arrays behind a `ConcurrentBag<T>`/`ConcurrentStack<T>`,
which is substantively what `ArrayPool<T>.Shared` does internally, minus its size-bucketing.
