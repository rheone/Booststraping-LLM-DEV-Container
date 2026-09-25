# Pattern-based disposal for `ref struct`s, and range/`Index` slicing (C# 8.0 / .NET Core 3.0, September 2019)

C# 8.0 shipped September 2019 alongside .NET Core 3.0. Two additions matter directly for
`Span<T>`/`ref struct` code: a `ref struct` can now participate in `using` even though it can
never implement `IDisposable` (interfaces are off-limits to `ref struct` types until C# 13's
interface-implementation relaxation), and the new range (`..`) and index (`^`) operators slice
`Span<T>`/`ReadOnlySpan<T>` without allocating.

## Syntax

```csharp
public ref struct PooledBuffer
{
    private byte[] _rented;
    public Span<byte> Data { get; }

    public PooledBuffer(int size)
    {
        _rented = ArrayPool<byte>.Shared.Rent(size);
        Data = _rented.AsSpan(0, size);
    }

    public void Dispose() // no `: IDisposable` — ref struct cannot implement it
    {
        ArrayPool<byte>.Shared.Return(_rented);
        _rented = null!;
    }
}
```

```csharp
using var buffer = new PooledBuffer(256); // structural (duck-typed) match on Dispose()
```

## Basic use case: pattern-based `Dispose()` for a `ref struct` wrapping a pooled buffer

```csharp
void ProcessChunk(int size)
{
    using var buffer = new PooledBuffer(size); // calls Dispose() via the pattern match, not an interface
    Fill(buffer.Data);
    Consume(buffer.Data);
} // buffer.Dispose() called here even though PooledBuffer : IDisposable is impossible
```

The compiler recognizes any type — `ref struct` or not — with an accessible, parameterless,
`void`-returning instance method named `Dispose()` as valid in a `using` statement or `using`
declaration, purely by matching that method shape structurally. This exception exists specifically
because `ref struct` types can never implement `IDisposable` (or any interface) before C# 13, so
without pattern-based disposal, a disposable `ref struct` like `PooledBuffer` above would have no
way to hook into `using` at all.

## Advanced use case: range slicing over `Span<T>` with no allocation

```csharp
public static ReadOnlySpan<char> TrimQuotes(ReadOnlySpan<char> text)
{
    if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
    {
        return text[1..^1]; // Slice(1, text.Length - 2) under the hood — no substring allocation
    }
    return text;
}
```

`text[^1]` uses `Index` (`^1` means "1 from the end"); `text[1..^1]` uses `Range`. Both compile
against `Span<T>`/`ReadOnlySpan<T>` because those types expose an `int`-parameter indexer and a
`Slice(int, int)` method — the exact shape the range/index pattern requires structurally, the same
duck-typing approach `using` takes for `Dispose()`. Compare to a `string`, where `text[1..^1]`
allocates a new `string` — the `Span<T>` version above never does, since `Slice` just returns a new
`Span<T>` view over the same backing memory.

## Requirements and restrictions

- Pattern-based `Dispose()` requires the exact shape: public, parameterless, returns `void`,
  instance (not static) method. A `Dispose(bool)` overload or a `ValueTask DisposeAsync()` does not
  match this pattern (`await using` has a separate, analogous pattern-based match for
  `DisposeAsync()`, unrelated to `ref struct`s specifically since a `ref struct` cannot participate
  in `async` code at all — it can never survive across an `await`).
- Range/`Index` slicing on `Span<T>` is bounds-checked at runtime exactly like ordinary indexing —
  it throws `ArgumentOutOfRangeException`, not a silently wrong slice, if the range falls outside
  `[0, Length]`.

## Fallback

Without pattern-based disposal (below C# 8.0), a `ref struct` with unmanaged resources to release
needs an explicitly named cleanup method and a `try`/`finally` at every call site instead of
`using`:

```csharp
var buffer = new PooledBuffer(size);
try
{
    Fill(buffer.Data);
    Consume(buffer.Data);
}
finally
{
    buffer.Dispose(); // called by hand; no using-statement support pre-8.0
}
```

Without range/`Index` operators, slice with the explicit `Slice(start, length)` method both
`Span<T>` and `ReadOnlySpan<T>` have carried since their C# 7.2 introduction — `text[1..^1]` becomes
`text.Slice(1, text.Length - 2)`, same runtime behavior, more verbose at the call site.
