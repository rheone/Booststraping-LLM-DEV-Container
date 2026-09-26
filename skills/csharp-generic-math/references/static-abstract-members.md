# Static Abstract Members in Interfaces

Static abstract (and static virtual) interface members are the C# 11 / .NET 7 language mechanism
generic math is built on: an interface can declare a static member — including an operator — that
every implementing type must supply its own version of, and a generic type parameter constrained to
that interface can call the member through the type parameter itself.

## Declaring one

```csharp
public interface IHasIdentity<TSelf> where TSelf : IHasIdentity<TSelf>
{
    static abstract TSelf Identity { get; }
    static abstract TSelf operator +(TSelf left, TSelf right);
}
```

- `static abstract` — the implementing type must provide its own implementation; there's no default
  body, mirroring an ordinary abstract instance member but for a static one.
- `static virtual` — the implementing type may provide its own implementation but the interface can
  supply a default body, mirroring a default interface method but for a static member.
- Operators must be declared `static` in C# regardless of context — this is exactly why a *static*
  abstract member is what's needed to put an overloadable operator on an interface at all; an
  instance-level abstract member couldn't express `+` as an operator.

## The curiously-recurring `TSelf` constraint

`where TSelf : IHasIdentity<TSelf>` (a type constraining itself as its own type parameter) is the
standard shape for a static-abstract-member interface — it lets `TSelf.Identity` and
`left + right` resolve to a concrete type's own implementation rather than some unrelated type
also implementing the interface. `INumber<T>` and every interface in the generic math hierarchy
follow this exact shape (`INumber<TSelf> where TSelf : INumber<TSelf>`), which is also why the
constraint on a generic method using it look like `where T : INumber<T>` — `T` is standing in for
`TSelf`.

## Calling a static abstract member

Only reachable through a type parameter constrained to the declaring interface — not through the
interface type itself, and not through a concrete type as if it were an ordinary static member
unless that type happens to also expose the member directly under its own name:

```csharp
public static TSelf CombineWithIdentity<TSelf>(TSelf value) where TSelf : IHasIdentity<TSelf> =>
    TSelf.Identity + value;
```

`IHasIdentity<int>.Identity` (calling through the constructed interface type directly) is not valid
C# — the whole point of the mechanism is dispatch through a type parameter, resolved at the
concrete type each caller supplies, not through the interface as a fixed target.

## Implementing a static abstract member on your own type

```csharp
public readonly struct Meters : IHasIdentity<Meters>
{
    public double Value { get; init; }

    public static Meters Identity => new() { Value = 0 };

    public static Meters operator +(Meters left, Meters right) =>
        new() { Value = left.Value + right.Value };
}
```

This is exactly the pattern [implementing-a-custom-numeric-type.md](implementing-a-custom-numeric-type.md)
walks through at full scale against the real `INumber<T>` hierarchy rather than a toy interface.

## Why this matters beyond generic math

Static abstract members are a general-purpose language feature — generic math is its first and by
far most visible BCL application, but the mechanism itself applies to any interface where "every
implementing type supplies its own version of a static factory, constant, or operator" is the
shape needed (a parser interface exposing a static `Parse` factory, for instance). This file covers
the mechanism only as it applies to writing or consuming the numeric interfaces; a from-scratch
custom interface using the same mechanism for a non-numeric purpose follows the same rules shown
here.
