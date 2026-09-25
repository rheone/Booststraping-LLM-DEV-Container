# Record Equality Semantics in Depth

This file goes deeper on the value-equality machinery introduced in
[csharp9-record-fundamentals.md](../references/csharp9-record-fundamentals.md) and extended to value
types in [csharp10-record-structs.md](../references/csharp10-record-structs.md): how `EqualityContract`
makes inheritance-aware equality work, the shallow-vs-deep distinction, a live-computed-property
pitfall, and a boxing trap specific to comparing `record struct` values through a non-generic path.

## Basic: why two records with identical properties but different runtime types are never equal

```csharp
public abstract record Person(string FirstName, string LastName);
public record Teacher(string FirstName, string LastName) : Person(FirstName, LastName);
public record Student(string FirstName, string LastName) : Person(FirstName, LastName);

Person teacher = new Teacher("Nancy", "Davolio");
Person student = new Student("Nancy", "Davolio");
Console.WriteLine(teacher == student); // False, even though both are typed as Person here
```

Every record class has a compiler-synthesized `EqualityContract` property that returns the record's
own runtime `Type`. It's `virtual` on a record that derives directly from `object`, and an `override`
on a derived record — either way, the synthesized `Equals` compares `EqualityContract` before
comparing any data member, so two instances of different runtime record types are never equal no
matter how their declared properties compare. This is why the *declared* variable type (`Person` in
both declarations above) has no bearing on the result — only the runtime type does.

## Basic: shallow copies and the reference-type-property trap

```csharp
public record Order(string Id, List<string> Items);

Order original = new("ORD-1", new List<string> { "Widget" });
Order copy = original with { Id = "ORD-2" };

copy.Items.Add("Gadget"); // mutates the SAME List<string> instance -- with is a shallow copy
Console.WriteLine(original.Items.Count); // 2, not 1 -- original is affected too
```

A `with`-expression (and a record's synthesized copy constructor generally) copies each property's
*value* — for a reference-type property, that value is the reference itself, not a new object. Two
records produced from the same `with` chain share every mutable reference-type property until one of
them is deliberately given a new instance of that property (`copy with { Items = new List<string>(original.Items) }`,
not `copy with { Id = "..." }` alone). Treat any reference-type record property as a shared object
unless the record's own type is itself immutable (an `ImmutableList<T>`, a record, a `readonly`
struct) or you explicitly deep-copy it.

## Advanced: computed properties evaluated at the wrong time

```csharp
// WRONG: Distance is computed once, at construction, and cached in a backing field.
public record PointCached(int X, int Y)
{
    public double Distance { get; } = Math.Sqrt(X * X + Y * Y);
}

PointCached p = new(3, 4) with { Y = 8 };
Console.WriteLine(p.Distance); // 5 -- stale; computed from the ORIGINAL Y, not the with-expression's

// RIGHT: Distance is computed on every access from the current property values.
public record PointLive(int X, int Y)
{
    public double Distance => Math.Sqrt(X * X + Y * Y);
}

PointLive q = new(3, 4) with { Y = 8 };
Console.WriteLine(q.Distance); // 8.544... -- correct, recomputed from the post-with values
```

A `with`-expression copies the *record's own stored state*, which for `{ get; } = expr` includes
whatever `expr` evaluated to at construction time — it does not re-run that initializer. Any record
property whose value is derived from other properties on the same record must be an expression-bodied
(`=>`) property computed on every access, never an initializer-assigned one, or `with`-expressions on
that record will silently carry stale derived data.

## Advanced: the boxing pitfall comparing `record struct` through a non-generic path

```csharp
public readonly record struct Money(decimal Amount, string Currency);

Money a = new(10m, "USD");
object boxedA = a;                       // boxed: heap-allocated copy of the struct
object boxedB = new Money(10m, "USD");   // separately boxed

Console.WriteLine(a.Equals(boxedB));      // True -- Equals(object) still unboxes and compares by value
Console.WriteLine(boxedA.Equals(boxedB)); // also True -- record struct's Equals(object) override handles this correctly

// the actual trap: comparing through a non-generic collection or API that only ever sees `object`
ArrayList untyped = new() { a };
Console.WriteLine(untyped.Contains(new Money(10m, "USD"))); // True, but every comparison boxes both sides
```

A `record struct`'s synthesized `Equals(object)` override does correctly unbox and perform value
comparison — records don't have the classic "boxed struct never equals anything" bug that a
hand-rolled struct without an `Equals(object)` override has. The real cost is allocation, not
correctness: every comparison that goes through the non-generic `Equals(object)` path (a non-generic
collection like `ArrayList`, or any API typed to accept `object`) boxes one or both operands. Prefer
the record struct's generic, boxing-free `Equals(Money other)` — automatically preferred by
`List<Money>`, `Dictionary<Money, _>`, `HashSet<Money>`, and `==` — by keeping record structs out of
non-generic collection types.

## Fallback

`EqualityContract` and the runtime-type-aware equality it enables are specific to `record class` and
have no equivalent on a [pre-C#9 hand-written class](../references/pre-csharp9-manual-value-types.md);
replicate it manually with a `GetType() == other.GetType()` check as the first line of a hand-written
`Equals`. `record struct` equality (from [C# 10](../references/csharp10-record-structs.md) onward) has
no inheritance dimension to replicate, since record structs can't participate in inheritance at all.
