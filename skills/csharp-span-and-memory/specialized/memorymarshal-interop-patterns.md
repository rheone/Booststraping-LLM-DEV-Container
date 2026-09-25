# `MemoryMarshal` interop patterns

`MemoryMarshal` (`System.Runtime.InteropServices`) is the escape hatch for reinterpreting memory
across `Span<T>`/`Memory<T>`/`ReadOnlySpan<T>`/`ReadOnlyMemory<T>` and raw bytes, without the
unsafe-pointer plumbing that would otherwise be required. It ships in the same `System.Memory`
package/shared-framework surface as `Span<T>`/`Memory<T>` themselves — see the language-version vs.
BCL-version split in
[csharp7.2-ref-struct-and-span.md](../references/csharp7.2-ref-struct-and-span.md) — so it's
available anywhere `Span<T>` is, with no separate version gate of its own.

## Basic: reinterpreting a `Span<byte>` as a `Span<int>`

```csharp
Span<byte> bytes = stackalloc byte[16];
Span<int> ints = MemoryMarshal.Cast<byte, int>(bytes); // 4 ints, same backing memory, no copy

ints[0] = 42;
// bytes now contains 42's little/big-endian byte representation in bytes[0..4]
```

`MemoryMarshal.Cast<TFrom, TTo>` reinterprets the same memory as a different element type — it does
not convert or copy values, so byte order/endianness and struct layout matter exactly as they would
for an unsafe pointer cast. Both `TFrom` and `TTo` must be unmanaged types (no references,
no `string`, no arrays as elements).

## Basic: viewing a struct as raw bytes for serialization

```csharp
public readonly struct Point3D { public readonly float X, Y, Z; }

public static ReadOnlySpan<byte> AsBytes(in Point3D point) =>
    MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in point, 1));

Point3D p = new();
ReadOnlySpan<byte> raw = AsBytes(p); // 12 bytes, zero-copy view over the struct's memory
```

`MemoryMarshal.AsBytes<T>` reinterprets a `Span<T>`/`ReadOnlySpan<T>` of an unmanaged type as raw
bytes — the common building block for binary serialization or hashing code that needs to feed a
struct's bit pattern into a stream or hash function without a manual field-by-field write.

## Advanced: `MemoryMarshal.GetReference` for interop with APIs expecting a pointer-adjacent shape

```csharp
public static unsafe void FillNative(Span<byte> buffer)
{
    ref byte first = ref MemoryMarshal.GetReference(buffer);
    fixed (byte* p = &first)
    {
        NativeFill(p, buffer.Length); // P/Invoke signature: void NativeFill(byte* buffer, int length)
    }
}
```

`GetReference` returns a `ref T` to the span's first element (or a null-adjacent reference for an
empty span — never dereference it without checking `Length > 0` first) without the bounds-checking
overhead of indexing `buffer[0]`. Combined with `fixed`, this is the standard bridge from
`Span<T>`-based managed code into a P/Invoke signature that expects a raw pointer.

## Advanced: `MemoryMarshal.TryGetArray` to recover the backing array from a `Memory<T>`

```csharp
public static void WriteToSocket(Socket socket, ReadOnlyMemory<byte> data)
{
    if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment))
    {
        socket.Send(segment.Array!, segment.Offset, segment.Count, SocketFlags.None); // older, array-only Socket API
    }
    else
    {
        socket.Send(data.Span); // Memory<T> wasn't array-backed (e.g. came from a custom MemoryManager<T>)
    }
}
```

Useful specifically when bridging into an older API (pre-`Span<T>`-aware BCL surface, or a
third-party library) that only accepts `byte[]`/`ArraySegment<byte>` — `TryGetArray` recovers the
underlying array when one exists, and the `bool` return means the code must still handle the case
where the `Memory<T>` isn't array-backed at all (e.g., it wraps native or pooled unmanaged memory
via a custom `MemoryManager<T>`).

## Requirements and restrictions

- `MemoryMarshal.Cast<TFrom, TTo>` requires both type parameters to be unmanaged (no reference
  fields anywhere in their layout, recursively) — the same constraint `unmanaged` (C# 7.3, part of
  the generic-constraints feature set) expresses at the language level.
- Reinterpreting memory this way bypasses type safety the same way an `unsafe` pointer cast would —
  `MemoryMarshal` methods don't require an `unsafe` block themselves (they're implemented with
  `unsafe` internally, exposed through a safe API surface), but misuse (wrong `TTo` size assumption,
  endianness mismatch) produces the same class of silent-corruption bug an unsafe cast would.
- `GetReference` on an empty span returns a reference that must not be dereferenced — check
  `Length` first, exactly as with any other span operation on an empty span.

## Fallback

Without `MemoryMarshal` (below the `Span<T>`/`System.Memory` BCL availability line), the equivalent
reinterpretation requires `unsafe` pointer casts directly:

```csharp
public static unsafe Span<int> BytesAsInts(byte[] bytes)
{
    fixed (byte* p = bytes)
    {
        return new Span<int>(p, bytes.Length / sizeof(int));
    }
}
```

Same runtime behavior, but requires `unsafe` explicitly and a `fixed` block to pin the managed
array against the GC moving it mid-cast — `MemoryMarshal`'s safe-surface methods handle that pinning
concern internally.
