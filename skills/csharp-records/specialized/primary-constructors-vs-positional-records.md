# Primary Constructors on Classes/Structs vs. Positional Records

C# 12.0 (GA November 2023, with .NET 8) extended primary-constructor syntax — a parameter list
directly on the type declaration — to ordinary `class` and `struct` types. This file exists because
that syntax looks identical to a positional record's declaration
(see [csharp9-record-fundamentals.md](../references/csharp9-record-fundamentals.md)) but means
something fundamentally different, and mixing the two up is an easy, silent mistake: it doesn't
change anything about records themselves (records have had this parameter-list syntax since C# 9,
three releases before ordinary classes got their own version of it), but it means a positional-record
declaration and an ordinary class's primary constructor are visually indistinguishable at a glance
unless you know which keyword introduced the type.

## Basic: the same parameter list, two entirely different outcomes

```csharp
// positional record: the compiler generates a PUBLIC init-only property per parameter,
// plus Equals, GetHashCode, ToString, Deconstruct, and a with-capable copy constructor.
public record PersonRecord(string FirstName, string LastName);

// ordinary class with a primary constructor: FirstName and LastName are constructor
// PARAMETERS ONLY. No properties are generated. No Equals/GetHashCode/ToString overrides.
// No with-expression support. The parameters are usable inside the class body, but nothing
// is exposed to callers unless you explicitly add a property that reads the parameter.
public class PersonClass(string FirstName, string LastName);
```

```csharp
var record = new PersonRecord("Nancy", "Davolio");
Console.WriteLine(record.FirstName); // Nancy -- a real, public, generated property

var plain = new PersonClass("Nancy", "Davolio");
// Console.WriteLine(plain.FirstName); // compile error: 'PersonClass' has no member 'FirstName'
```

The single most common mistake this causes: assuming an ordinary class's primary constructor
parameters became properties, the way a record's positional parameters do, and being surprised when
nothing is accessible outside the constructor body.

## Advanced: when a class's primary constructor is used *instead of* turning the type into a record

```csharp
public class OrderProcessor(IOrderRepository repository, ILogger<OrderProcessor> logger)
{
    public async Task ProcessAsync(string orderId)
    {
        logger.LogInformation("Processing {OrderId}", orderId);
        var order = await repository.GetAsync(orderId);
        // repository and logger are captured for use throughout the class body,
        // exactly like fields assigned in a traditional constructor would be --
        // but never exposed as public properties, and never compared for equality.
    }
}
```

A primary constructor on an ordinary class is the right tool for a service or behavior-holding type
that needs constructor-injected dependencies with less ceremony than a traditional
`private readonly` field plus constructor assignment — it is not, and was never meant to be, a
lighter-weight record. `OrderProcessor` above has no business having value equality or a `with`copy:
it's a service, not a data carrier, and primary-constructor syntax on the class only saves boilerplate
for capturing its dependencies — it adds none of records' data-type behavior.

## Decision list

- Need generated public properties, value equality, `ToString`, `Deconstruct`, and `with`-copies from
  a parameter list → a positional [record](../references/csharp9-record-fundamentals.md) (or
  [record struct](../references/csharp10-record-structs.md)), available since C# 9/10.
- Need a concise way to capture constructor-injected dependencies or setup parameters for a type
  whose job is behavior, not data — with no properties, no equality, no `with` — → an ordinary
  `class`'s or `struct`'s C# 12 primary constructor.
- Need *some* parameters to become public properties and others to stay private captured state, on a
  type that is otherwise data-like → still reach for a positional record and add explicit member
  declarations for anything that needs different accessibility, as shown in
  [csharp9-record-fundamentals.md](../references/csharp9-record-fundamentals.md); a plain class's
  primary constructor generates no properties at all, so it can't partially replicate this.

## Fallback

Positional records need [C# 9](../references/csharp9-record-fundamentals.md) at minimum, same as
every other record syntax in this skill. Primary constructors on ordinary classes and structs need
C# 12 specifically; on an older target, write the traditional constructor body instead —
`private readonly` fields (or properties) assigned from constructor parameters — which is what a
class's primary constructor lowers to under the hood regardless of language version.
