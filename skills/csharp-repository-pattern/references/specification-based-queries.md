# Specification-Based Queries

Once a repository interface accumulates enough narrow, single-purpose query methods —
`GetByRegion`, `GetByRegionAndStatus`, `GetByRegionOrderedByName`, `GetActiveByRegion` — the
interface itself becomes the bottleneck: every new combination of filter, sort, or projection a
consumer needs means another method added to the interface (and every implementation of it). A
specification-based approach replaces that growth with a single query method that accepts an object
describing what to query for.

## The specification object

A specification captures a query's criteria as data, independent of the repository that will
execute it:

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    int? Take { get; }
    int? Skip { get; }
}
```

A concrete specification expresses one meaningful, named query:

```csharp
public sealed class ActiveCustomersInRegionSpec : ISpecification<Customer>
{
    public Expression<Func<Customer, bool>> Criteria { get; }
    public List<Expression<Func<Customer, object>>> Includes { get; } = new();
    public Expression<Func<Customer, object>>? OrderBy { get; }
    public int? Take { get; }
    public int? Skip { get; }

    public ActiveCustomersInRegionSpec(string region)
    {
        Criteria = c => c.IsActive && c.Region == region;
        OrderBy = c => c.Name;
    }
}
```

## The repository's query method

The repository exposes a single method that accepts any specification and applies its criteria:

```csharp
public interface IRepository<T> where T : class
{
    IReadOnlyList<T> Find(ISpecification<T> specification);
}

public sealed class CustomerRepository : IRepository<Customer>
{
    public IReadOnlyList<Customer> Find(ISpecification<Customer> specification)
    {
        IEnumerable<Customer> query = _customers.Where(specification.Criteria.Compile());

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy.Compile());
        }

        if (specification.Skip is int skip)
        {
            query = query.Skip(skip);
        }

        if (specification.Take is int take)
        {
            query = query.Take(take);
        }

        return query.ToList();
    }
}
```

Consumer code asks for what it wants by naming a specification, not by calling a growing family of
bespoke methods:

```csharp
IReadOnlyList<Customer> customers = repository.Find(new ActiveCustomersInRegionSpec("West"));
```

## What this buys you over more narrow methods

- **The repository interface stops growing.** New query needs become new specification classes,
  never new repository methods — the interface stays at one `Find` method indefinitely.
- **Query logic is unit-testable on its own.** A specification's `Criteria` expression can be
  compiled and run against an in-memory list directly in a test, with no repository involved at all,
  isolating "is this filter correct" from "does the repository apply filters correctly."
- **Composable criteria without an open `IQueryable<T>` surface.** Specifications can be combined
  (an `AndSpecification<T>` composing two `ISpecification<T>` instances) without handing the
  consumer a live, provider-specific query-building surface the way returning `IQueryable<T>` does —
  see [iqueryable-and-leaky-abstractions.md](iqueryable-and-leaky-abstractions.md) for why that
  distinction matters.

## When narrow methods are still simpler

For a repository with only two or three genuinely distinct queries that are never expected to grow,
introducing the specification abstraction (the interface, the expression compilation, the
translation logic in `Find`) is more machinery than the problem calls for. Reach for
specification-based querying once the number of narrow methods is visibly growing and starting to
duplicate filter logic across them, not preemptively for a repository that only ever needs
`GetById` and one or two named lookups.
