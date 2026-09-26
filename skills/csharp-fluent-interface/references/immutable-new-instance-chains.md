# Immutable, New-Instance Chains

Instead of mutating a shared instance, every chained call returns a fresh instance carrying the
accumulated change, leaving the receiver it was called on untouched. The chain still reads as one
fluent expression; what changes is that every intermediate value in it is a distinct, independent
object.

## Basic: each call returns a new value

```csharp
public sealed record ShippingOptions(bool SignatureRequired, bool Insured, decimal DeclaredValue)
{
    public ShippingOptions RequireSignature() => this with { SignatureRequired = true };
    public ShippingOptions Insure(decimal declaredValue) => this with { Insured = true, DeclaredValue = declaredValue };
}
```

```csharp
ShippingOptions basic = new(SignatureRequired: false, Insured: false, DeclaredValue: 0m);
ShippingOptions upgraded = basic.RequireSignature().Insure(500m);

// `basic` is unchanged — SignatureRequired: false, Insured: false, DeclaredValue: 0
```

`this with { ... }` (records, C# 9.0) copies every member the call doesn't name and changes only the
ones it does, which is what makes each fluent method a one-line "return a modified copy" instead of
a hand-written constructor call repeating every field. `basic` still refers to the original,
untouched value after the chain runs — nothing about calling `RequireSignature()` on it changed what
`basic` points to.

## Advanced: LINQ's deferred, chained operators are the same shape

```csharp
IEnumerable<Order> query = orders
    .Where(o => o.IsActive)
    .OrderBy(o => o.CreatedAt)
    .Select(o => o.Total);
```

Each LINQ operator returns a new `IEnumerable<T>` wrapping the previous one rather than mutating
`orders` — the same immutable, new-instance chain shape, just over a query pipeline instead of a
configuration value. None of `Where`, `OrderBy`, or `Select` runs its logic when called; each
returns an object that runs it later, when something enumerates the final result. That deferral is
orthogonal to the chain shape itself (an immutable chain doesn't have to be lazy — the
`ShippingOptions` example above computes eagerly), but it's worth naming explicitly for a fluent
query API: a caller who expects the whole pipeline to execute at the point the chain is written,
rather than at the point it's enumerated, will misread when side effects inside a lambda actually
run.

## The discarded-return trap, revisited

```csharp
var options = new ShippingOptions(false, false, 0m);
options.RequireSignature();          // does nothing — the new instance is discarded
Ship(options);                       // still SignatureRequired: false
```

This is the failure mode [core-concepts-and-terminal-operations.md](core-concepts-and-terminal-operations.md)
names generally, sharper here: an immutable chain's calls are pure — calling one and ignoring its
return value has *no effect at all*, unlike the mutable shape, where the same mistake at least still
mutated the shared instance. Every immutable chain call must be captured, either by reassigning the
same variable (`options = options.RequireSignature();`) or by continuing the chain into the next
call directly.

## Requirements and restrictions

- Every method in the chain needs a cheap, correct way to produce "a copy with one thing changed" —
  records' `with`-expression (C# 9.0) gives this for free; a hand-written immutable class needs a
  copy constructor or a manually written `With*` method that passes every other field through
  unchanged.
- Prefer this shape when the receiver might be held onto and reused as a stable base for several
  different chains (a shared "default options" value that many call sites branch from without
  affecting each other) — the mutable shape can't offer that safely, since branching from a shared
  mutable instance means every branch's mutations collide on the same object.
