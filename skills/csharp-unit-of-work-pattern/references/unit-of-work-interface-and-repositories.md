# Unit of Work Interface and Repositories

## The interface

An `IUnitOfWork` exposes the repositories a caller needs and one commit method. Each repository it
hands out must share the same underlying connection or session as every other repository from the
same `IUnitOfWork` instance — that shared session is what makes a single commit apply to all of
them.

```csharp
public interface IUnitOfWork : IDisposable
{
    IOrderRepository Orders { get; }
    IInventoryRepository Inventory { get; }
    IAuditRepository Audit { get; }

    int SaveChanges();
}
```

Exposing named repository properties (`Orders`, `Inventory`, `Audit`) reads clearly at call sites
and lets each repository keep its own bespoke methods (`Orders.MarkFulfilled(orderId)`) alongside
the common `Add`/`Update`/`Remove` shape. The tradeoff is that the interface grows a new property
every time a new aggregate needs coordinating — see
[generic-unit-of-work.md](generic-unit-of-work.md) for a factory-style alternative that avoids that
growth.

## Implementing it

```csharp
public sealed class SqlUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;
    private bool _disposed;

    public SqlUnitOfWork(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
        _transaction = _connection.BeginTransaction();

        Orders = new OrderRepository(_connection, _transaction);
        Inventory = new InventoryRepository(_connection, _transaction);
        Audit = new AuditRepository(_connection, _transaction);
    }

    public IOrderRepository Orders { get; }
    public IInventoryRepository Inventory { get; }
    public IAuditRepository Audit { get; }

    public int SaveChanges()
    {
        try
        {
            int affected = _transaction.Connection is null ? 0 : ExecutePendingWrites();
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

    private int ExecutePendingWrites() => /* flush each repository's queued operations */ 0;
}
```

The exact mechanics of `ExecutePendingWrites` depend on whether each repository executes its writes
immediately against the open transaction (most common for a thin repository over raw SQL) or queues
them for the unit of work to flush (closer to how a change-tracking context behaves — see
[change-tracking-contexts-as-unit-of-work.md](change-tracking-contexts-as-unit-of-work.md)). Either
way, the caller-facing contract is the same: mutate through the repositories, call `SaveChanges()`
once.

## Repository shape a unit of work expects

A repository a unit of work coordinates takes its connection/session/transaction as a constructor
dependency rather than opening its own — that's what lets the unit of work guarantee every
repository it hands out participates in the same transaction:

```csharp
public interface IRepository<TEntity> where TEntity : class
{
    TEntity? Find(object id);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
```

A repository that opens its own connection internally cannot be coordinated this way — nothing
would tie its writes to the same transaction as any other repository's, defeating the pattern.

## Caller usage

```csharp
public sealed class PlaceOrderHandler
{
    private readonly IUnitOfWorkFactory _factory;

    public PlaceOrderHandler(IUnitOfWorkFactory factory) => _factory = factory;

    public void Handle(PlaceOrderCommand command)
    {
        using IUnitOfWork unitOfWork = _factory.Create();

        var order = new Order(command.CustomerId, command.LineItems);
        unitOfWork.Orders.Add(order);

        foreach (var line in command.LineItems)
        {
            var item = unitOfWork.Inventory.Find(line.SkuId)
                ?? throw new InvalidOperationException($"Unknown SKU {line.SkuId}");
            item.Reserve(line.Quantity);
            unitOfWork.Inventory.Update(item);
        }

        unitOfWork.Audit.Add(new AuditEntry("OrderPlaced", order.Id));

        unitOfWork.SaveChanges();
    }
}
```

The handler never opens a transaction itself and never calls a per-repository save method — it
composes the operation entirely through the unit of work it was handed, and commits once.
