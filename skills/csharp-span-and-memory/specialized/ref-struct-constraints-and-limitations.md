# `ref struct` constraints and limitations, in depth

`ref struct` (C# 7.2, [csharp7.2-ref-struct-and-span.md](../references/csharp7.2-ref-struct-and-span.md))
trades away most of the places an ordinary value type is allowed to live, in exchange for a
compiler-enforced guarantee it never outlives its stack frame. This file is the full restriction
list in one place — every reference tier above links here rather than restating it piecemeal.

## The restriction list

- **Cannot be boxed.** No implicit or explicit conversion to `object`, `dynamic`, or any interface
  type exists (interfaces excluded entirely — see next point) — boxing would put the value on the
  heap, defeating the entire stack-only guarantee.
- **Cannot implement an interface**, before C# 13's separate, related relaxation (check current
  documentation for exact current-version rules if this matters to a specific design) — a value
  accessed through an interface reference is indistinguishable from a boxed one to the runtime, so
  this follows directly from the no-boxing rule.
- **Cannot be a field of a non-`ref struct` type.** A `class` or ordinary `struct` field must have a
  fixed, boxable-if-needed storage location; a `ref struct` field is only legal inside another
  `ref struct`, where the same stack-only guarantee already applies to the whole enclosing type.
- **Cannot be a static field**, anywhere, for the same reason — static storage isn't a stack frame.
- **Cannot be used as a type argument for a generic type or method parameter**, before C# 13's
  `allows ref struct` anti-constraint (see
  [csharp13-allows-ref-struct.md](../references/csharp13-allows-ref-struct.md)) — without it, this
  is still the default and by far the most common restriction developers hit.
- **Cannot be captured by a lambda expression or local function that escapes the current method** —
  capturing means storing the value in a compiler-generated closure class, which is heap-allocated.
  A lambda that doesn't capture the `ref struct` (doesn't reference it in its body) is unaffected;
  the restriction is about capture, not about lambdas existing nearby.
- **Cannot be used as a field/local that's alive across an `await` or `yield return`** — an async
  method's or iterator's state machine is a compiler-generated class (heap-allocated) once it needs
  to suspend, so a `ref struct` value can never be part of that suspended state. This is exactly why
  `Memory<T>`/`ReadOnlyMemory<T>` exist as the async-safe counterpart — see
  [span-vs-memory-vs-readonlymemory.md](span-vs-memory-vs-readonlymemory.md).
- **Cannot be declared as an `async` method's parameter or local at all in the general case** — a
  narrow exception exists in C# 13 for `ref`/`ref struct` locals confined entirely between
  `await`s within a single synchronous span of an async method; the parameter-level restriction and
  the general "can't cross an `await`" rule remain otherwise unchanged.

## Basic: hitting the closure-capture restriction directly

```csharp
public void Process(Span<byte> buffer)
{
    // compile error: cannot use local 'buffer' inside a lambda expression
    // Action process = () => buffer.Clear();

    // fine: no capture, because the span is used directly, not from inside a delegate body
    buffer.Clear();
}
```

## Basic: the non-`ref struct` field restriction, and the fix

```csharp
public class Parser // ordinary class, NOT ref struct
{
    // compile error CS8345: cannot declare a field of ref struct type 'ReadOnlySpan<char>'
    // private ReadOnlySpan<char> _remaining;

    // fix: store the backing data as a field, materialize the Span<T> locally where it's used
    private string _text;
    private int _position;

    public ReadOnlySpan<char> Remaining => _text.AsSpan(_position);
}
```

A class that logically "has a current span" almost always actually has array/string data plus an
offset — store those (ordinary, storable types) as fields, and expose the `Span<T>` view as a
computed property or method result instead of a stored field.

## Advanced: the async/lambda restrictions colliding with a common LINQ-style refactor

```csharp
public int CountMatches(Span<int> values, int target)
{
    // compile error: 'values' cannot be used inside a lambda (captured by Where/Count)
    // return values.ToArray().Where(v => v == target).Count(); // also allocates via ToArray()

    // fix: hand-written loop, no delegate capturing the span
    int count = 0;
    foreach (int v in values)
    {
        if (v == target) count++;
    }
    return count;
}
```

This is the most common real-world friction point: LINQ's `IEnumerable<T>`-based operators take
delegates, and a `Span<T>` can't be captured by one — LINQ over `Span<T>` requires either
`Span<T>`'s own non-LINQ instance methods (`IndexOf`, `Contains`, `BinarySearch` where applicable)
or a hand-written loop, never `.Where()`/`.Select()`/`.Count()` directly on a span-derived sequence.

## Fallback

None of these restrictions apply to ordinary structs or classes — they exist specifically because
`ref struct` opts into stack-only semantics. A type that turns out to need heap storage, interface
implementation, or async/lambda capture in practice should not be a `ref struct` at all; use
`Memory<T>`/`ReadOnlyMemory<T>` (see
[span-vs-memory-vs-readonlymemory.md](span-vs-memory-vs-readonlymemory.md)) or an ordinary
reference type instead of fighting these restrictions.
