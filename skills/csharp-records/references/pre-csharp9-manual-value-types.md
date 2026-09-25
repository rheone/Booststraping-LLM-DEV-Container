# Manual Value-Equality Types (Before C# 9.0)

Before the `record` keyword existed, an immutable data-carrying type with value equality had to be
hand-built: a `class` (or `struct`) with hand-written overrides of `Equals`, `GetHashCode`, and
`ToString`, plus a hand-written "copy with changes" method to stand in for what became the
`with`-expression. None of this is generated — every line below is code a developer wrote and
maintained, and every one of the mistakes records later made structurally impossible (forgetting a
field in `Equals` but not `GetHashCode`, an `Equals` that doesn't check the runtime type, a `ToString`
that goes stale) was a real, recurring bug class in codebases that predate C# 9.

## Syntax

```csharp
public sealed class PersonManual : IEquatable<PersonManual>
{
    public string FirstName { get; }
    public string LastName { get; }

    public PersonManual(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public override bool Equals(object obj) => Equals(obj as PersonManual);

    public bool Equals(PersonManual other) =>
        other is not null &&
        FirstName == other.FirstName &&
        LastName == other.LastName;

    public override int GetHashCode() => HashCode.Combine(FirstName, LastName);

    public override string ToString() => $"PersonManual {{ FirstName = {FirstName}, LastName = {LastName} }}";

    public static bool operator ==(PersonManual left, PersonManual right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PersonManual left, PersonManual right) => !(left == right);
}
```

## Basic use case

```csharp
var original = new PersonManual("Nancy", "Davolio");

// no with-expression exists yet -- "copy with a change" is a hand-written method,
// one parameter per constructor argument, that the author must remember to update
// every time a field is added to the class.
public static PersonManual WithLastName(PersonManual source, string lastName) =>
    new PersonManual(source.FirstName, lastName);

var renamed = WithLastName(original, "Fuller");

Console.WriteLine(original == renamed); // False -- value equality, hand-implemented
Console.WriteLine(original);            // PersonManual { FirstName = Nancy, LastName = Davolio }
```

## Advanced use case: the bug class this pattern invites

```csharp
public sealed class OrderManual
{
    public string Id { get; }
    public decimal Total { get; }
    public string Currency { get; } // added later, after Equals/GetHashCode were written

    public OrderManual(string id, decimal total, string currency)
    {
        Id = id;
        Total = total;
        Currency = currency;
    }

    public override bool Equals(object obj) =>
        obj is OrderManual other && Id == other.Id && Total == other.Total;
        // Currency is silently excluded -- added to the class but never wired into
        // Equals. Two orders with different currencies now compare equal. The compiler
        // gives no warning; only a test (or a production bug) catches it.

    public override int GetHashCode() => HashCode.Combine(Id, Total);
}
```

This is the exact defect class records eliminate structurally: because a record's `Equals`,
`GetHashCode`, `ToString`, and `Deconstruct` are all synthesized from the same declared member list,
adding `Currency` to the type automatically adds it to every one of those members at once — there is
no "wire it in by hand and forget one" step left to skip.

## Requirements and restrictions

- Every one of `Equals(object)`, a typed `Equals`, `GetHashCode`, `ToString`, and the `==`/`!=`
  operators must be written and kept in sync by hand as fields are added, renamed, or removed.
- A "copy with changes" operation has no dedicated syntax; it's whatever ad hoc static method,
  instance method, or constructor overload a codebase happens to standardize on, and it typically
  needs a new parameter (or a new overload) every time a field is added.
- Nothing about this pattern is checked by the compiler beyond ordinary type checking — a forgotten
  field in `Equals` compiles cleanly and fails silently at runtime.

## Fallback

This is the first tier; there is no older fallback. Every C# version from 1.0 onward supports this
manual pattern, so it's also the fallback target named by every later tier in this skill when
targeting a pre-C#9 compiler.
