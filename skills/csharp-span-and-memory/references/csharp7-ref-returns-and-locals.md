# `ref` returns and `ref` locals (C# 7.0 / .NET Framework 4.6.2, .NET Core 1.0)

C# 7.0 shipped March 2017 alongside Visual Studio 2017 15.0. Before this release, a method could
receive a parameter `by ref`, but it could never *return* a reference — the caller always got a
copy of whatever the method returned. C# 7.0 lets a method return an alias to a storage location
instead of a value, and lets a local variable hold that alias. This is the foundational mechanic
`Span<T>` is later built on: a `Span<T>` indexer returns `ref T`, not `T`, which is what lets
`span[i] = x` mutate the original backing storage rather than a copy.

## Syntax

```csharp
public ref int Find(int[] numbers, int target)
{
    for (int i = 0; i < numbers.Length; i++)
    {
        if (numbers[i] == target)
        {
            return ref numbers[i];
        }
    }
    throw new InvalidOperationException("Not found.");
}
```

```csharp
ref int found = ref Find(numbers, 42);
found = 100; // mutates numbers[] in place, no re-indexing
```

## Basic use case: returning a reference into an array element to avoid a redundant lookup

```csharp
public ref int GetOrAddSlot(int[] table, int key)
{
    ref int slot = ref table[key % table.Length];
    return ref slot;
}

ref int slot = ref GetOrAddSlot(table, 42);
slot += 1; // one lookup instead of a separate read-then-write pair
```

## Advanced use case: a `ref` local aliasing a large struct's field to avoid copying

```csharp
public struct Matrix4x4 { public float M11, M12, M13, M14; /* ...12 more fields... */ }

public static void ScaleRow(ref Matrix4x4 matrix, float factor)
{
    ref float m11 = ref matrix.M11;
    m11 *= factor; // mutates the field in place; no copy of the 64-byte struct
}
```

For a struct this size, `matrix.M11 *= factor` alone already avoids a copy — the payoff for a
`ref` local grows when the same aliased field is read and written several times in a loop, since
every use after the `ref` local's declaration is a direct memory access rather than a fresh
field lookup through the struct.

## Requirements and restrictions

- A `ref return` can only return a reference to something with a lifetime that outlives the
  method call: a field, an array element, or another `ref` parameter/return — never a local
  variable declared inside the method (the compiler rejects `return ref localVariable;` for an
  ordinary local, since that storage disappears when the method returns).
- The method signature must say `ref` on the return type (`public ref int Find(...)`), and the
  call site must say `ref` again (`ref int found = ref Find(...)`) — a plain `int found = Find(...)`
  compiles but silently copies the value instead of aliasing it.
- `ref` locals declared in C# 7.0 could not be reassigned to point somewhere else after
  initialization — that came in C# 7.3, see
  [csharp7.3-ref-reassignment-and-stackalloc-init.md](csharp7.3-ref-reassignment-and-stackalloc-init.md).

## Fallback

There is no earlier tier for aliasing a storage location by reference across a method boundary —
before C# 7.0, a method could only accept a `ref`/`out` parameter, never return one. The workaround
is restructuring so the caller passes the target location in as a `ref` parameter instead of
receiving one back:

```csharp
public bool TryFind(int[] numbers, int target, out int index)
{
    for (int i = 0; i < numbers.Length; i++)
    {
        if (numbers[i] == target) { index = i; return true; }
    }
    index = -1;
    return false;
}

// caller does its own indexing to mutate in place
if (TryFind(numbers, 42, out int index))
{
    numbers[index] = 100;
}
```
