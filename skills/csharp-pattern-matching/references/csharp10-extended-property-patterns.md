# Extended Property Patterns (C# 10.0)

C# 10.0 shipped November 2021 with .NET 6 and added **extended property patterns**: dot-notation
access to a *nested* property or field directly inside a property pattern's braces, instead of
requiring a separate pair of braces for every level of nesting. This is a pure syntax
simplification — it doesn't change what's matchable, only how tersely a nested property check can
be written.

## Syntax

```csharp
// Before C# 10: one brace pair per nesting level
bool isManagerNamedSmith = employee is { Manager: { LastName: "Smith" } };

// C# 10+: dot-notation reaches straight to the nested property
bool isManagerNamedSmithExtended = employee is { Manager.LastName: "Smith" };
```

## Basic use case

```csharp
public record Address(string City, string State);
public record Customer(string Name, Address Address);

public static bool IsCaliforniaCustomer(Customer customer) =>
    customer is { Address.State: "CA" };
```

Without extended property patterns, the same check needs a nested property pattern:
`customer is { Address: { State: "CA" } }`. Both compile to the same thing; the dot-notation form
just reads like ordinary member access.

## Advanced use case: mixing extended access with other pattern kinds in a switch expression

```csharp
public record Order(Customer Customer, decimal Total, bool IsInternational);

public static decimal GetShippingCost(Order order) => order switch
{
    { Customer.Address.State: "CA", Total: >= 100m } => 0m,
    { Customer.Address.Country: "US", IsInternational: false } => 5.99m,
    { IsInternational: true } => 24.99m,
    _ => 9.99m,
};
```

`Customer.Address.State` chains through two levels of nesting (`Order.Customer`, then
`Customer.Address`) in one dot-path, combined with a sibling `Total: >= 100m` relational pattern in
the same property pattern — extended property patterns compose with every other pattern kind
covered elsewhere in this skill exactly like a regular property pattern does, since it's the same
underlying construct with shorter syntax.

## Requirements and restrictions

- Every step in the dot-path must itself be an accessible, non-indexed property or field — the
  same accessibility rules that apply to writing `order.Customer.Address.State` as an ordinary
  member-access expression apply here.
- If any intermediate value in the path is `null` at run time, the whole pattern simply fails to
  match (the same short-circuit behavior a nested property pattern already had) — it does not
  throw a `NullReferenceException`.
- The IDE0170 analyzer (`Simplify property pattern`) flags places a nested property pattern could
  be rewritten using the dot-notation form.

## Fallback

Below C# 10.0, write a nested property pattern with one brace pair per level instead of a
dot-path:

```csharp
public static bool IsCaliforniaCustomer(Customer customer) =>
    customer is { Address: { State: "CA" } };
```

Every other pattern-matching capability is unchanged from
[csharp9-relational-and-logical-patterns.md](csharp9-relational-and-logical-patterns.md); this tier
only affects how nested property access is spelled.
