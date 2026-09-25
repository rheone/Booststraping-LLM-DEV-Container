# Generic Methods as Lambdas and Method Groups

A generic method can supply a delegate exactly like a non-generic one — the generic method must
already be **closed** (every type parameter fixed, explicitly or by inference) at the point it
converts to a delegate. This file works through that composition: passing a closed generic method
as a method group, writing a generic method that accepts a lambda/delegate parameter, and how
custom generic delegate types relate to `Func<>`/`Action<>`.

## Basic: a closed generic method group as a delegate

```csharp
public static class Parser
{
    public static bool TryParseValue<T>(string text, out T result) where T : IParsable<T>
    {
        return T.TryParse(text, null, out result!);
    }
}

public delegate bool TryParser<T>(string text, out T result);

// the type argument must be fixed BEFORE the method group converts to a delegate — a delegate
// can't itself stay generic over T the way Parser.TryParseValue<T> is
TryParser<int> parseInt = Parser.TryParseValue<int>; // method group conversion infers T = int
                                                       // from TryParser<int>'s own signature
```

There's no delegate type that stays generic the way `TryParseValue<T>` itself is — every delegate
instance is closed over one specific set of type arguments, fixed here by explicitly writing
`TryParseValue<int>` and matching it against `TryParser<int>`'s own signature.

## Basic: a generic method taking a delegate parameter

```csharp
public static IEnumerable<TResult> Map<TSource, TResult>(
    IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    foreach (TSource item in source)
    {
        yield return selector(item);
    }
}

IEnumerable<string> labels = Map(new[] { 1, 2, 3 }, n => $"Item {n}"); // TSource, TResult both inferred
```

This is the shape every LINQ-style method uses: a generic method whose type parameters are inferred
from the combination of the source sequence's element type and the lambda's own inferred parameter
and return types — neither `TSource` nor `TResult` needs to be written explicitly at the call site.

## Advanced: overload resolution between a lambda and a method group for the same generic parameter

```csharp
public static class Comparers
{
    public static int ByLength(string a, string b) => a.Length.CompareTo(b.Length);
}

public static T[] SortBy<T>(T[] items, Comparison<T> comparer)
{
    T[] copy = (T[])items.Clone();
    Array.Sort(copy, comparer);
    return copy;
}

string[] byLength = SortBy(new[] { "ccc", "a", "bb" }, Comparers.ByLength); // method group
string[] byLengthDesc = SortBy(new[] { "ccc", "a", "bb" }, (a, b) => Comparers.ByLength(b, a)); // lambda
```

Both a method group and a lambda satisfy `Comparison<T>` identically from the caller's perspective —
`SortBy<T>`'s type inference doesn't care which syntax supplied the delegate, only that the supplied
value converts to `Comparison<T>`. Prefer whichever reads more clearly at the call site: a method
group when an existing named method already does exactly the right thing, a lambda when the logic
is call-site-specific or needs to close over local state.

## Advanced: a custom generic delegate vs. reaching for `Func<>`

```csharp
public delegate TResult BinaryOperation<T, TResult>(T left, T right);

public static TResult Reduce<T, TResult>(
    IEnumerable<T> source, TResult seed, Func<TResult, T, TResult> accumulator)
{
    TResult result = seed;
    foreach (T item in source)
    {
        result = accumulator(result, item);
    }
    return result;
}

int sum = Reduce(new[] { 1, 2, 3, 4 }, 0, (acc, n) => acc + n); // Func<> reused rather than a
                                                                  // custom BinaryOperation<T,TResult>
```

`Reduce<T, TResult>` reaches for `Func<TResult, T, TResult>` instead of declaring
`BinaryOperation<T, TResult>` — the generic `Func<>` family covers the shape perfectly, and a custom
delegate only earns its keep when the name itself communicates something `Func<>` can't (see
[references/csharp2-anonymous-methods-and-generic-delegates.md](../references/csharp2-anonymous-methods-and-generic-delegates.md)
for the original `Predicate<T>`/`Converter<TInput,TResult>`-vs-custom-delegate framing this extends).

## A method-group-conversion performance note (C# 11+)

Every method group conversion to a delegate allocated a fresh delegate object on C# 10 and earlier,
even for the same static method converted repeatedly (for example, inside a loop or a hot path).
From C# 11 (.NET 7, November 2022) onward, the compiler caches the delegate instance for a
method-group conversion of a `static` method and reuses it, matching the allocation profile lambdas
already had for non-capturing bodies. This doesn't change any syntax or generic-inference behavior
above — `Parser.TryParseValue<int>` and `Comparers.ByLength` above both benefit from this caching
automatically on C# 11+ without any code change.

## Fallback

Generic methods as method groups and as lambda-consuming parameters both work from C# 2.0 onward
(method group conversion) and C# 3.0 onward (lambda syntax) — see
[references/csharp2-anonymous-methods-and-generic-delegates.md](../references/csharp2-anonymous-methods-and-generic-delegates.md)
and
[references/csharp3-lambdas-and-func-action.md](../references/csharp3-lambdas-and-func-action.md).
The method-group delegate-caching improvement is a C# 11 compiler optimization with no source-level
fallback needed — code compiled under an earlier `<LangVersion>` is identical source, just less
efficient IL.
