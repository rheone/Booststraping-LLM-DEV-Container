# `required` Members Satisfy Definite Assignment (C# 11 / .NET 7, November 2022)

C# 11 added the `required` modifier for fields and properties generally — it's a member-initialization
feature in its own right, not part of nullable reference types. It earns a tier here because it
directly changes the most common piece of nullable-warning friction: a non-nullable property with
no constructor assigning it. Before `required`, the analyzer only had two ways to see a non-nullable
member as definitely assigned — a constructor that sets it, or `[MemberNotNull]` on a helper the
constructor calls. `required` adds a third: a compiler-enforced guarantee that every caller's
object-initializer syntax must set the member, which the analyzer accepts as satisfying definite
assignment without a constructor parameter for it at all.

## Syntax

```csharp
#nullable enable

public class Order
{
    public required string CustomerName { get; init; }
    public required string OrderId { get; init; }
    public string? Notes { get; init; } // still optional -- nullable, no required needed
}
```

## Basic use case: no constructor-assignment warning, no constructor needed

```csharp
#nullable enable

public class Order
{
    public required string CustomerName { get; init; } // no warning: required members don't need
    public required string OrderId { get; init; }       // constructor assignment to satisfy the analyzer
}

Order order = new()
{
    CustomerName = "Nancy Davolio",
    OrderId = "ORD-1001",
    // omitting either required property is a compiler ERROR (CS9035), not a nullable warning
};
```

Before this tier, the same two properties without a constructor produced
`CS8618: Non-nullable property 'CustomerName' must contain a non-null value when exiting
constructor` — the standard fix was either a constructor with one parameter per property, or
`= null!;` to silence the warning by lying to the analyzer. `required` replaces both: the compiler
enforces the guarantee itself, at every call site, instead of relying on a constructor the analyzer
can trace.

## Advanced use case: `SetsRequiredMembers` for a constructor that still exists

```csharp
#nullable enable

public class Order
{
    public required string CustomerName { get; init; }
    public required string OrderId { get; init; }

    public Order() { } // callers use object-initializer syntax to satisfy `required`

    [SetsRequiredMembers]
    public Order(string customerName, string orderId)
    {
        CustomerName = customerName;
        OrderId = orderId;
    }
}

Order fromCtor = new("Nancy Davolio", "ORD-1001");       // OK: SetsRequiredMembers vouches for this
Order fromInit = new() { CustomerName = "x", OrderId = "y" }; // OK: satisfies `required` directly
```

A type can keep a positional constructor for convenience while still declaring its properties
`required` for object-initializer callers; `[SetsRequiredMembers]` on that constructor tells the
compiler it assigns every required member itself, so the constructor doesn't also have to be called
through object-initializer syntax to satisfy the check.

## Requirements and restrictions

- `required` is enforced at every construction site as a compiler **error**, not a nullable
  warning — a caller who omits a required member fails to compile regardless of whether nullable
  warnings are enabled at all; the two features are independently useful and happen to compose.
- `required` works on properties/fields of any type, nullable or not — putting `required` on a
  nullable-typed property (`public required string? Notes { get; init; }`) is legal and simply
  means callers must set it explicitly, even if the value they set is `null`.
- A constructor that assigns every required member still needs `[SetsRequiredMembers]` to be usable
  without also satisfying the object-initializer requirement; without it, callers using that
  constructor must still additionally set every required member through initializer syntax.

## Fallback

Below C# 11, there's no `required` modifier. Fall back to a constructor that takes one parameter per
non-nullable member and assigns it directly — the pattern the
[C# 8.0 tier](csharp8-nullable-reference-types.md) already relies on — or, for a helper-method
initialization path, `[MemberNotNull]` from the
[C# 9.0 tier](csharp9-unconstrained-generic-nullability.md). Both remain the correct answer to "how
do I satisfy definite assignment without `required`" on any pre-C#-11 target.
