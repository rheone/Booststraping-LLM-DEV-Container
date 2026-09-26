# Repository Integration

## The problem specifications solve here

A repository that grows a new query method for every distinct filtering need
(`FindActiveCustomers`, `FindCustomersWithOrdersOver(decimal amount)`,
`FindActiveCustomersWithOrdersOver(decimal amount)`) accumulates a combinatorial explosion of
near-duplicate methods as the number of independent filtering conditions grows — the third method
above already exists only because the first two couldn't be combined at the call site.

## Narrowing the method surface

A repository that accepts a specification instead needs far fewer methods — typically one that
finds a single match and one that finds all matches, parameterized by whatever specification the
caller supplies:

```csharp
public interface IRepository<T>
{
    T? FindOne(ISpecification<T> specification);
    IReadOnlyList<T> FindAll(ISpecification<T> specification);
}
```

```csharp
public sealed class Repository<T> : IRepository<T> where T : class
{
    private readonly IQueryable<T> _queryable;
    public Repository(IQueryable<T> queryable) => _queryable = queryable;

    public T? FindOne(ISpecification<T> specification) =>
        _queryable.FirstOrDefault(specification.ToExpression());

    public IReadOnlyList<T> FindAll(ISpecification<T> specification) =>
        _queryable.Where(specification.ToExpression()).ToList();
}
```

Every filtering need from the earlier example becomes a call with a different specification, not a
different method:

```csharp
var active = repository.FindAll(new ActiveCustomerSpecification());
var bigSpenders = repository.FindAll(new OrdersOverAmountSpecification(1000m));
var activeBigSpenders = repository.FindAll(
    new ActiveCustomerSpecification().And(new OrdersOverAmountSpecification(1000m)));
```

The repository's interface stops growing every time a new filtering combination is needed — new
combinations are composed at the call site from existing specifications, per
[composition-and-or-not.md](composition-and-or-not.md), rather than requiring a new repository
method.

## Keeping bespoke methods where they still earn their place

Not every repository method should become a specification-accepting one. A method that does more
than filter — one that eager-loads a specific set of related data, applies pagination and sorting
tied to a specific screen, or performs an aggregate calculation — still deserves its own named
method; specifications solve the "too many single-purpose filter methods" problem, not the "every
repository method must be generic" one. A repository can carry both a small set of
specification-accepting methods and a small set of bespoke methods for real one-off needs, without
that mix being a design smell.

## Ordering and paging alongside a specification

A specification is a predicate, not a full query — ordering and paging still belong on the
repository method's own parameters rather than folded into `ISpecification<T>` itself, keeping the
specification's job (does this candidate match) separate from the query's job (in what order, how
many):

```csharp
public interface IRepository<T>
{
    IReadOnlyList<T> FindAll(ISpecification<T> specification, int skip = 0, int take = int.MaxValue);
}
```
