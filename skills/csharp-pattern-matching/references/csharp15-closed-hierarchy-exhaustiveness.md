# Closed Hierarchy Switch Exhaustiveness (C# 15.0)

C# 15 ships with .NET 11 (RC1 as of September 2026; GA expected November 2026 — .NET 11 RC1
carries a "go-live" support license, but details can still shift before GA). It adds the `closed`
modifier for a `class`, which fixes the set of that class's *direct* descendants at compile time
within the declaring assembly. A `switch` expression whose governing type is a closed class is
*exhaustive* — no default/discard arm needed, no CS8509 warning — once its arms handle every direct
descendant.

> **RC caveat:** verify against the installed .NET 11 SDK rather than treating this file as
> frozen; the feature is stable enough for the RC1 go-live license but the surrounding ecosystem
> (analyzers, IDE tooling) is still catching up as of this writing.

## Syntax

```csharp
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;
```

```csharp
public static string Describe(GateState state) => state switch
{
    Closed => "closed",
    Open(var percent) => $"{percent}% open",
    // No discard arm, no CS8509 warning: every direct descendant of 'GateState' is handled.
};
```

## Basic use case

```csharp
public closed record class PaymentMethod;
public record class Cash : PaymentMethod;
public record class Card(string Last4) : PaymentMethod;
public record class BankTransfer(string Iban) : PaymentMethod;

public static string Describe(PaymentMethod method) => method switch
{
    Cash => "cash",
    Card(var last4) => $"card ending {last4}",
    BankTransfer(var iban) => $"bank transfer to {iban}",
};
```

Without `closed`, this switch would need a discard arm — the compiler has no way to know
`PaymentMethod` can't have a fourth, unhandled subtype introduced later. With `closed`, adding a
new descendant of `PaymentMethod` anywhere in the declaring assembly makes every switch over
`PaymentMethod` that lacks an arm for it a compile-time warning instead of a silent runtime gap.

## Advanced use case: cross-assembly visibility and type-parameter governing types

```csharp
// Assembly 1
public closed record class Shape;
public record class Circle(double Radius) : Shape;
internal record class Triangle(double Base, double Height) : Shape;
```

A closed hierarchy's exhaustiveness is visibility-scoped: code in the same assembly sees
`Triangle` and a switch handling `Circle` and `Triangle` is exhaustive there, but code in a
different assembly only sees `Circle` — a switch there needs a discard arm (or a base-type arm) to
stay exhaustive, because `Triangle` isn't visible at that call site even though it exists.

```csharp
public static string Describe<X>(X method) where X : PaymentMethod => method switch
{
    Cash => "cash",
    Card(var last4) => $"card ending {last4}",
    BankTransfer(var iban) => $"bank transfer to {iban}",
    // No warning: 'X' is constrained to the closed type 'PaymentMethod', so every
    // direct descendant is still handled.
};
```

A generic method whose type parameter is constrained to a closed class gets the same
exhaustiveness treatment as switching on the closed class directly — this is the pattern-matching
angle on writing one generic algorithm over a closed hierarchy instead of one overload per
descendant.

## Requirements and restrictions

- `closed` is a contextual keyword and implicitly makes the class `abstract` — it can't be combined
  with `sealed`, `static`, or an explicit `abstract` modifier, and the class itself can't be
  instantiated directly.
- Derivation isn't transitive: a non-`closed` direct descendant of a closed class can still be
  further derived from in other assemblies. Exhaustiveness only covers *direct* descendants; mark
  an intermediate descendant `closed` too if exhaustiveness should extend further down.
- When the governing type is nullable (`PaymentMethod?`), a switch that omits a `null` arm isn't
  exhaustive even when every non-null descendant is handled.
- Subsumption still applies normally: an arm matching a base type covers every subtype beneath it,
  so a closed hierarchy's exhaustiveness doesn't force one arm per leaf type — an arm for `Car`
  alone still exhaustively covers a further, non-closed descendant like `Sedan`.

## Fallback

Below C# 15.0, there's no `closed` modifier and no compiler-verified exhaustiveness over a class
hierarchy — a switch over a base class always needs a discard arm (or risks CS8509) regardless of
how carefully the hierarchy is designed:

```csharp
public abstract record class PaymentMethod;
public sealed record class Cash : PaymentMethod;
public sealed record class Card(string Last4) : PaymentMethod;
public sealed record class BankTransfer(string Iban) : PaymentMethod;

public static string Describe(PaymentMethod method) => method switch
{
    Cash => "cash",
    Card(var last4) => $"card ending {last4}",
    BankTransfer(var iban) => $"bank transfer to {iban}",
    _ => throw new ArgumentOutOfRangeException(nameof(method), "Unknown payment method."),
};
```

The discard arm's `throw` is the idiomatic stand-in for compiler-verified exhaustiveness: it turns
a missed case into a loud runtime failure instead of a silent one, which is the best available
substitute until the hierarchy's target moves to C# 15. See
[../specialized/switch-expressions-vs-statements.md](../specialized/switch-expressions-vs-statements.md)
for the general discard-arm/CS8509 guidance this pattern builds on. Everything else about pattern
matching is unchanged from
[csharp11-list-and-slice-patterns.md](csharp11-list-and-slice-patterns.md) — C# 12, 13, and 14
added no pattern-matching syntax between C# 11 and this tier.
