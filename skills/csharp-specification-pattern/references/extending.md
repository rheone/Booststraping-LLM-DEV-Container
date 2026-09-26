# Extending Specifications

## Adding a new specification

A new business rule becomes a new class implementing `ISpecification<T>` (or extending
`Specification<T>` for composition support) — nothing about an existing specification, composite, or
repository method needs to change:

```csharp
public sealed class HasOverdueInvoiceSpecification : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer candidate) =>
        candidate.Invoices.Any(i => i.IsOverdue);
}
```

It composes with every existing specification immediately, because composition is defined on the
shared `Specification<T>` base, not on any specific pair of concrete types:

```csharp
Specification<Customer> collectionsCandidate =
    new HasOverdueInvoiceSpecification().And(new ActiveCustomerSpecification());
```

## Adding a new composition operator

`And`/`Or`/`Not` cover the common cases; a project that needs another combinator (an "exactly one
of" / XOR specification, for example) adds it the same way the existing three were built — as
another `Specification<T>` subclass taking one or two inner specifications:

```csharp
public sealed class XorSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public XorSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) != _right.IsSatisfiedBy(candidate);
}
```

```csharp
public abstract class Specification<T> : ISpecification<T>
{
    // existing And/Or/Not ...
    public Specification<T> Xor(Specification<T> other) => new XorSpecification<T>(this, other);
}
```

Adding a method to the shared base is additive for every existing subclass — none of them override
or otherwise depend on the base's method set being exactly what it was before.

## Adding expression support to a specification hierarchy that didn't have it

A hierarchy built purely around `IsSatisfiedBy` (no `ToExpression()`) can add expression support
without breaking existing callers by introducing the expression member as a new abstract method with
the base providing `IsSatisfiedBy` in terms of it going forward — but every existing concrete
specification then needs updating to implement the new abstract member, which is a breaking change
for that hierarchy's implementers even though it's additive for callers. Weigh that cost against
building expression support only into new specifications that need it, going forward, and leaving
existing pure-`IsSatisfiedBy` specifications as they are if they're never used against
`IQueryable<T>`.

## What doesn't break existing code

Adding a new leaf specification, a new composition operator, or a new repository method that accepts
a specification are all additive. What does break existing code: changing an existing
specification's `IsSatisfiedBy` (or `ToExpression()`) logic to mean something different — every
composite and every call site built on the old meaning silently starts behaving differently, with no
compiler error to catch it, since the signature hasn't changed, only the behavior. Treat a meaning
change to an existing specification as a breaking change requiring the same scrutiny as a public
API's behavior change, not as a safe internal tweak.
