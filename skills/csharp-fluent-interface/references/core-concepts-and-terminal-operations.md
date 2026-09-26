# Core Concepts and Terminal Operations

A fluent interface isn't a language feature — it's a naming for a return-type shape that C# has
supported since 1.0: any method whose return type has further members you can call takes part in a
chain, and any call whose return type doesn't ends the chain. Designing a fluent API is entirely
about choosing, for each method, which of those two things it does and why.

## What makes a call chainable

```csharp
public sealed class QueryFilter
{
    public QueryFilter WhereActive() { /* ... */ return this; }
    public QueryFilter WhereCreatedAfter(DateOnly date) { /* ... */ return this; }
    public IReadOnlyList<Order> Execute() { /* ... */ return _results; }
}
```

```csharp
IReadOnlyList<Order> orders = new QueryFilter()
    .WhereActive()
    .WhereCreatedAfter(new DateOnly(2026, 1, 1))
    .Execute();
```

`WhereActive` and `WhereCreatedAfter` return `QueryFilter` itself, so the next call attaches
directly. `Execute` returns `IReadOnlyList<Order>` — a type with no further chain member relevant
here — so it's the chain's **terminal operation**. Nothing about this needs generics, inheritance,
or any particular C# version; the entire mechanism is "return a type the next call can use."

## The three chain shapes

- **Mutable, self-returning**: every call mutates shared state and returns the same instance
  (`return this;`). See
  [mutable-self-returning-chains.md](mutable-self-returning-chains.md).
- **Immutable, new-instance**: every call leaves the receiver untouched and returns a fresh
  instance carrying the accumulated change. See
  [immutable-new-instance-chains.md](immutable-new-instance-chains.md).
- **Staged**: each call returns a *different* type representing "what's legal to call next,"
  turning call order into a compile-time constraint instead of a runtime concern. See
  [staged-fluent-apis-and-call-order.md](staged-fluent-apis-and-call-order.md).

A single fluent API can mix these — a query builder might chain mutably through its filter calls,
then hand back an immutable, already-materialized result type from its terminal operation.

## Choosing the terminal operation's type

The terminal operation is the one call in a chain deliberately designed to return something with no
further chain member — the point where the caller stops composing and starts consuming. Pick its
return type for what the caller does *next*: a materialized collection when the caller iterates,
`void` when the chain's entire purpose was a side effect (configuring an object in place), or the
chain's own product type when the fluent surface was building something up to hand back whole. A
terminal operation that returns the same chainable type as every other call in the chain isn't
wrong, but it invites a caller to keep going past the point the API intended, with no compiler
signal that the chain is "done."

## The silently-discarded-call trap

```csharp
var filter = new QueryFilter();
filter.WhereActive();               // return value discarded — did this mutate `filter`, or not?
filter.WhereCreatedAfter(cutoff);
IReadOnlyList<Order> orders = filter.Execute();
```

This compiles and, for the mutable self-returning shape, works correctly — but reading it, nothing
tells you whether `WhereActive()` mutated `filter` in place or returned an unused new instance that
got thrown away. The failure mode is real for the *immutable* shape: calling `WhereActive()` and
discarding its return value does nothing at all, because the original `filter` was never mutated
and the new instance carrying the change was never captured. Fluent APIs read safely only when every
call in the chain is actually chained — see
[naming-and-chain-readability.md](naming-and-chain-readability.md) for the readability side of this,
and pick a chain shape ([mutable-self-returning-chains.md](mutable-self-returning-chains.md) vs.
[immutable-new-instance-chains.md](immutable-new-instance-chains.md)) with this trap in mind: a
mutable shape tolerates an accidentally broken chain silently continuing to work; an immutable one
doesn't.
