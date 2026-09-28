# Core Concept and Interface

## The problem

A business rule expressed as an inline condition — `customer.Status == CustomerStatus.Active &&
customer.Orders.Count > 0` — tends to get copy-pasted everywhere it's needed: a query filter here,
a validation check there, a UI enablement condition somewhere else. When the rule changes, every
copy needs finding and updating, and nothing enforces that the copies stay in sync in the meantime.

## The pattern

A specification wraps a single business rule or query predicate in its own named type, satisfying
one interface:

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
}
```

```csharp
public sealed class ActiveCustomerWithOrdersSpecification : ISpecification<Customer>
{
    public bool IsSatisfiedBy(Customer candidate) =>
        candidate.Status == CustomerStatus.Active && candidate.Orders.Count > 0;
}
```

The rule now lives in exactly one place. Every call site that needs it references the specification
type instead of re-deriving the condition:

```csharp
ISpecification<Customer> spec = new ActiveCustomerWithOrdersSpecification();

if (spec.IsSatisfiedBy(customer))
{
    // eligible
}

List<Customer> eligible = allCustomers.Where(spec.IsSatisfiedBy).ToList();
```

A rule change (adding a minimum order total, say) touches this one class; every caller picks up the
change automatically the next time it runs.

## Why a named type, not just a `Func<T, bool>`

A bare `Func<T, bool>` gets the same reuse for a single predicate, but loses two things a named
specification type keeps: a name a reader recognizes at the call site (`IsSatisfiedBy` on an
`ActiveCustomerWithOrdersSpecification` reads as a stated business rule; an anonymous lambda doesn't
carry that label), and a stable identity that composition (see
[composition-and-or-not.md](composition-and-or-not.md)) and expression conversion (see
[expression-conversion-and-iqueryable.md](expression-conversion-and-iqueryable.md)) can both build
on. Nothing about the pattern forbids also exposing a `Func<T, bool>` view of a specification for
convenience — the named type is the thing composition and translation operate on.

## Parameterized specifications

A specification with configuration data takes it through its constructor, keeping `IsSatisfiedBy`
itself parameterless beyond the candidate:

```csharp
public sealed class OrdersOverAmountSpecification : ISpecification<Customer>
{
    private readonly decimal _minimumTotal;

    public OrdersOverAmountSpecification(decimal minimumTotal) => _minimumTotal = minimumTotal;

    public bool IsSatisfiedBy(Customer candidate) =>
        candidate.Orders.Sum(o => o.Total) >= _minimumTotal;
}
```

```csharp
ISpecification<Customer> bigSpenders = new OrdersOverAmountSpecification(minimumTotal: 1000m);
```

Each distinct threshold is a new instance of the same specification type, not a new type — the
specification's *type* captures the shape of the rule, and its constructor arguments capture the
specific values being checked for.

## Where this leads

A single-condition specification like the ones above is the starting point. The pattern's real
value shows up once specifications combine (see
[composition-and-or-not.md](composition-and-or-not.md)) and once they can express themselves as
something a query provider can translate to a store-side filter (see
[expression-conversion-and-iqueryable.md](expression-conversion-and-iqueryable.md)) instead of only
ever running against objects already loaded into memory.
