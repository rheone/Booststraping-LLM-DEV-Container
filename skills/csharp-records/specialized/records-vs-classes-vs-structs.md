# Records vs. Classes vs. Structs vs. Staged Assembly

Records ([csharp9-record-fundamentals.md](../references/csharp9-record-fundamentals.md),
[csharp10-record-structs.md](../references/csharp10-record-structs.md)) remove a large share of the
reasons a type used to need hand-written equality, a hand-written `ToString`, or a dedicated
staged-assembly helper type — but not all of them. This file is the decision list: given a type to
declare, which shape (plain `class`, plain `struct`, `record class`, `record struct`, or a
staged-assembly helper written by hand) costs the least code for the guarantees actually needed.

## Basic: the case records were built for — skip everything else

```csharp
public record ShipmentPlan(string Carrier, IReadOnlyList<string> Stops, bool SignatureRequired);
```

```csharp
ShipmentPlan plan = new("Standard", ["Warehouse A"], false);
ShipmentPlan expedited = plan with { Carrier = "Overnight", SignatureRequired = true };

Console.WriteLine(plan == expedited); // False -- value equality, no hand-written Equals
Console.WriteLine(expedited);         // ShipmentPlan { Carrier = Overnight, Stops = [...], SignatureRequired = True }
```

Flat, immutable, every field independent, no cross-field rule to enforce: a positional record is
strictly less code than a plain class with `{ get; init; }` properties plus hand-written equality,
and strictly less code than any staged-assembly helper whose only job would be producing variants of
an already-built instance — `with` does that in one expression.

## Basic: when a plain `class` (or `struct`) is still the right choice — behavior, not data

```csharp
public sealed class OrderProcessor
{
    private readonly IOrderRepository _repository;

    public OrderProcessor(IOrderRepository repository) => _repository = repository;

    public Task ProcessAsync(string orderId) => _repository.MarkProcessedAsync(orderId);
}
```

A type whose job is to *do* something — call dependencies, hold no comparable state, never be
copied-with-changes — gains nothing from `record`: value equality on a service type is meaningless
(two `OrderProcessor`s are never "the same value"), and a record's synthesized `ToString`/`Equals`
are pure overhead on a type nobody will ever compare or print for its data. Reach for `record` only
when the type's job is to represent data; reach for a plain `class`/`struct` when its job is to
perform behavior.

## Advanced: cross-field validation — the case records don't solve

```csharp
public sealed class DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("End must not precede Start.");
        }
        Start = start;
        End = end;
    }
}
```

A positional record's primary constructor has no hook for rejecting an invalid *combination* of
values — `required` only checks that a member was supplied, not that supplied members satisfy a
relationship. You can add a constructor body to a positional record and validate there, but the
moment a `with`-expression is involved this protection has a real gap: `with` doesn't route through
the constructor body at all, so `plan with { End = someEarlierDate }` can silently produce an invalid
`DateRange` that the constructor would have rejected had it been called directly. A record used this
way needs its own re-validation on every mutation path (constructor *and* the properties `with`
touches), which is real extra code — at that point a plain class with a validating constructor and no
`with`-equivalent at all is often simpler, not more code.

## Advanced: a very large optional-parameter surface — records don't reduce this either

```csharp
public sealed class ReportOptions
{
    public required string Title { get; init; }
    public bool IncludeCharts { get; init; }
    public bool IncludeRawData { get; init; }
    public string? Footer { get; init; }
    public DateOnly? AsOfDate { get; init; }
    public string DateFormat { get; init; } = "yyyy-MM-dd";
    // ...a dozen more independent, order-irrelevant optional settings
}
```

A record's positional syntax loses its main advantage — a short, readable declaration — once a type
has more than a handful of members, most of them optional: a fifteen-parameter positional record's
constructor call site is exactly as unreadable as a fifteen-parameter plain constructor call, since
C# has no named-parameters requirement at the call site. Declaring this as a record with explicit
`{ get; init; }` members (shown above with `record` implied, though the same declaration works
verbatim on a plain `class`) gets object-initializer syntax and keyword-named arguments regardless of
whether the type is a record — records add value equality and `with` on top, which is worth having
here only if instances of `ReportOptions` are themselves meaningfully compared or cloned-with-changes
elsewhere in the code; if they're each built once and consumed immediately, the extra synthesized
members are inert weight, not free.

## Decision list

- Flat data, immutable, every property independent, will be compared or `with`-copied → `record`
  (reference semantics) or `record struct` (value semantics, small, no inheritance needed).
- Behavior/service type: dependencies, no meaningful equality, never copied-with-changes → plain
  `class`.
- Cross-field validation that must hold after every mutation, not just at construction → a plain
  `class` with a validating constructor, or a record whose properties are re-validated on every
  `with`-reachable path — evaluate whether that extra validation code still nets out simpler than the
  plain class before choosing the record.
- A large optional-parameter surface, values set once and not meaningfully compared afterward → a
  plain `class` (or record, if comparison/`with` genuinely matter) with explicit `{ get; init; }`
  members and object-initializer call sites — positional syntax adds nothing once the member count is
  large.
- Multi-step assembly process where the *steps themselves*, not just the final field values, need to
  be modeled (an order that matters, a validation that must run mid-assembly, not just at the end) →
  neither a record nor a plain class's constructor covers this; it needs a dedicated accumulate-then-
  produce type outside this skill's scope.

## Fallback

Every alternative on this page except the record-based ones is available on any C# version this
skill's baseline target supports. The record-based rows need at minimum
[C# 9](../references/csharp9-record-fundamentals.md) (`record class`) or
[C# 10](../references/csharp10-record-structs.md) (`record struct`); on an older target, every case
that named a record above collapses to the plain-`class`-with-hand-written-equality shape in
[pre-csharp9-manual-value-types.md](../references/pre-csharp9-manual-value-types.md).
