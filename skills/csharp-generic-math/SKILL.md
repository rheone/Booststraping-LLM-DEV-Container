---
name: csharp-generic-math
description: 'Guidance on C# generic math — writing algorithms generic over numeric types using INumber<T> and the wider .NET numeric interface hierarchy (INumberBase<T>, ISignedNumber<T>, IFloatingPoint<T>, IBinaryInteger<T>, and friends), built on static abstract/virtual interface members (verified: the language feature and this interface hierarchy shipped together in C# 11 / .NET 7), plus later BCL additions built on the same interfaces (verified: .NET 9''s expanded TensorPrimitives generic-over-INumber<T> span operations). Use when writing a numeric algorithm that should work across int/long/double/decimal/custom number types without duplicating it per type, choosing which numeric interface to constrain a generic parameter to, implementing a custom numeric type against these interfaces, or writing/reviewing a static abstract or static virtual interface member.'
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Generic Math

Generic math lets a generic method or type express "any number-like type" as a constraint
(`where T : INumber<T>`) and then use ordinary arithmetic operators, comparisons, and numeric
conversions on values of that type — no reflection, no `dynamic`, no per-type overload
duplication. It shipped as one feature: the `INumber<T>`-and-friends interface hierarchy in the
.NET 7 base class library, built on the static abstract/virtual interface members language
mechanism introduced in C# 11. Organized by task, not by version — this is a single feature with
minor library additions since, each noted inline where it matters, not a family of per-version
tiers.

## Quick start (C# 11 / .NET 7+)

```csharp
public static T Sum<T>(IEnumerable<T> values) where T : INumber<T>
{
    var total = T.Zero;
    foreach (var value in values)
    {
        total += value;
    }
    return total;
}

Sum(new[] { 1, 2, 3 });          // int
Sum(new[] { 1.5, 2.5 });         // double
Sum(new[] { 1m, 2m, 3m });       // decimal
```

`T.Zero` and `+=` both resolve through `INumber<T>`'s static abstract members — `Zero` is a static
abstract property, `+` a static abstract operator, and the compiler dispatches each to whichever
concrete `T` the caller supplies, with no boxing and no runtime type check.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Writing an algorithm generic over `int`/`double`/`decimal`/etc. using `INumber<T>` | [references/writing-generic-numeric-algorithms.md](references/writing-generic-numeric-algorithms.md) |
| Choosing the right interface (`INumberBase<T>`, `ISignedNumber<T>`, `IFloatingPoint<T>`, `IBinaryInteger<T>`, ...) for a constraint | [references/numeric-interface-hierarchy.md](references/numeric-interface-hierarchy.md) |
| Understanding static abstract/virtual interface members, the language mechanism generic math is built on | [references/static-abstract-members.md](references/static-abstract-members.md) |
| Implementing a custom numeric type (a `Fraction`, a fixed-point type, a units-of-measure wrapper) against these interfaces | [references/implementing-a-custom-numeric-type.md](references/implementing-a-custom-numeric-type.md) |
| Testing a generic-math algorithm or a custom `INumber<T>` implementation | [references/testing.md](references/testing.md) |

## Out of scope

- Generic type/method fundamentals unrelated to numeric constraints (`where T : class`, variance,
  ordinary generic collections) — this skill covers only the numeric-interface hierarchy and the
  static-abstract-member mechanism it depends on.
- `System.Numerics.Vector<T>`/SIMD intrinsics as a performance topic in their own right — covered
  here only insofar as `TensorPrimitives` (noted in
  [references/writing-generic-numeric-algorithms.md](references/writing-generic-numeric-algorithms.md))
  is itself built on the generic-math interfaces; general SIMD vectorization technique is out of
  scope.
- `System.Numerics.BigInteger`/`Complex` as types in their own right beyond how they implement the
  generic math interfaces — their non-generic-math API surface is out of scope.
