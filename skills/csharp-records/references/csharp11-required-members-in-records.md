# `required` Members in Records (C# 11.0, GA November 2022, with .NET 7)

C# 11.0's `required` modifier is a general-purpose language feature — it applies to fields and
properties on any `class` or `struct`, not just records — but it has a specific, non-obvious rule
where it meets records: **you can't apply `required` to a positional record's parameter list.** A
positional record's compiler-generated properties can't individually be marked required, because
`required` is a member-declaration modifier and positional parameters aren't member declarations.
Records get `required` support in the same way any other type does — by declaring the property
explicitly instead of positionally — with one piece of compiler help specific to records: a
positional record's synthesized primary constructor is automatically annotated with
`[SetsRequiredMembers]` when any explicitly declared property on the record is `required`.

## Syntax

```csharp
// invalid -- required can't be applied inside a positional parameter list.
// public record Person(required string FirstName, required string LastName);

// valid -- explicit property declaration, required + init, same shape record types have had since C# 9.
public record Person
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public int? Age { get; init; }
}
```

## Basic use case: enforcing mandatory fields on a non-positional record

```csharp
var person = new Person
{
    FirstName = "Nancy",
    LastName = "Davolio"
};
// var invalid = new Person(); // compile error CS9035: required member 'Person.FirstName' must be set

Person renamed = person with { LastName = "Fuller" }; // with-expressions are unaffected by required
```

`required` plus `init` on an explicit-member record gives the same "must supply this value, can
never change it after" guarantee that positional syntax gives implicitly for every constructor
parameter — the difference is purely which members are mandatory. A positional record makes every
listed parameter mandatory by construction; `required` on an explicit-member record lets you make
only some of the type's properties mandatory while leaving others (`Age` above) optional.

## Advanced use case: mixing positional parameters with a required explicit property

```csharp
public record Order(string Id, decimal Total)
{
    public required string CustomerEmail { get; init; }
}
```

```csharp
var order = new Order("ORD-001", 49.99m)
{
    CustomerEmail = "nancy@example.com"
};
// var invalid = new Order("ORD-001", 49.99m); // compile error: CustomerEmail not set
```

The positional constructor parameters (`Id`, `Total`) stay mandatory the way positional parameters
always are; the explicit `CustomerEmail` property adds a *third* mandatory member enforced through
object-initializer syntax rather than the primary constructor's parameter list — the two mechanisms
compose on the same record without conflict.

## Requirements and restrictions

- `required` can't be written inside a positional record's parameter list; it only applies to a
  property or field declared with ordinary property syntax in the record's body.
- A `required` property must have an accessible `init` or `set` accessor — this is true for any type,
  and applies equally to a record's explicitly declared members.
- If a record declares a custom constructor that fully initializes every `required` member, mark that
  constructor `[SetsRequiredMembers]` yourself (from `System.Diagnostics.CodeAnalysis`) — the compiler
  only adds this automatically to a positional record's synthesized primary constructor, not to
  hand-written constructors.
- A record's compiler-generated copy constructor (the one backing `with`-expressions on a
  `record class`) is itself annotated `[SetsRequiredMembers]` automatically whenever the record has
  any required members, so `with`-expressions are unaffected by adding `required` to a property.
- Applies to `record class`, `record struct`, and `readonly record struct` alike — the positional
  restriction and the explicit-property workaround are identical across all three.

## Fallback

On a target before C# 11.0, `required` doesn't exist for any type, records included. Enforce a
mandatory member the way [C# 9 records](csharp9-record-fundamentals.md) already do it by default —
make the member a positional parameter, since positional parameters have always been mandatory — or,
for a non-positional record, fall back to a constructor that takes the mandatory values as parameters
and assigns them, accepting that object-initializer syntax alone can no longer be relied on to
enforce the requirement at compile time.
