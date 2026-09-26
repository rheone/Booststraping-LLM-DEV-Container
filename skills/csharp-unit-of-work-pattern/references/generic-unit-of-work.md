# Generic Unit of Work

Named-property `IUnitOfWork` interfaces (one property per aggregate — see
[unit-of-work-interface-and-repositories.md](unit-of-work-interface-and-repositories.md)) grow a
new member every time a new entity type needs coordinating. A generic unit of work instead exposes
one type-parameterized accessor that hands back a repository for whatever entity type the caller
asks for, keeping the interface itself fixed as the entity model grows.

## The generic interface

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;
    int SaveChanges();
}
```

## Implementing it with a per-type repository cache

```csharp
public sealed class GenericUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;
    private readonly Dictionary<Type, object> _repositories = new();
    private bool _disposed;

    public GenericUnitOfWork(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
        _transaction = _connection.BeginTransaction();
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if (_repositories.TryGetValue(typeof(TEntity), out object? existing))
        {
            return (IRepository<TEntity>)existing;
        }

        var repository = new Repository<TEntity>(_connection, _transaction);
        _repositories[typeof(TEntity)] = repository;
        return repository;
    }

    public int SaveChanges()
    {
        try
        {
            int affected = FlushAll();
            _transaction.Commit();
            return affected;
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _transaction.Dispose();
        _connection.Dispose();
        _disposed = true;
    }

    private int FlushAll() => /* flush every cached repository's queued operations */ 0;
}
```

Caching one repository instance per entity type (rather than constructing a new one on every call)
means a caller that asks for `Repository<Order>()` twice within the same unit of work gets the same
instance back, so writes queued through the first reference are visible to code holding the second.

## Generic repository backing it

```csharp
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public Repository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public TEntity? Find(object id) => /* query by key */ default;
    public void Add(TEntity entity) { /* queue insert */ }
    public void Update(TEntity entity) { /* queue update */ }
    public void Remove(TEntity entity) { /* queue delete */ }
}
```

A specific aggregate that needs query methods beyond the generic `Find`/`Add`/`Update`/`Remove`
shape gets its own narrow interface (`IOrderRepository : IRepository<Order>`) and its own
implementation registered ahead of the generic fallback, rather than forcing every bespoke query
onto the generic interface itself.

## Tradeoff versus named properties

The generic form trades discoverability (an IDE can't autocomplete `unitOfWork.Repository<Order>()`
the way it autocompletes `unitOfWork.Orders`) for a fixed interface surface that never grows with
the entity model. Pick named properties when the set of coordinated aggregates is small and stable;
pick the generic form when it's large or changes often enough that a growing interface would itself
become a maintenance burden.
