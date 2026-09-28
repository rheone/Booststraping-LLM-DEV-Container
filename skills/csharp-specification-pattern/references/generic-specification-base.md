# Generic Specification Base

Every specification in this skill already implements the generic `ISpecification<T>` interface —
the type parameter is what lets the same composition and repository-integration machinery work for
a specification over `Customer`, `Order`, or any other type without rewriting it per entity. This
file covers the generic *base class* form: a reusable abstract base that implements the composition
operators once, so individual specifications only ever implement the rule itself.

## Why a base class in addition to the interface

Without a shared base, every concrete specification that wants `And`/`Or`/`Not` support (see
[composition-and-or-not.md](composition-and-or-not.md)) needs to implement those methods itself,
duplicating identical composition logic across every specification type. A generic abstract base
implements composition once; concrete specifications inherit it for free.

```csharp
public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);
    public Specification<T> Not() => new NotSpecification<T>(this);
}
```

A concrete specification now only implements the rule:

```csharp
public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer candidate) => candidate.Status == CustomerStatus.Active;
}
```

## Generic composite specifications

The composite specifications `And`/`Or`/`Not` construct are themselves generic over the same `T`,
so one implementation of each handles every entity type a project defines specifications for — see
[composition-and-or-not.md](composition-and-or-not.md) for their full implementation.

## Interface vs. abstract base: when each is enough

Implement `ISpecification<T>` directly for a specification that never needs to compose with others
and never needs expression conversion — a one-off in-memory check used in exactly one place. Reach
for the `Specification<T>` abstract base as soon as composition or expression conversion (see
[expression-conversion-and-iqueryable.md](expression-conversion-and-iqueryable.md)) is needed
anywhere, since both of those build on the shared base rather than on the bare interface.

## A generic base with expression support

A base that also demands an expression form (rather than only a boolean predicate) folds both
concerns into one abstract member, so every concrete specification supplies exactly one thing and
gets both in-memory evaluation and query translation for free:

```csharp
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T candidate) => ToExpression().Compile().Invoke(candidate);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);
    public Specification<T> Not() => new NotSpecification<T>(this);
}
```

```csharp
public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression() =>
        customer => customer.Status == CustomerStatus.Active;
}
```

`IsSatisfiedBy` compiling the expression on every call has a real per-call cost — acceptable for
occasional in-memory checks, wasteful in a tight loop. A specification used exclusively for
in-memory filtering over a large in-memory sequence gets more from the plain `IsSatisfiedBy`-first
base shown earlier in this file; a specification used for both in-memory checks and query
translation gets more from this expression-first base. See
[expression-conversion-and-iqueryable.md](expression-conversion-and-iqueryable.md) for the
translation side in full.
