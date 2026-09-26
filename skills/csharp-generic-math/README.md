# Generic Math

Guidance on C#'s generic math feature — `INumber<T>` and the wider numeric interface hierarchy,
built on static abstract/virtual interface members. The routing table (by task, not version) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per C# version

| File | Covers |
| --- | --- |
| `writing-generic-numeric-algorithms.md` | Writing methods/types generic over `INumber<T>`; the .NET 9 `TensorPrimitives` extension of the same interfaces |
| `numeric-interface-hierarchy.md` | `INumberBase<T>`, `INumber<T>`, `ISignedNumber<T>`, `IUnsignedNumber<T>`, `IFloatingPoint<T>`, `IBinaryInteger<T>`, `IBinaryFloatingPointIeee754<T>`, and how they relate |
| `static-abstract-members.md` | The static abstract/virtual interface members language mechanism generic math depends on |
| `implementing-a-custom-numeric-type.md` | Implementing the interfaces on a custom type (a `Fraction`, a fixed-point number) |
| `testing.md` | Testing a generic-math algorithm across multiple `T`, and testing a custom numeric type's interface implementation |

## Scope

C#'s generic math feature only: the `INumber<T>`-and-friends BCL interface hierarchy and the
static abstract/virtual interface members language mechanism it's built on. Out of scope: generic
type/method fundamentals unrelated to numeric constraints, SIMD/vectorization technique in its own
right, and the non-generic-math API surface of `BigInteger`/`Complex`.

This is a single feature (shipped together in C# 11 / .NET 7) with minor BCL additions since
(noted inline where relevant); version is not the file-splitting axis for this skill (see
[SKILL.md](SKILL.md) for why).
