# `record struct` and Explicit `record class` (C# 10.0, GA November 2021, with .NET 6)

C# 10.0 adds `record struct`, making records available as value types, and lets `record class` be
written out in full (it was already legal but redundant at the C# 9 tier — see
[csharp9-record-fundamentals.md](csharp9-record-fundamentals.md)). A `record struct` gets the same
synthesized `Equals`, `GetHashCode`, `ToString`, `==`/`!=`, and (for positional declarations)
`Deconstruct` that a `record class` gets, but with two behavioral differences that follow directly
from being a value type: positional properties default to mutable (`{ get; set; }`, not
`{ get; init; }`) unless the declaration also adds `readonly`, and there's no `with`-expression clone
method to synthesize — a `record struct` is copied by value on assignment, so `with` just builds a
new value from a copy of the original instance's fields directly, with no clone method involved.

## Syntax

```csharp
// record struct: positional properties default to mutable (get; set;).
public record struct Point(double X, double Y);

// readonly record struct: positional properties are init-only, matching record class's default.
public readonly record struct ImmutablePoint(double X, double Y);

// explicit record class -- identical to plain `record`, spelled out for clarity alongside record struct.
public record class Employee(string FirstName, string LastName);
```

## Basic use case: value semantics without giving up records' synthesized members

```csharp
public readonly record struct Money(decimal Amount, string Currency);

Money price = new(19.99m, "USD");
Money copy = price;               // full value copy, not a reference -- no aliasing possible
Money discounted = price with { Amount = 14.99m };

Console.WriteLine(price == copy);        // True -- value equality, synthesized without reflection
Console.WriteLine(price == discounted);  // False
Console.WriteLine(discounted);           // Money { Amount = 14.99, Currency = USD }
```

Unlike an ordinary `struct`'s inherited `Equals`, which falls back to
[ValueType.Equals](https://learn.microsoft.com/dotnet/api/system.valuetype.equals) and uses
reflection, a record struct's equality is compiler-synthesized from its declared members — same
performance characteristic as a record class's equality, applied to a value type.

## Advanced use case: mutable record struct fields inside a collection

```csharp
public record struct Counter(string Label, int Value);

var counters = new List<Counter> { new("Hits", 0), new("Misses", 0) };

// mutable record struct: fields can be updated in place via indexer access on a List<T>,
// unlike a record class field which would require a with-expression and reassignment.
Counter current = counters[0];
current.Value++;
counters[0] = current; // still needs writing back -- a List<T> indexer returns a copy
```

```csharp
public readonly record struct ReadOnlyCounter(string Label, int Value);

var readOnlyCounters = new List<ReadOnlyCounter> { new("Hits", 0) };
// readOnlyCounters[0].Value++;  // compile error: Value has no accessible set/init accessor
readOnlyCounters[0] = readOnlyCounters[0] with { Value = readOnlyCounters[0].Value + 1 };
```

A mutable `record struct` gets ordinary struct-mutation ergonomics and pitfalls (an indexer or
property returning a struct by value returns a copy, so mutating the copy doesn't affect the
original storage) layered on top of records' synthesized equality — see
[specialized/record-equality-semantics-in-depth.md](../specialized/record-equality-semantics-in-depth.md)
for the boxing implications of comparing a mutable record struct through a non-generic path.

## Requirements and restrictions

- Positional properties on a plain `record struct` are read-write by default; add `readonly` to the
  struct declaration (`public readonly record struct Point(...)`) to get the same init-only
  semantics a `record class`'s positional properties have by default.
- The compiler doesn't synthesize a copy constructor for `record struct` types the way it does for
  `record class` types — a `with`-expression on a `record struct` still works (the struct's fields
  are copied by value, then the named members are changed), but if you hand-write your own copy
  constructor it is never called by a `with`-expression.
- `record struct` can't participate in inheritance the way `record class` does — no `record struct`
  can inherit from another record, and the `closed`-hierarchy exhaustiveness feature (see
  [csharp15-closed-record-hierarchies.md](csharp15-closed-record-hierarchies.md)) doesn't apply to it
  for the same reason: a struct can't be abstract, so it can't anchor a hierarchy.
- `record class` and plain `record` remain fully interchangeable at this tier and every later one;
  choosing `record class` over `record` is a style decision (explicitness), never a behavioral one.

## Fallback

On a target before C# 10.0, a value-type record has no equivalent — use a
[C# 9 `record class`](csharp9-record-fundamentals.md) instead, accepting reference-type semantics
(heap allocation, reference equality is not used because record equality is still value-based, but
the instance itself is a reference), or fall back further to a
[hand-written struct with manually overridden `Equals`/`GetHashCode`](pre-csharp9-manual-value-types.md)
if a genuine value type with value equality is required on a pre-C#10 target.
