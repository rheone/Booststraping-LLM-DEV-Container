# `ref` local reassignment and `stackalloc` initializers (C# 7.3, May 2018)

C# 7.3 shipped May 2018 alongside Visual Studio 2017 15.7 / .NET Core SDK 2.1.300. It closes two
gaps left by C# 7.0/7.2: a `ref` local couldn't be pointed at a different storage location after
its initial assignment, and `stackalloc` had no array-initializer syntax and needed `unsafe` to
convert to `Span<T>`.

## Syntax

```csharp
ref int current = ref array[0];
current = ref array[1]; // ref reassignment — current now aliases array[1], not array[0]
```

```csharp
Span<int> numbers = stackalloc int[] { 1, 2, 3, 4, 5 }; // initializer syntax, no unsafe needed
```

## Basic use case: `ref` reassignment to walk a linked structure without copying nodes

```csharp
public struct Node
{
    public int Value;
    public int NextIndex; // -1 means none
}

public static int SumChain(Node[] nodes, int startIndex)
{
    int total = 0;
    ref Node current = ref nodes[startIndex];
    while (true)
    {
        total += current.Value;
        if (current.NextIndex < 0)
        {
            break;
        }
        current = ref nodes[current.NextIndex]; // re-point the same ref local, no re-declaration
    }
    return total;
}
```

Before 7.3, this loop needed a fresh `ref Node` declaration each iteration, or a plain (copying)
`Node` local defeating the point of aliasing at all.

## Advanced use case: `stackalloc` directly into a typed `Span<T>` with initializer syntax

```csharp
public static int Checksum(ReadOnlySpan<byte> header)
{
    Span<byte> scratch = stackalloc byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
    int total = 0;
    for (int i = 0; i < header.Length && i < scratch.Length; i++)
    {
        total += header[i] ^ scratch[i];
    }
    return total;
}
```

No `unsafe` keyword, no pointer type anywhere — the `stackalloc byte[] { ... }` expression converts
directly to `Span<byte>` because the target type is `Span<T>`/`ReadOnlySpan<T>`, which C# 7.2
already permitted as a *type*, and C# 7.3 adds the array-initializer shorthand plus removes the
`unsafe` requirement for this specific conversion.

## Requirements and restrictions

- `ref` reassignment (`r = ref v`) requires the right-hand side's storage to live at least as long
  as the left-hand side's — the compiler enforces this the same way it enforces `ref return`
  lifetime rules in [csharp7-ref-returns-and-locals.md](csharp7-ref-returns-and-locals.md).
- The `unsafe`-free `stackalloc` conversion applies only when the compile-time target type is
  `Span<T>`/`ReadOnlySpan<T>` — `byte* p = stackalloc byte[10];` (a pointer target) still requires
  `unsafe`, same as before 7.2.

## Fallback

Without `ref` reassignment (below C# 7.3), re-declare a fresh `ref` local per logical re-point
rather than reassigning one — verbose in a loop, but expresses the same aliasing:

```csharp
// C# 7.0-7.2: cannot write `current = ref nodes[...]` in a loop body;
// restructure as a plain index-walking loop instead of a ref-local chain walk
public static int SumChain(Node[] nodes, int startIndex)
{
    int total = 0;
    int index = startIndex;
    while (index >= 0)
    {
        total += nodes[index].Value; // indexes the array directly each time, no ref local at all
        index = nodes[index].NextIndex;
    }
    return total;
}
```

For the `stackalloc` initializer shorthand, fall back to
[csharp7.2-ref-struct-and-span.md](csharp7.2-ref-struct-and-span.md)'s `unsafe`-pointer form and
assign elements individually, or build the array with an ordinary managed array literal and call
`.AsSpan()` on it instead of `stackalloc` (trading a heap allocation for the convenience).
