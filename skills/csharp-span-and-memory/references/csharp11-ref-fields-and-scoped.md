# `ref` fields and the `scoped` modifier (C# 11 / .NET 7, November 2022)

C# 11 shipped November 2022 alongside .NET 7. Before this release, a `ref struct` could hold a
`Span<T>` field (itself already a compiler-magic wrapper around a managed pointer) but could not
declare an ordinary field typed as `ref T` — that capability was hardcoded into the runtime's own
implementation of `Span<T>`, not expressible in C# source. C# 11 makes `ref` fields a real language
feature, and introduces `scoped` to let method authors constrain how far a `ref`-like value (a
`ref` parameter/local, or a `ref struct` value) is allowed to escape.

## Syntax

```csharp
public readonly ref struct CustomSpan<T>
{
    private readonly ref T _reference;
    private readonly int _length;

    public CustomSpan(ref T reference, int length)
    {
        _reference = ref reference;
        _length = length;
    }

    public ref T this[int index] => ref Unsafe.Add(ref _reference, index);
}
```

```csharp
void Process(scoped ref readonly Header header)
{
    // header (and anything derived from it) is guaranteed not to escape this method
}
```

## Basic use case: a custom `ref struct` that needed hand-written unsafe code before this tier

```csharp
public readonly ref struct Sliding3<T>
{
    private readonly ref T _first;

    public Sliding3(Span<T> source)
    {
        if (source.Length < 3)
        {
            throw new ArgumentException("Need at least 3 elements.");
        }
        _first = ref source[0];
    }

    public T First => _first;
    public T Second => Unsafe.Add(ref _first, 1);
    public T Third => Unsafe.Add(ref _first, 2);
}
```

Before C# 11, expressing "this type holds a reference into someone else's memory, safely
lifetime-checked" required either the runtime's own built-in `Span<T>`/`ReadOnlySpan<T>` (which
uses internal, non-expressible-in-C#-source mechanisms) or genuinely `unsafe` pointer fields with
no compiler-enforced lifetime at all. `ref` fields close that gap for ordinary user code.

## Advanced use case: `scoped` narrowing what a method's `ref`-like parameter can do

```csharp
public static void Normalize(scoped Span<double> values)
{
    double sum = 0;
    foreach (double v in values)
    {
        sum += v;
    }
    double mean = sum / values.Length;
    for (int i = 0; i < values.Length; i++)
    {
        values[i] -= mean; // mutates through the span; fine
    }
    // NOT fine, and this is exactly what `scoped` prevents:
    // this._cachedSpan = values;  -- would be a compile error: values cannot escape this method
}
```

`scoped` on a `Span<T>` parameter is often implicit for ordinary parameters already (the compiler
infers the safest scope it can), but becomes load-bearing and must be written explicitly once a
type or method also has `ref` fields or ref-returning members in play, where the compiler can no
longer infer a safe default on its own — the C# 13 generic anti-constraint in
[csharp13-allows-ref-struct.md](csharp13-allows-ref-struct.md) is the case this shows up in most
often, since `scoped T` is required there.

## Requirements and restrictions

- A `ref` field may only be declared inside a `ref struct` — the whole point is that the enclosing
  type is already stack-only, so the compiler can enforce that the `ref` field's target outlives
  the struct instance the same way it enforces any other `ref` lifetime rule.
- `readonly ref struct` types with a `ref` field must assign it in the constructor and never
  reassign it afterward, exactly like any other `readonly` field.
- `scoped` can be applied to a `ref` parameter, a `ref struct`-typed parameter, or a local
  declaration — it narrows the compiler's inferred "safe-to-escape" scope of that value to the
  current method body, never widens it.

## Fallback

Without `ref` fields (below C# 11), a custom type needing to hold a reference into external memory
has to either wrap a `Span<T>`/`Memory<T>` field instead of a raw `ref T` (works for most cases,
since `Span<T>` already carries that capability internally):

```csharp
public readonly ref struct Sliding3<T>
{
    private readonly Span<T> _source;

    public Sliding3(Span<T> source) => _source = source;

    public T First => _source[0];
    public T Second => _source[1];
    public T Third => _source[2];
}
```

or fall back to genuinely `unsafe` pointer fields when the target memory isn't already expressible
as a `Span<T>`. `scoped` has no pre-C#11 equivalent — an author relying on the compiler's default
scope inference (which existed since C# 7.2 for ordinary `ref`/`Span<T>` parameters) simply cannot
narrow it explicitly, and must instead avoid writing APIs where the inferred default is too
permissive for the intended usage.
