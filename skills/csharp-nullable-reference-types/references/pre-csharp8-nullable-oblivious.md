# Before Nullable Reference Types (Before C# 8.0)

Before C# 8.0, every reference type was implicitly nullable and the compiler tracked none of it.
`string` and `string?` were the same type — there was no `?` syntax for reference types at all,
because `?` had exactly one meaning in the language: `Nullable<T>` for value types (`int?`,
`DateTime?`), introduced in C# 2.0. A `string` parameter, field, or return value could always be
`null`, always compiled without complaint, and a `NullReferenceException` at the dereference site
was the only signal that a `null` had gone somewhere it shouldn't. This state is what the C# 8.0
nullable-reference-types design docs call the **oblivious** context: the compiler has no opinion
about nullability at all, not even a lenient one.

## Syntax

There is no dedicated syntax — this is the absence of a feature, not a form of it.

```csharp
public class Order
{
    public string CustomerNote { get; set; } // could be null; nothing says so
    public Customer Customer { get; set; }   // could be null; nothing says so
}
```

## Basic use case: defensive checks as the only tool

```csharp
public string Describe(Order order)
{
    if (order == null)
    {
        throw new ArgumentNullException(nameof(order));
    }

    // CustomerNote might still be null here -- nothing forces this check,
    // and forgetting it compiles cleanly.
    return order.CustomerNote != null
        ? order.CustomerNote
        : "(no note)";
}
```

Every null-safety guarantee had to be enforced by convention: a defensive `if (x == null) throw`
at the top of every public method, a team style guide, or a third-party static analyzer bolted on
top of the compiler (Code Contracts, ReSharper's own `[NotNull]`/`[CanBeNull]` annotations, or
FxCop/Roslyn analyzer rules). None of these were part of the language, none were enforced by the
compiler that shipped in the box, and a caller outside the team's own tooling saw no difference
between a parameter that tolerated `null` and one that didn't — the method signature alone never
said which.

## Advanced use case: the workaround pattern for "this can never be null"

```csharp
public class Repository
{
    private readonly List<Order> _orders = new List<Order>();

    public Order GetOrder(int id)
    {
        Order found = _orders.FirstOrDefault(o => o.Id == id);
        if (found == null)
        {
            throw new InvalidOperationException($"Order {id} not found.");
        }

        return found; // guaranteed non-null by the throw above, but the signature
                       // gives a caller no compile-time way to know that
    }
}
```

A method that genuinely never returns `null` and a method that might both had the exact same
signature, `Order GetOrder(int id)` — the only way a caller learned which was true was documentation
(if it existed) or reading the implementation.

## Requirements and restrictions

- `?` is reserved for `Nullable<T>` over value types; it does not parse on a reference type at all
  before C# 8.0 — `string?` is a compile error, not a lenient no-op.
- No compiler warning exists for storing, returning, or dereferencing a potentially-null reference
  anywhere in this era.
- Nothing distinguishes "callers may pass null" from "callers must not pass null" in a method
  signature; that contract lives entirely outside the type system.

## Fallback

This is the first tier; there is no older fallback. Every C# version from 1.0 through 7.x is
nullable-oblivious in this sense, and this is also the pattern to fall back to when a project must
target a pre-C#-8 compiler (or has `#nullable disable`/no `<Nullable>` setting at all): defensive
`if (x == null)` checks by convention, with no compiler backing them up.
