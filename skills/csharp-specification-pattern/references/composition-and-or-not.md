# Composition: And, Or, Not

Individual specifications combine into composite specifications the same way boolean expressions
combine — the composite is itself a specification, so it composes further without any special
casing at the call site.

## The composite specifications

```csharp
public sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) && _right.IsSatisfiedBy(candidate);
}

public sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) || _right.IsSatisfiedBy(candidate);
}

public sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _inner;

    public NotSpecification(Specification<T> inner) => _inner = inner;

    public override bool IsSatisfiedBy(T candidate) => !_inner.IsSatisfiedBy(candidate);
}
```

`AndSpecification<T>`, `OrSpecification<T>`, and `NotSpecification<T>` are each specifications
themselves — every one of them satisfies `ISpecification<T>` and inherits `And`/`Or`/`Not` from
[generic-specification-base.md](generic-specification-base.md)'s abstract base, which is what makes
arbitrary-depth composition possible without a distinct type for every combination.

## Composing

```csharp
var isActive = new ActiveCustomerSpecification();
var hasOrders = new HasOrdersSpecification();
var isVip = new VipTierSpecification();

Specification<Customer> eligibleForPromo = isActive.And(hasOrders).Or(isVip);

bool result = eligibleForPromo.IsSatisfiedBy(customer);
```

`eligibleForPromo` reads as "active and has orders, or VIP" — the composite's structure mirrors the
business rule's own structure, and each leaf specification (`isActive`, `hasOrders`, `isVip`)
remains independently reusable and independently testable (see [testing.md](testing.md)).

## Operator overloads as an alternative call shape

Overloading `&`, `|`, and `!` on the base class gives the same composition a symbolic shape some
codebases prefer over named methods:

```csharp
public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);

    public static Specification<T> operator &(Specification<T> left, Specification<T> right) =>
        new AndSpecification<T>(left, right);

    public static Specification<T> operator |(Specification<T> left, Specification<T> right) =>
        new OrSpecification<T>(left, right);

    public static Specification<T> operator !(Specification<T> spec) => new NotSpecification<T>(spec);
}
```

```csharp
Specification<Customer> eligibleForPromo = (isActive & hasOrders) | isVip;
```

Both call shapes produce the identical composite tree — pick whichever reads more naturally for a
given codebase's style and keep it consistent; mixing `And(...)` calls and `&` operators for the
same kind of composition in the same codebase reads as two conventions doing one job.

## Composing at depth

Composition has no built-in depth limit — `a.And(b).Or(c.Not()).And(d)` builds a tree of composite
specifications exactly as `(a && b) || (!c && d)` builds a tree of boolean expressions. A
specification tree that's grown deep and hard to read at a glance is a sign the underlying business
rule itself has become complex enough to warrant breaking into fewer, better-named intermediate
specifications rather than one large composed expression — the same signal a deeply nested boolean
expression gives in ordinary code.
