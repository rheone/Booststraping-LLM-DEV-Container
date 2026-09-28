# Implementing a Custom Numeric Type

Implement the generic math interfaces on a custom type (a `Fraction`, a fixed-point decimal, a
units-of-measure wrapper) to let it participate in the same generic algorithms the built-in numeric
types do — anything written against `where T : INumber<T>` accepts your type for free once it
correctly implements the interface.

## Minimal `INumber<T>` implementation shape

`INumber<T>` pulls in a large number of required static abstract members transitively through
`INumberBase<T>` and the operator interfaces it composes. Implement it on a small custom type to
see the full shape:

```csharp
public readonly struct Fraction : INumber<Fraction>
{
    public int Numerator { get; }
    public int Denominator { get; }

    public Fraction(int numerator, int denominator)
    {
        var gcd = Gcd(Math.Abs(numerator), Math.Abs(denominator));
        Numerator = numerator / gcd;
        Denominator = denominator / gcd;
    }

    public static Fraction Zero => new(0, 1);
    public static Fraction One => new(1, 1);
    public static int Radix => 2;

    public static Fraction operator +(Fraction left, Fraction right) =>
        new(left.Numerator * right.Denominator + right.Numerator * left.Denominator,
            left.Denominator * right.Denominator);

    public static Fraction operator -(Fraction left, Fraction right) =>
        new(left.Numerator * right.Denominator - right.Numerator * left.Denominator,
            left.Denominator * right.Denominator);

    public static Fraction operator *(Fraction left, Fraction right) =>
        new(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

    public static Fraction operator /(Fraction left, Fraction right) =>
        new(left.Numerator * right.Denominator, left.Denominator * right.Numerator);

    public static bool operator ==(Fraction left, Fraction right) =>
        left.Numerator == right.Numerator && left.Denominator == right.Denominator;

    public static bool operator !=(Fraction left, Fraction right) => !(left == right);

    public static bool operator <(Fraction left, Fraction right) =>
        left.Numerator * right.Denominator < right.Numerator * left.Denominator;

    public static bool operator >(Fraction left, Fraction right) => right < left;
    public static bool operator <=(Fraction left, Fraction right) => !(right < left);
    public static bool operator >=(Fraction left, Fraction right) => !(left < right);

    // ...CreateChecked/CreateSaturating/CreateTruncating, TryParse/Parse, Abs, IsCanonical,
    // IsZero and the other INumberBase<T> query members, plus IEquatable<Fraction>/IComparable<Fraction>...

    public override bool Equals(object? obj) => obj is Fraction other && this == other;
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    private static int Gcd(int a, int b) => b == 0 ? Math.Max(a, 1) : Gcd(b, a % b);
}
```

The elided members (`CreateChecked`, `TryParse`, `IsZero`, `IsCanonical`, and similar) are real
required members of `INumberBase<T>` in a genuinely complete implementation — the compiler reports
every one that's missing, so treat that as the authoritative checklist rather than trying to
memorize the full member list.

## Implement only the interfaces the type honestly supports

Don't implement `INumber<T>` on a type that can't support a total ordering, or `ISignedNumber<T>`
on a type that has no meaningful negative value — implement the narrowest interface that's
genuinely true of the type (`INumberBase<T>` alone for something `Complex`-shaped with no
ordering), the same way `System.Numerics.Complex` itself only implements `INumberBase<T>` and not
`INumber<T>`. A caller writing `where T : INumber<T>` is relying on every member of that interface
actually behaving sensibly, not just compiling.

## `TryParse`/`Parse` need a real implementation, not a stub

A common shortcut mistake is implementing `Parse`/`TryParse` by throwing
`NotSupportedException` "for now" — this silently breaks any generic algorithm that legitimately
calls `T.Parse` (input parsing, deserialization helpers written generically over `INumber<T>`).
Either implement real parsing for the type's textual format or, if the type genuinely has no sane
textual representation, document that gap explicitly rather than leaving a silent trap for
generic-algorithm callers.

## `Radix`

`INumberBase<T>.Radix` reports the base the type's internal representation uses (`2` for ordinary
binary floating-point/integer types, `10` for a decimal-based type). Get this right for any custom
type feeding into `TensorPrimitives` or other library code that branches on it — reporting the
wrong radix produces algorithms that quietly assume the wrong precision/rounding behavior for the
type.
