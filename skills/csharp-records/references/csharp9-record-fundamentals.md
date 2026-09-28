# Record Fundamentals (C# 9.0, GA November 2020, with .NET 5)

C# 9.0 introduces the `record` keyword. `record` and `record class` are the same declaration — the
`class` keyword is optional and purely a readability choice, since at this tier `record struct`
doesn't exist yet (that arrives in C# 10). A record declared this way is a reference type: the
compiler synthesizes value-based `Equals`, `GetHashCode`, `ToString`, `==`/`!=`, and (for positional
records) a `Deconstruct` method, replacing every hand-written member from the
[pre-C#9 manual pattern](pre-csharp9-manual-value-types.md). Records also gain `with`-expressions for
non-destructive copies, and init-only properties (usable on any type, not just records) for
construct-then-freeze immutability.

## Syntax

```csharp
// positional record: primary-constructor-style parameter list becomes init-only properties,
// plus a synthesized Deconstruct method -- all from one line.
public record Person(string FirstName, string LastName);

// equivalent, written with explicit members instead of positional syntax.
public record PersonExplicit
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
}

// record and record class are identical; the keyword is optional.
public record class Employee(string FirstName, string LastName, string Department);
```

## Basic use case: positional record, value equality, and `with`

```csharp
Person original = new("Nancy", "Davolio");
Person duplicate = new("Nancy", "Davolio");
Person renamed = original with { LastName = "Fuller" };

Console.WriteLine(original == duplicate);  // True -- value equality, not reference equality
Console.WriteLine(original == renamed);    // False -- LastName differs
Console.WriteLine(renamed);                // Person { FirstName = Nancy, LastName = Fuller }

var (first, last) = original;              // Deconstruct is synthesized automatically
Console.WriteLine($"{first} {last}");      // Nancy Davolio
```

`with { LastName = "Fuller" }` performs a shallow copy of every member, then applies the named
changes — the entire replacement for the [pre-C#9 manual pattern's](pre-csharp9-manual-value-types.md)
hand-written "copy with changes" method, and it can never drift out of sync with the type's member
list the way a hand-written method can.

## Advanced use case: record inheritance and generic records

```csharp
public abstract record Shape(string Name);
public record Circle(string Name, double Radius) : Shape(Name);
public record Rectangle(string Name, double Width, double Height) : Shape(Name);
```

```csharp
Shape circle = new Circle("C1", Radius: 5.0);
Shape sameCircle = new Circle("C1", Radius: 5.0);
Shape rectangle = new Rectangle("C1", Width: 5.0, Height: 5.0);

Console.WriteLine(circle == sameCircle);  // True -- same runtime type, same values
Console.WriteLine(circle == rectangle);   // False -- different runtime type, even with overlapping data
Console.WriteLine(circle);                // Circle { Name = C1, Radius = 5 }
```

Value equality on a derived record compares the *runtime* type, not the compile-time (declared)
type — two records with identical property values but different runtime types are never equal. A
record can also be generic, which composes naturally with positional syntax:

```csharp
public record Result<T>(T Value, bool Success, string? Error = null);

Result<int> ok = new(42, Success: true);
Result<int> failed = new(default, Success: false, Error: "not found");

Console.WriteLine(ok);      // Result { Value = 42, Success = True, Error =  }
Console.WriteLine(failed);  // Result { Value = 0, Success = False, Error = not found }
```

`Result<T>` reads and behaves exactly like `Person` above — the type parameter flows through the
positional parameter list, the synthesized properties, `Equals`, `ToString`, and `Deconstruct`
exactly as it would for any other generic type; nothing about records changes how a type parameter
itself is declared or constrained.

## Requirements and restrictions

- A record can only inherit from another record — a record can't inherit from a plain `class`, and a
  plain `class` can't inherit from a record.
- A derived positional record must repeat every base positional parameter in its own primary
  constructor and forward them to the base (`Rectangle(string Name, ...) : Shape(Name)` above).
- The compiler-synthesized `Equals`/`GetHashCode`/`==`/`!=`/`ToString`/`Deconstruct` members can't be
  declared explicitly with a matching signature without either replacing the synthesized member
  (allowed, for most of them) or causing a compile error (for the base-typed `Equals` overload and
  the equality operators, which can't be hand-written at all).
- `with`-expressions require every property being set to have an accessible `init` or `set`
  accessor; a property with only a `get` accessor can't be changed through `with`.
- Immutability from init-only properties is shallow: a `with`-copied record holding a mutable
  reference type (an array, a `List<T>`) shares that same instance with the original — mutating the
  referenced object's contents affects both.

## Fallback

On a target before C# 9.0, use the
[pre-C#9 manual pattern](pre-csharp9-manual-value-types.md): a plain class with hand-written
`Equals`, `GetHashCode`, `ToString`, `==`/`!=` overrides, `{ get; private set; }` or
constructor-only-assigned properties in place of `init`, and a hand-written static or instance
"copy with changes" method in place of `with`.
