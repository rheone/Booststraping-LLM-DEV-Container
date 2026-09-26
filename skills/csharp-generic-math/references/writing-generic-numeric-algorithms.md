# Writing Generic Numeric Algorithms

Constrain a type parameter to `INumber<T>` (or a narrower interface — see
[numeric-interface-hierarchy.md](numeric-interface-hierarchy.md)) to write an algorithm once and
run it over every built-in numeric type plus any custom type implementing the same interfaces.

## Basic shape

```csharp
public static T Average<T>(IReadOnlyList<T> values) where T : INumber<T>
{
    var total = T.Zero;
    foreach (var value in values)
    {
        total += value;
    }
    return total / T.CreateChecked(values.Count);
}
```

- `T.Zero` — a static abstract property from `INumberBase<T>` (inherited by `INumber<T>`); every
  conforming type supplies its own additive identity.
- `+=` — resolves to the type's static abstract `+` operator.
- `T.CreateChecked(values.Count)` — converts an `int` count into `T`, throwing
  `OverflowException` if `T` can't represent the value (e.g. converting `int.MaxValue` into a
  narrow custom type). `CreateSaturating` and `CreateTruncating` are the non-throwing alternatives
  when overflow should clamp or wrap instead of fail.

## Comparisons and min/max

`INumber<T>` implements `IComparisonOperators<T, T, bool>`, so ordinary `<`, `>`, `<=`, `>=`
comparisons work directly on `T`:

```csharp
public static T Max<T>(T left, T right) where T : INumber<T> => left > right ? left : right;
```

`INumberBase<T>` also exposes static `Max`/`Min`/`MaxNumber`/`MinNumber` members directly — prefer
`T.Max(left, right)` over hand-rolling a ternary when it's available, since the interface's own
implementation already handles `T`'s NaN/negative-zero edge cases correctly for floating-point
types where a naive `<` comparison would not.

## Parsing and formatting generically

`INumberBase<T>` includes static abstract `Parse`/`TryParse` members, so a generic method can parse
user input into whatever `T` the caller asked for without a per-type `switch`:

```csharp
public static T ParseOrDefault<T>(string text, T fallback) where T : INumber<T> =>
    T.TryParse(text, CultureInfo.InvariantCulture, out var result) ? result : fallback;
```

## Working with constants beyond zero/one

`INumberBase<T>` supplies `Zero` and `One`; for any other constant your algorithm needs (a
threshold, an epsilon), take it as an ordinary `T` parameter rather than trying to conjure it from
the interface — the interfaces expose the identities every number-like type can supply, not
arbitrary literals.

```csharp
public static bool IsNearZero<T>(T value, T epsilon) where T : INumber<T> =>
    T.Abs(value) < epsilon;
```

## Later BCL additions built on the same interfaces

.NET 9 substantially expanded `System.Numerics.Tensors.TensorPrimitives`, growing its set of
`Span<T>`-based numerical operations generic over `INumber<T>` (and related interfaces like
`IFloatingPointIeee754<T>`) from roughly 40 to nearly 200 overloads — the same interface hierarchy
this skill covers, applied to bulk operations over spans of values rather than single scalars.
Reach for `TensorPrimitives` once an algorithm operates over a whole `Span<T>`/`ReadOnlySpan<T>` of
numeric values rather than one value at a time; its operations are SIMD-optimized where the
runtime and `T` support it, which a hand-written `foreach` loop over the same interfaces is not.

## Pitfall: `T.Zero`/`T.One` inside a `static` context that isn't itself generic

Static abstract members are only callable through a generic type parameter constrained to the
declaring interface (`T.Zero` where `T : INumber<T>`) — you cannot call `INumber<int>.Zero`
directly as if it were an ordinary static member access on a concrete closed type in the same way
you'd call `int.MaxValue`. Reach for the built-in type's own members (`int.MinValue`) when working
with one concrete, known type; reach for the interface's static abstract members only from inside
code that is itself generic over `T`.
