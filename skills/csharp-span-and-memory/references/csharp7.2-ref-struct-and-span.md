# `ref struct`, `in` parameters, and `Span<T>` (C# 7.2, December 2017)

C# 7.2 shipped with Visual Studio 2017 15.5 (December 4, 2017). It adds the `ref struct` modifier,
`readonly struct`/`readonly ref struct`, `in` parameters, `ref readonly` returns, and `stackalloc`
usable in a nested expression context — the whole language-level foundation `Span<T>` needed to
exist as a safe, stack-only type.

## The language-version vs. BCL-version split

This is two separate release trains, and mixing them up is the single most common mistake when
targeting an older runtime:

- **The `ref struct` *language feature*** (the ability to declare `public ref struct Foo { }` at
  all) is part of the C# 7.2 compiler — available the moment a project's `<LangVersion>` is 7.2 or
  higher, regardless of target framework.
- **`Span<T>` and `Memory<T>` themselves are ordinary BCL types**, not language syntax. They did
  not ship with the C# 7.2 compiler — they shipped roughly five months later as the `System.Memory`
  NuGet package (first stable release **4.5.0, May 29, 2018**), usable from .NET Standard 1.1
  upward, then became part of the shared framework starting with **.NET Core 2.1** and fully
  built into the runtime (no package reference needed) from **.NET Core 3.0** onward.

Practical effect: a project can have `<LangVersion>7.2</LangVersion>` and still not have `Span<T>`
available until it also references `System.Memory` (older target) or targets .NET Core 2.1+/.NET
Framework 4.7.2+ with the compatibility shim. Declaring your own `ref struct` type needs only the
language version; using `Span<T>` needs the BCL type to actually be present.

## Syntax

```csharp
public readonly ref struct Buffer
{
    private readonly Span<byte> _data;

    public Buffer(Span<byte> data) => _data = data;

    public byte this[int index] => _data[index];
}
```

```csharp
public void Process(in LargeStruct config, ref readonly Header header)
{
    // config and header are passed by reference but cannot be reassigned here
}
```

## Basic use case: a zero-allocation view over part of an array

```csharp
byte[] buffer = new byte[1024];
Span<byte> view = buffer.AsSpan(10, 20); // no copy, no allocation beyond the Span value itself

for (int i = 0; i < view.Length; i++)
{
    view[i] = 0xFF; // writes through to buffer[10..30]
}
```

```csharp
ReadOnlySpan<char> ParseFirstWord(ReadOnlySpan<char> text)
{
    int spaceIndex = text.IndexOf(' ');
    return spaceIndex < 0 ? text : text[..spaceIndex];
}
```

## Advanced use case: `in` parameters avoiding large-struct copies at call sites

```csharp
public readonly struct Vector3D
{
    public readonly double X, Y, Z;
    public Vector3D(double x, double y, double z) => (X, Y, Z) = (x, y, z);
}

public static double Dot(in Vector3D a, in Vector3D b) =>
    a.X * b.X + a.Y * b.Y + a.Z * b.Z;
```

Callers pass `Dot(vectorA, vectorB)` exactly as if the parameters were by value — no `in` keyword
needed at the call site — but the compiler passes both by reference under the hood, skipping the
copy `Vector3D` by-value parameters would otherwise incur on every call.

## Requirements and restrictions

- A `ref struct` can **never** be boxed, stored as a field of a non-`ref struct` type, or used as a
  type argument for a generic parameter before C# 13 (see
  [csharp13-allows-ref-struct.md](csharp13-allows-ref-struct.md)) — full list in
  [specialized/ref-struct-constraints-and-limitations.md](../specialized/ref-struct-constraints-and-limitations.md).
- `in` guarantees the callee cannot reassign the parameter, but does not guarantee the caller's
  argument is actually read-only elsewhere — `in` is a calling-convention optimization first,
  a mutation-prevention guarantee second.
- `stackalloc` was legal before 7.2 only as the sole initializer of a local pointer variable
  (`byte* p = stackalloc byte[256];`, requiring `unsafe`). C# 7.2 allows it in a nested expression
  position when the target is `Span<T>`/`ReadOnlySpan<T>` — but the *safe*, no-`unsafe`-required
  form of that conversion is a C# 7.3 addition; see
  [csharp7.3-ref-reassignment-and-stackalloc-init.md](csharp7.3-ref-reassignment-and-stackalloc-init.md).

## Fallback

Below C# 7.2 language version, or on a runtime without `Span<T>`/`Memory<T>` available at all, use
the pre-history patterns in
[pre-csharp7-arrays-and-pointers.md](pre-csharp7-arrays-and-pointers.md) — `ArraySegment<T>` for
array slices, raw `offset`/`count` parameters, or `unsafe` pointers for stack/unmanaged memory.
`in` parameters have no real substitute for avoiding large-struct copies pre-7.2 beyond passing
by `ref` and trusting callers not to mutate, or accepting the copy.
