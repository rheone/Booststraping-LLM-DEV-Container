# Numeric Interface Hierarchy

All generic math interfaces live in `System.Numerics` (BCL, .NET 7+) and every built-in numeric
type (`int`, `long`, `float`, `double`, `decimal`, `BigInteger`, `Complex`, and the rest) implements
whichever subset genuinely applies to it. Choose a constraint by asking what operations the
algorithm actually needs — a narrower interface accepts fewer types but promises the caller less
than the algorithm doesn't need.

## The core chain

- **`INumberBase<T>`** — the root. Additive/multiplicative identities (`Zero`, `One`), parsing
  (`Parse`/`TryParse`), conversion (`CreateChecked`/`CreateSaturating`/`CreateTruncating`),
  `Abs`/`Max`/`Min`, and the basic arithmetic operator interfaces
  (`IAdditionOperators<T,T,T>`, `ISubtractionOperators<T,T,T>`, etc.). Every other interface below
  builds on this one.
- **`INumber<T>`** — adds full ordering (`IComparisonOperators<T,T,bool>`) and
  `Clamp`/`CopySign`/`Sign` on top of `INumberBase<T>`. This is the default, broadest-but-still-
  useful constraint for "any ordinary number" — reach for it first unless a narrower or wider
  interface is specifically called for.
- **`ISignedNumber<T>`** — adds `NegativeOne` for types that can represent negative values. Not
  implemented by unsigned integer types.
- **`IUnsignedNumber<T>`** — a marker interface with no members of its own; use it as a constraint
  when an algorithm must reject signed types entirely (a byte-packing routine, a hash accumulator
  that assumes no negative values).

## Integer-specific

- **`IBinaryInteger<T>`** — bitwise operators (`IBitwiseOperators<T,T,T>`), shifting, bit-counting
  (`PopCount`, `LeadingZeroCount`, `TrailingZeroCount`), and big-endian/little-endian byte
  conversion. Implemented by `int`, `long`, `BigInteger`, and the other integer types, not by
  floating-point types.

## Floating-point-specific

- **`IFloatingPointConstants<T>`** — `E`, `Pi`, `Tau` as static abstract properties.
- **`IFloatingPoint<T>`** — rounding (`Round`, `Ceiling`, `Floor`, `Truncate`) and
  bit-representation queries.
- **`IFloatingPointIeee754<T>`** — the fullest floating-point contract: `NaN`, `PositiveInfinity`,
  `NegativeInfinity`, `Epsilon`, transcendental functions (`Sqrt`, `Exp`, `Log`, trigonometric
  functions via the separately-composed `ITrigonometricFunctions<T>`/`IExponentialFunctions<T>`/
  `IHyperbolicFunctions<T>`/`ILogarithmicFunctions<T>`/`IPowerFunctions<T>`/`IRootFunctions<T>`
  interfaces). Implemented by `float` and `double`; not by `decimal`, which trades IEEE 754
  semantics for exact base-10 arithmetic.

## `IMinMaxValue<T>`

Separate from the arithmetic chain — exposes `MinValue`/`MaxValue` as static abstract properties.
Implemented by every fixed-range built-in numeric type; not implemented by types with no fixed
range (`BigInteger`).

## Choosing among them

| Need | Constrain to |
| --- | --- |
| Basic arithmetic, works for anything number-like including complex/imaginary values | `INumberBase<T>` |
| Basic arithmetic plus ordering/comparison — the default choice | `INumber<T>` |
| Must reject unsigned types (needs a genuine negative range) | `ISignedNumber<T>` |
| Must reject signed types | `IUnsignedNumber<T>` |
| Bitwise operations, shifting, bit counting | `IBinaryInteger<T>` |
| Rounding behavior | `IFloatingPoint<T>` |
| Transcendental functions (`Sqrt`, trigonometry, logarithms) | `IFloatingPointIeee754<T>` (or the specific function interface it composes, e.g. `IRootFunctions<T>` alone for just `Sqrt`/`RootN`) |
| A type's min/max bound | `IMinMaxValue<T>` |

Combine interfaces with `where T : INumber<T>, IMinMaxValue<T>` when an algorithm genuinely needs
both facets — constraints compose with an ordinary comma-separated `where` clause like any other
interface constraint.

## `Complex` is a deliberate outlier

`System.Numerics.Complex` implements `INumberBase<T>` but not `INumber<T>` — complex numbers have
no total ordering, so `INumber<T>`'s comparison operators would be meaningless for it. An algorithm
constrained to `INumber<T>` intentionally excludes `Complex`; one that only needs `INumberBase<T>`
accepts it.
