# Switch Expressions vs. Switch Statements

Both forms exist side by side since C# 8.0 introduced the switch expression
([csharp8-switch-expressions-and-recursive-patterns.md](../references/csharp8-switch-expressions-and-recursive-patterns.md))
alongside the switch statement that's been around since C# 1.0 (pattern-capable since
[csharp7-is-and-switch-patterns.md](../references/csharp7-is-and-switch-patterns.md)). This file
covers when to reach for which, and the exhaustiveness/discard-arm/CS8509 mechanics that apply
mostly — but not only — to the expression form.

## Basic: choosing the form by what the code needs to do

```csharp
// Switch EXPRESSION: every arm produces a value assigned to the same target.
decimal cost = shape switch
{
    Circle c => Math.PI * c.Radius * c.Radius,
    Rectangle r => r.Width * r.Height,
    _ => throw new ArgumentException("Unknown shape.", nameof(shape)),
};
```

```csharp
// Switch STATEMENT: arms have side effects, not a shared value to produce.
switch (shape)
{
    case Circle c:
        _log.LogInformation("Circle with radius {Radius}", c.Radius);
        RenderCircle(c);
        break;
    case Rectangle r:
        _log.LogInformation("Rectangle {Width}x{Height}", r.Width, r.Height);
        RenderRectangle(r);
        break;
    default:
        throw new ArgumentException("Unknown shape.", nameof(shape));
}
```

Default to the expression form whenever every branch's job is "compute a value for the same
variable/return" — it's shorter, and the compiler's exhaustiveness check gives you a warning a
statement-form default case never would. Reach for the statement form when a branch needs multiple
statements, mixed side effects, or `break`/`continue` control flow that doesn't fit an expression.
A switch expression *can* invoke a multi-statement local function or method per arm to keep using
expression form even with more complex logic per branch — that's often preferable to switching
back to statement form just because one arm got bigger.

## Advanced: exhaustiveness, the discard arm, and CS8509

```csharp
public enum ConnectionState { Connecting, Connected, Disconnected }

// Missing 'Disconnected' -> CS8509 warning at compile time, not a runtime surprise.
string Describe(ConnectionState state) => state switch
{
    ConnectionState.Connecting => "connecting...",
    ConnectionState.Connected => "connected",
    // CS8509: the switch expression does not handle all possible values of its input type
    // (it is not exhaustive). For example, the pattern 'ConnectionState.Disconnected' is
    // not covered.
};
```

A switch expression that doesn't provably cover every possible input gets **CS8509**, a *warning*
(not an error) at the point where the switch expression is compiled. Two ways to resolve it:

```csharp
// Fix 1: handle the missing case explicitly — the right choice when there IS a correct
// behavior for it.
string Describe(ConnectionState state) => state switch
{
    ConnectionState.Connecting => "connecting...",
    ConnectionState.Connected => "connected",
    ConnectionState.Disconnected => "disconnected",
    _ => throw new ArgumentOutOfRangeException(nameof(state)), // still needed: enums aren't
                                                                 // restricted to their declared
                                                                 // members at the type level.
};

// Fix 2: an explicit discard arm — the right choice when "anything else is a bug, fail loudly"
// is genuinely the intended behavior, or the input's full value space can't be enumerated
// (e.g. int, string).
string Grade(int score) => score switch
{
    >= 90 => "A",
    >= 80 => "B",
    >= 70 => "C",
    < 70 => "F",
    _ => throw new ArgumentOutOfRangeException(nameof(score)), // unreachable given the ranges
                                                                 // above, but the compiler
                                                                 // can't prove that.
};
```

An `enum` in C# isn't actually restricted to its declared members — `(ConnectionState)99` is a
valid value of that type — so even a switch handling every *declared* enum member still benefits
from a discard arm that throws, both to silence CS8509 and to fail loudly on an out-of-range value
rather than silently falling through. A `switch` **statement** never produces CS8509 — a
non-exhaustive statement with no matching case simply does nothing and falls through, which is
exactly the silent-gap behavior the expression form's warning exists to catch. That's a real
argument for preferring the expression form even when a statement would otherwise do, whenever the
input's exhaustiveness actually matters. One exception:
[the C# 15 closed-hierarchy tier](../references/csharp15-closed-hierarchy-exhaustiveness.md) lets
the compiler *prove* exhaustiveness over a class hierarchy (not just enums) and skip the discard
arm without losing the CS8509 safety net, when the target supports it.

## Fallback

Below C# 8.0, there's no switch expression and no CS8509 — only the switch statement exists, and a
non-exhaustive statement silently does nothing on an unmatched input, so the discard-arm safety net
this file describes has no equivalent; the closest available practice is to make every switch
statement end in an explicit `default: throw ...;` by convention, since the compiler won't flag a
missing one. See
[csharp7-is-and-switch-patterns.md](../references/csharp7-is-and-switch-patterns.md) for the
switch-statement pattern syntax available at that tier.
