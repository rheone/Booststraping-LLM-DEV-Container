# First-class `Span<T>`/`ReadOnlySpan<T>` conversions (C# 14 / .NET 10, November 2025)

C# 14 shipped November 2025 alongside .NET 10. Before this release, converting between `T[]`,
`Span<T>`, and `ReadOnlySpan<T>` required an explicit call — `.AsSpan()` to go from array to span,
an explicit `Span<T>`-to-`ReadOnlySpan<T>` conversion existed already but composing these
conversions with generic type inference or extension-method receivers often didn't work the way an
author expected, forcing extra overloads or explicit casts. C# 14 gives the compiler first-class
knowledge of the relationship between these three types, adding new implicit conversions and
letting span types serve as extension method receivers and participate in generic inference the
way arrays and other BCL types already could.

## Syntax

```csharp
// implicit array -> Span<T> and array -> ReadOnlySpan<T>, without .AsSpan()
void Process(Span<int> values) { /* ... */ }
int[] numbers = { 1, 2, 3 };
Process(numbers); // implicit conversion; previously required Process(numbers.AsSpan())
```

```csharp
// implicit Span<T> -> ReadOnlySpan<T> already existed pre-14; C# 14 extends composability
void Read(ReadOnlySpan<int> values) { /* ... */ }
Span<int> writable = numbers;
Read(writable); // still implicit, now composes more naturally with generic inference below
```

## Basic use case: passing an array directly where `ReadOnlySpan<T>` is expected

```csharp
public static int Sum(ReadOnlySpan<int> values)
{
    int total = 0;
    foreach (int v in values)
    {
        total += v;
    }
    return total;
}

int[] data = { 1, 2, 3, 4, 5 };
int total = Sum(data); // C# 14: implicit array -> ReadOnlySpan<int>, no .AsSpan() call needed
```

Pre-C#14, the same call required `Sum(data.AsSpan())` explicitly — a minor but constant piece of
friction on every call site of every `Span<T>`/`ReadOnlySpan<T>`-accepting API, since arrays remain
the far more common way data actually arrives in a codebase.

## Advanced use case: span types as extension method receivers and generic inference targets

```csharp
public static class SpanExtensions
{
    public static bool IsAllZero(this ReadOnlySpan<int> values)
    {
        foreach (int v in values)
        {
            if (v != 0) return false;
        }
        return true;
    }
}

int[] numbers = new int[10];
bool allZero = numbers.IsAllZero(); // C# 14: array implicitly becomes the ReadOnlySpan<int> receiver
```

Before C# 14, calling a `ReadOnlySpan<T>` extension method on an array required an explicit
`numbers.AsSpan().IsAllZero()` — the array itself wasn't a valid extension-method receiver for a
method declared to extend `ReadOnlySpan<T>`. C# 14 makes the array a legal receiver directly,
because the compiler now understands the implicit array-to-span conversion applies in receiver
position too, the same way it already applied in ordinary argument position for other implicit
conversions.

## Requirements and restrictions

- These are one-way, narrowing-safe conversions in the direction `T[]` → `Span<T>` → `ReadOnlySpan<T>`
  (and `T[]` → `ReadOnlySpan<T>` directly) — never the reverse. Going from `ReadOnlySpan<T>` back to
  `Span<T>` or `T[]` still requires an explicit, potentially-allocating call (`ToArray()`), since
  that direction can't be done implicitly without either an unsafe cast or a copy.
  the conversion table lives in the C# language reference's built-in-types article; verify current
  exact entries there before relying on an edge case not shown above.
- Overload resolution changes are possible when a method has both a `T[]`-accepting and a
  `Span<T>`/`ReadOnlySpan<T>`-accepting overload — the array argument may now bind to either,
  and the compiler's tie-breaking rules determine which; check for ambiguity warnings when adding a
  `Span<T>` overload alongside an existing array overload on an older codebase migrating to C# 14.

## Fallback

Below C# 14, every array-to-span conversion needs its explicit call: `.AsSpan()` for `Span<T>` or
`ReadOnlySpan<T>` targets, and an extension method receiver must be the span type itself, not the
array — `numbers.AsSpan().IsAllZero()` instead of `numbers.IsAllZero()`. Nothing about
`Span<T>`/`ReadOnlySpan<T>`'s own runtime behavior changes; this tier is purely about how much
conversion syntax the call site has to spell out.
