# `Span<T>`/`Memory<T>` as a Concern When Writing Tests

This file is about how `Span<T>`/`ref struct` semantics show up *while authoring tests* — asserting
on span contents without allocating a comparison array, testing production methods that accept
`Span<T>` parameters, why a `ref struct` (or anything wrapping one) can't be captured in a test
framework's lambda-based assertion helpers or used inside an `async Task` test method, and
`stackalloc`-backed scratch buffers for test setup. It is not a tutorial on testing this skill's own
`Span<T>`/`ref struct` syntax examples.

## Basic: asserting on span contents without allocating a comparison array

```csharp
[Fact]
public void Normalize_Test_ZerosTheMean()
{
    Span<double> values = stackalloc double[] { 1.0, 2.0, 3.0 };

    BufferOps.Normalize(values);

    Assert.Equal(0.0, values[0] + values[1] + values[2], precision: 10);
    // or, comparing element-by-element against an expected ReadOnlySpan<double>:
    ReadOnlySpan<double> expected = stackalloc double[] { -1.0, 0.0, 1.0 };
    Assert.True(values.SequenceEqual(expected));
}
```

`Span<T>.SequenceEqual` (an instance method on the span types themselves, not a LINQ extension —
see [ref-struct-constraints-and-limitations.md](ref-struct-constraints-and-limitations.md) for why
LINQ's delegate-based operators don't apply here at all) compares contents without either side
being converted to an array first. Most assertion libraries' generic `Assert.Equal<T>(T, T)`
overloads don't have a `Span<T>`-specific overload (since `Span<T>` can't be a generic type argument
before C# 13 — see
[csharp13-allows-ref-struct.md](../references/csharp13-allows-ref-struct.md) — so a library predating
that release structurally cannot offer one), which is why comparing via `SequenceEqual` and
asserting the resulting `bool`, rather than handing the span itself to a generic equality assertion,
is the idiomatic pattern.

## Basic: testing a production method that accepts `Span<T>`

```csharp
public static class TextOps
{
    public static int CountVowels(ReadOnlySpan<char> text) { /* ... */ }
}

[Theory]
[InlineData("hello world", 3)]
[InlineData("xyz", 0)]
public void CountVowels_Test(string input, int expected) =>
    Assert.Equal(expected, TextOps.CountVowels(input)); // string -> ReadOnlySpan<char> implicit conversion
```

Test data itself is almost always easiest to express as ordinary `string`/array literals in
`[InlineData]`/`[MemberData]` — theory data sources are evaluated outside the test method and
serialized/compared by the test runner's infrastructure, which cannot hold a `ref struct` value
(see the next section), so the `ReadOnlySpan<char>` conversion happens inside the test body, not in
the data source itself.

## Why a `ref struct` (or `Span<T>`) cannot be used in an `async Task` test method or captured in a test lambda

```csharp
// compile error: cannot declare a Span<T> local inside an async method body
// unless it's confined entirely between awaits — and it CANNOT be a parameter at all
[Fact]
public async Task ProcessAsync_Test_Throws_WhenEmpty()
{
    Span<byte> empty = Span<byte>.Empty; // fine on its own, but...
    // await Task.Delay(1); // ...adding this makes `empty` illegal to reference afterward
    await Assert.ThrowsAsync<ArgumentException>(() =>
    {
        // compile error: cannot use 'empty' — a Span<T> — inside a lambda passed to ThrowsAsync
        // return ProcessAsync(empty);
        return Task.CompletedTask; // the lambda itself can't capture a ref struct at all
    });
}
```

This is a real, easy-to-hit gotcha specific to `Span<T>`-accepting production APIs: `xUnit`'s
`Assert.ThrowsAsync`, `Assert.Throws` with a closure, and any assertion helper shaped as
"pass me a delegate to invoke" cannot receive a `Span<T>` through that delegate's captured state,
because the delegate is a compiler-generated closure class — exactly the boxed/heap-stored shape a
`ref struct` can never occupy (see
[ref-struct-constraints-and-limitations.md](ref-struct-constraints-and-limitations.md)). The fix is
to call the `Span<T>`-accepting method directly and wrap only the *synchronous* call in a
`try`/`catch`, or restructure the production API under test so the exception-throwing check has a
non-`Span<T>` synchronous entry point:

```csharp
[Fact]
public void Process_Test_Throws_WhenEmpty()
{
    Span<byte> empty = Span<byte>.Empty;
    Assert.Throws<ArgumentException>(() => Process(empty)); // Assert.Throws (sync) — but still can't
    // capture `empty` in the lambda above either; this still fails to compile for the same reason.

    // actually-working form: no delegate at all, ordinary try/catch
    try
    {
        Process(empty);
        Assert.Fail("Expected ArgumentException.");
    }
    catch (ArgumentException)
    {
        // expected
    }
}
```

The lambda-based `Assert.Throws(Action)` form fails to compile the moment the action's body
references a captured `Span<T>` local, regardless of sync or async — a plain `try`/`catch` around
the direct call is the reliable pattern whenever the code under test takes a `Span<T>` parameter.

## Advanced: `stackalloc`-backed scratch buffers in test helpers

```csharp
public static class SpanTestHelpers
{
    public static bool RoundTrips(ReadOnlySpan<byte> original, Func<Span<byte>, int> decode, Func<ReadOnlySpan<byte>, Span<byte>, int> encode)
    {
        Span<byte> scratch = stackalloc byte[256]; // test-local scratch buffer, no heap allocation
        int written = encode(original, scratch);
        Span<byte> decoded = stackalloc byte[256];
        int decodedLength = decode(scratch[..written]);
        return scratch[..written].SequenceEqual(decoded[..decodedLength]);
    }
}
```

A `stackalloc` scratch buffer inside a test helper method works exactly like production code — it's
still confined to that method's stack frame, still can't be returned or stored, and still can't
cross an `await` if the helper is itself `async`. It's a useful pattern specifically for
round-trip/encode-decode test helpers that need working memory without cluttering assertions with
heap allocations the production code path doesn't have either — keeping the test's allocation
profile representative of the real call path it exercises.

## Fallback

Below [C# 7.2 / `Span<T>`'s BCL availability](../references/csharp7.2-ref-struct-and-span.md), test
against `ArraySegment<T>` or plain arrays/`offset, count` parameters instead — see
[pre-csharp7-arrays-and-pointers.md](../references/pre-csharp7-arrays-and-pointers.md). None of the
capture/async restrictions above apply to an ordinary array or `ArraySegment<T>`, since neither is a
`ref struct` — they can be captured in lambdas and held across `await` freely, at the cost of the
allocation/aliasing guarantees `Span<T>` exists to provide.
