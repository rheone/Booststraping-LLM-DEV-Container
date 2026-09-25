# `allows ref struct` — ref structs as generic type arguments (C# 13 / .NET 9, November 2024)

C# 13 and .NET 9 shipped November 12, 2024. This is the single biggest change to how `Span<T>` and
custom `ref struct` types interact with generic code since `ref struct` itself was introduced in
C# 7.2. Before C# 13, **no `ref struct` — not `Span<T>`, not `ReadOnlySpan<T>`, not a type you wrote
yourself — could ever be used as a generic type argument**, full stop. `List<Span<int>>`,
`Dictionary<string, ReadOnlySpan<char>>`, and a hand-written `MyGeneric<Span<byte>>` all failed to
compile with CS0306, because the compiler could not prove the generic code wouldn't do something a
`ref struct` fundamentally cannot survive — get boxed, get stored as a field on a non-`ref struct`
type, or get captured into a closure or async state machine. C# 13 adds the `allows ref struct`
anti-constraint, which opts a specific type parameter into permitting (not requiring) a `ref
struct` argument, once the generic code proves it won't do any of those things to it.

This file is about what the anti-constraint unlocks for `Span<T>`/`ref struct` code specifically —
generic algorithms that finally work over `Span<T>`-shaped inputs the same way they already worked
over arrays or `List<T>`. It assumes you already know how to read and combine `where` constraints;
it doesn't re-derive constraint syntax from scratch.

## Syntax

```csharp
public static T Sum<T>(scoped ReadOnlySpan<T> values) where T : INumber<T> // T here is a numeric element type, unrelated to this feature
{
    T total = T.Zero;
    foreach (T v in values)
    {
        total += v;
    }
    return total;
}

// what's NEW in C# 13: a generic parameter that stands for the container/span type itself
public static void FillWithDefault<TBuffer>(scoped TBuffer buffer) where TBuffer : allows ref struct
{
    // ...
}
```

## Why `scoped` is mandatory alongside it

The moment `T` is allowed to be a `ref struct`, every use of a `T` value inside the generic method
is subject to the same escape-analysis rules a hand-written `ref struct` parameter would face:
it may not be boxed, stored in a field, captured by a lambda, or held across an `await` — because
it might, at the call site, actually *be* a `Span<int>`. `scoped T` on a parameter is how the
method promises the compiler "this value will not outlive this call," which is required before the
compiler will let the method do anything with a `T` that could be a `ref struct`.

## Basic use case: a generic buffer-processing method that finally accepts `Span<T>`

```csharp
public static class BufferOps
{
    // before C# 13: this signature could only accept T[], never Span<T> or ReadOnlySpan<T>,
    // forcing either an array-only API or a separate hand-written Span<T> overload
    public static void Clear<T>(scoped Span<T> buffer) where T : allows ref struct
    {
        buffer.Clear();
    }
}

Span<byte> stackBuffer = stackalloc byte[256];
BufferOps.Clear(stackBuffer); // Span<byte> as a type argument — illegal before C# 13
```

Note this specific example doesn't even need the anti-constraint — `Span<T>` isn't itself the type
argument here, `T` (the element type) is, and element types were never restricted. The anti-
constraint matters once a generic parameter stands for the `Span<T>`/`ref struct` *container*
itself, as in the next example.

## Advanced use case: a generic algorithm parameterized over the span/container type

```csharp
public static class ParserOps
{
    public static int CountMatches<TSpan, TPredicate>(scoped TSpan source, TPredicate predicate)
        where TSpan : allows ref struct
        where TPredicate : IPredicate<char>
    {
        int count = 0;
        // TSpan constrained further by an interface the caller's concrete span-like type implements,
        // e.g. IReadOnlySpanLike<char> — illustrative; the BCL doesn't ship one interface every
        // span-shaped type implements, since ref structs could not implement interfaces at all
        // before C# 13's separate, related ref-struct-interfaces relaxation.
        return count;
    }
}
```

The realistic payoff of `allows ref struct` today is less "write one generic method over an
abstract span-like interface" (since `Span<T>`/`ReadOnlySpan<T>` still don't implement a common
interface in the BCL as of C# 13/14) and more the *first* case above: ordinary generic buffer code,
generic caching/pooling helpers, and state-passed-through-a-callback patterns that previously had
to fall back to `object` boxing or per-type overloads specifically because a `Span<T>`-shaped
argument was categorically rejected, not because the generic logic itself needed anything
`Span<T>`-specific.

## State-passed-through-callback: the pattern this most directly fixes

```csharp
public static class SpanCallback
{
    public static TResult WithState<TState, TResult>(TState state, Func<TState, TResult> callback)
        where TState : allows ref struct =>
        callback(state);
}

ReadOnlySpan<char> chars = "hello world";
int length = SpanCallback.WithState(chars, s => s.Length); // ReadOnlySpan<char> as TState — new in C# 13
```

Before C# 13, passing a `ReadOnlySpan<char>` through a generic "call this callback with some state"
helper (a common shape in high-performance parsing and formatting code, used to avoid closure
allocations) required either boxing the span into `object` (impossible — `ref struct` can't be
boxed at all) or a non-generic overload hand-written per state shape. `allows ref struct` on
`TState` makes the generic version itself legal.

## Requirements and restrictions

- `allows ref struct` only ever *widens* what a type argument may be — it's an anti-constraint, the
  only constraint kind that relaxes rather than narrows. A type parameter without it is unaffected;
  existing generic code that never expected a `ref struct` argument keeps rejecting one exactly as
  before, with no source change required to preserve that behavior.
- Once present, the compiler enforces full escape-analysis on every use of that type parameter
  inside the generic method/type body — not just where a `ref struct` argument is suspected, since
  the compiler must support the *worst case* caller.
- A type parameter with `allows ref struct` still cannot be boxed, used in a `lock` statement,
  compared with `==`/`!=` unless a suitable operator/constraint exists, or captured in a lambda or
  local function that escapes the enclosing scope — everything a hand-written `ref struct` already
  cannot do (full list in
  [specialized/ref-struct-constraints-and-limitations.md](../specialized/ref-struct-constraints-and-limitations.md))
  applies transitively to a type parameter that allows one.
- `ref struct` types still cannot implement interfaces before this same C# 13 release's separate
  (related but distinct) relaxation — check current documentation for exact interface-
  implementation rules if constraining `TSpan` by interface, since that's a different feature than
  the anti-constraint covered here.

## Fallback

No equivalent before C# 13. A generic type or method that needs to accept a `ref struct` argument
on an older `LangVersion` has two options: drop generics for that parameter entirely and write a
non-generic overload taking `Span<T>`/the concrete `ref struct` type directly, or accept the
parameter unconstrained and simply document that it can never be instantiated with a `ref struct`
type argument — the compiler enforces that restriction automatically pre-13 with CS0306, so no
runtime check is needed, only the API design accommodation of a separate overload per shape.
