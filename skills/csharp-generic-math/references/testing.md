# Testing

Generic-math code has two distinct testable surfaces: an algorithm written generic over `T`, and a
custom type implementing the numeric interfaces. Test both, but not the same way.

## Testing a generic algorithm across multiple `T`

The whole point of a generic-math algorithm is that it behaves correctly for every `T` a caller
might supply — so the test suite should call it with more than one concrete numeric type rather
than proving it once against `int` and assuming the rest follow:

```csharp
[Theory]
[InlineData(new[] { 1, 2, 3 }, 2)]
public void Average_Int_ReturnsExpected(int[] values, int expected) =>
    Average<int>(values).Should().Be(expected);

[Theory]
[InlineData(new[] { 1.0, 2.0, 4.0 }, 7.0 / 3)]
public void Average_Double_ReturnsExpected(double[] values, double expected) =>
    Average<double>(values).Should().BeApproximately(expected, 1e-9);

[Theory]
[InlineData(new[] { 1m, 2m, 3m }, 2)]
public void Average_Decimal_ReturnsExpected(decimal[] values, decimal expected) =>
    Average<decimal>(values).Should().Be(expected);
```

Cover at least one integer type, one floating-point type, and `decimal` if the algorithm is meant
to support it — floating-point and decimal exercise materially different rounding and precision
paths through the same generic code, and an algorithm that only ever compiled and ran against
`int` in its test suite has not actually verified the "generic" part of generic math.

## Testing a custom numeric type's interface implementation

Test a custom `INumber<T>` implementation (see
[implementing-a-custom-numeric-type.md](implementing-a-custom-numeric-type.md)) against the
contract each interface member promises, not just against the type's own hand-written arithmetic
semantics in isolation:

```csharp
[Fact]
public void Zero_IsAdditiveIdentity()
{
    var value = new Fraction(3, 4);

    (value + Fraction.Zero).Should().Be(value);
}

[Fact]
public void ComparisonOperators_AreConsistentWithEquality()
{
    var a = new Fraction(1, 2);
    var b = new Fraction(2, 4); // reduces to the same value as a

    (a == b).Should().BeTrue();
    (a < b).Should().BeFalse();
    (a > b).Should().BeFalse();
}

[Fact]
public void TryParse_RoundTripsToString()
{
    var original = new Fraction(3, 4);

    Fraction.TryParse(original.ToString(), null, out var parsed).Should().BeTrue();
    parsed.Should().Be(original);
}
```

The identity laws (`x + Zero == x`, `x * One == x`), comparison/equality consistency
(`a == b` implies neither `a < b` nor `a > b`), and parse/format round-tripping are the specific
properties a generic algorithm relying on `INumber<T>` implicitly assumes hold for *any* `T` —
verify them explicitly for a custom type rather than assuming the hand-written operator
implementations happen to satisfy them.

## Testing generic code called through a type parameter vs. a concrete type directly

A test that only ever instantiates the generic method with a concrete type argument
(`Average<int>(...)`) exercises exactly the same JIT-specialized code path production use with
`int` would — there's no separate "generic dispatch" layer left to test independently once a
concrete `T` is chosen, since static abstract member calls resolve to the concrete type's
implementation at that instantiation. This means no special mocking or virtualization strategy is
needed for testing generic-math code beyond calling it with the concrete types the test cares about.
