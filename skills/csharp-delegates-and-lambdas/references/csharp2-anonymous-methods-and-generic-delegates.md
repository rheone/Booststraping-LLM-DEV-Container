# Anonymous Methods and Generic Delegates (C# 2.0 / .NET Framework 2.0)

C# 2.0 (.NET Framework 2.0, November 2005) shipped generics and anonymous methods together, and
delegates absorbed both immediately: the BCL added generic delegate types (`Predicate<T>`,
`Comparison<T>`, `Converter<TInput, TOutput>` — the ancestors of `Func<>`/`Action<>`, which don't
arrive until C# 3.0/4.0), and the language added `delegate { ... }` blocks plus implicit method
group conversion, removing the `new Operation(Method)` ceremony from
[csharp1-delegates-and-multicast.md](csharp1-delegates-and-multicast.md).

## Syntax

```csharp
Predicate<int> isEven = delegate(int n) { return n % 2 == 0; };
```

An anonymous method is a `delegate` block assignable to any delegate type whose signature it
matches — no separate named method declaration required.

## Basic use case: generic BCL delegates

```csharp
public delegate TResult Converter<TInput, TResult>(TInput input);

List<int> numbers = new List<int> { 1, -2, 3, -4 };

Predicate<int> isNegative = delegate(int n) { return n < 0; };
List<int> negatives = numbers.FindAll(isNegative);

Converter<int, string> toDisplay = delegate(int n) { return $"#{n}"; };
List<string> labels = numbers.ConvertAll(toDisplay);
```

`Predicate<T>`, `Comparison<T>`, and `Converter<TInput, TResult>` are themselves generic delegate
types — every one of the `Array`/`List<T>` methods that takes one (`Find`, `FindAll`, `Sort`,
`ConvertAll`, `TrueForAll`, ...) is a generic method parameterized over the collection's element
type, taking a generic delegate as its callback argument.

## Advanced use case: implicit method group conversion + closures

C# 2.0 also allows a method group to convert to a matching delegate type without `new`, and an
anonymous method can capture (close over) local variables from its enclosing scope:

```csharp
public delegate bool Matcher<T>(T candidate);

public static Matcher<T> MakeEqualityMatcher<T>(T target) where T : IEquatable<T>
{
    // captures `target` — the anonymous method below is a closure over the enclosing method's parameter
    return delegate(T candidate) { return candidate.Equals(target); };
}

Matcher<string> matchesFoo = MakeEqualityMatcher("foo");
bool found = Array.Exists(new[] { "bar", "foo", "baz" }, delegate(string s) { return matchesFoo(s); });
```

```csharp
// method group conversion — C# 1.0 required `new Predicate<int>(IsPositive)` here
static bool IsPositive(int n) => n > 0; // (C# 6.0+ expression-bodied method syntax used for brevity)
Predicate<int> isPositive = IsPositive;
```

Closures are the foundation every later tier's lambda captures build on; the pitfalls (loop
variables, mutable captured state, lifetime) are covered fully in
[specialized/closures-and-variable-capture.md](../specialized/closures-and-variable-capture.md).

## Requirements and restrictions

- An anonymous method with an empty parameter list, `delegate { ... }`, matches *any* delegate
  signature (the parameters are simply ignored) — useful for `event` handlers where the sender/args
  aren't needed.
- Anonymous methods cannot use `ref`/`out` captured variables the way ordinary parameters can be
  passed by reference across the closure boundary in the way later C# versions eventually restrict
  further (see [csharp14-lambda-parameter-modifiers.md](csharp14-lambda-parameter-modifiers.md) for
  what those modifiers look like on lambdas).
- Generic delegate types follow the same variance-free rules generic classes do at this tier — `in`
  `/`out` annotations don't exist until C# 4.0
  ([csharp4-variance-and-extended-func-action.md](csharp4-variance-and-extended-func-action.md)).

## Fallback

On .NET Framework 1.0/1.1 (C# 1.0), there is no generics, so `Predicate<T>`/`Converter<TInput,
TResult>` don't exist — declare a non-generic delegate type per concrete type instead
(`public delegate bool IntPredicate(int candidate);`), and replace every anonymous method with a
named method plus `new IntPredicate(MethodName)`, per
[csharp1-delegates-and-multicast.md](csharp1-delegates-and-multicast.md).
