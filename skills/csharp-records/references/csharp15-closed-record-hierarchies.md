# `closed` Record Hierarchies (C# 15, RC as of September 2026)

**RC caveat:** C# 15 is currently at .NET 11 RC1 (released September 8, 2026); GA is expected around
November 10, 2026 alongside .NET 11's general availability. RC1 carries a go-live license, but
details of this feature could still shift before GA — verify against the current
[What's new in C# 15](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15) page before
relying on it in a production target.

C# 15 adds the `closed` modifier, which restricts a `record class` hierarchy's direct derivation to
the declaring assembly. It's a general class-hierarchy feature — it isn't record-specific in the
compiler's implementation — but it earns its own tier here because of where it lands: a `closed`
record hierarchy is precisely the shape a record inheritance tree (see
[csharp9-record-fundamentals.md](csharp9-record-fundamentals.md)) already tends to take, and the
payoff is a compiler-verified exhaustive `switch` over that hierarchy with no `default` arm required.
`closed` doesn't apply to `record struct` — a struct can't be `abstract`, and a closed hierarchy's
base type is implicitly abstract, so a struct can't anchor one.

## Syntax

```csharp
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;
```

## Basic use case: exhaustive switch with no default arm

```csharp
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;

string Describe(GateState state) => state switch
{
    Closed => "closed",
    Open(var percent) => $"{percent}% open",
    // no warning, and no default arm needed: every direct descendant of GateState is handled.
};
```

Before this tier, an exhaustive switch over a record hierarchy needed either a `default` arm (which
silently swallows a genuinely missing case instead of failing to compile) or an `abstract` method on
the base record that every derived record implements, forgoing `switch`-based dispatch entirely.
`closed` gives the compiler enough information to verify exhaustiveness the way it already does for
a hierarchy expressed as a `switch` over an `enum`.

## Advanced use case: an intermediate `closed` type extending exhaustiveness deeper

```csharp
public closed record class Shape;
public closed record class Polygon : Shape;
public record class Triangle(double Base, double Height) : Polygon;
public record class Square(double Side) : Polygon;
public record class Circle(double Radius) : Shape;

double Area(Shape shape) => shape switch
{
    Triangle t => 0.5 * t.Base * t.Height,
    Square s => s.Side * s.Side,
    Circle c => Math.PI * c.Radius * c.Radius,
    // exhaustive: Polygon's descendants (Triangle, Square) and Shape's other
    // direct descendant (Circle) are all accounted for.
};
```

Derivation isn't transitive through a single `closed` marker — `Polygon` itself needed the `closed`
modifier (not just `Shape`) for the compiler to verify that `Triangle` and `Square` are `Polygon`'s
complete set of direct descendants, and by extension that switching over all of `Shape`'s reachable
concrete descendants is exhaustive.

## Requirements and restrictions

- `closed` only applies to `record class` (and to plain `class`, outside this skill's scope) — never
  to `record struct`, since a closed hierarchy's root must be implicitly `abstract`.
- A `closed` record class is implicitly `abstract` and can't also be `sealed`, `static`, or
  explicitly `abstract`.
- Restricts *direct* derivation to the declaring assembly only; a non-`closed` descendant of a
  `closed` record can still be derived from in other assemblies, so exhaustiveness only extends as
  deep into the hierarchy as `closed` is applied at each level (see the advanced example above).
- This is a contextual keyword, not a reserved word — it doesn't break existing code that happens to
  use `closed` as an identifier outside a class-declaration-modifier position.

## Fallback

On a target before this feature ships (any pre-C#15/.NET 11 target, including the
[C# 11 required-members tier](csharp11-required-members-in-records.md) and every earlier tier in this
skill), a `switch` over a record hierarchy needs either an explicit `default` arm (accepting that a
missing case fails silently instead of at compile time) or an `abstract` method declared on the base
record and overridden by every derived record, in place of compiler-verified `switch` exhaustiveness.
