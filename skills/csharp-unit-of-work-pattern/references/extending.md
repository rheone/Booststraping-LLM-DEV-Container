# Extending a Unit of Work

## Adding a new repository to a named-property interface

Add a new property to the interface and construct the corresponding repository against the same
connection/transaction every other repository shares:

```csharp
public interface IUnitOfWork : IDisposable
{
    IOrderRepository Orders { get; }
    IInventoryRepository Inventory { get; }
    IAuditRepository Audit { get; }
    IShipmentRepository Shipments { get; } // new

    int SaveChanges();
}
```

Every existing caller that only used `Orders`/`Inventory`/`Audit` keeps compiling unchanged — a new
property is additive. The one thing to check is whether any existing implementation of
`IUnitOfWork` besides the production one (a fake used in tests, for example) also needs the new
member added, since adding a member to an interface breaks every class that implements it without
a default.

## Adding a new repository to a generic unit of work

Nothing changes on the interface at all — a caller that needs the new entity type just asks for it:

```csharp
IRepository<Shipment> shipments = unitOfWork.Repository<Shipment>();
```

This is the main advantage the generic form (see
[generic-unit-of-work.md](generic-unit-of-work.md)) has for extension: adding a new coordinated
aggregate is a zero-change operation for the interface and every existing implementation of it.

## Adding a new kind of atomic operation

When an operation needs behavior beyond "queue writes across a few repositories, then save" —
publishing an event only after a successful commit, for example — add that as an explicit step
around `SaveChanges()` rather than baking it into the unit of work's own commit logic, so the unit
of work itself stays focused on write coordination:

```csharp
public sealed class OutboxUnitOfWork : IUnitOfWork
{
    private readonly IUnitOfWork _inner;
    private readonly List<object> _pendingEvents = new();

    public OutboxUnitOfWork(IUnitOfWork inner) => _inner = inner;

    public void EnqueueEvent(object domainEvent) => _pendingEvents.Add(domainEvent);

    public int SaveChanges()
    {
        int affected = _inner.SaveChanges();
        foreach (object domainEvent in _pendingEvents)
        {
            _inner.Repository<OutboxMessage>().Add(new OutboxMessage(domainEvent));
        }
        _pendingEvents.Clear();
        return affected;
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class => _inner.Repository<TEntity>();
    public void Dispose() => _inner.Dispose();
}
```

Wrapping an existing `IUnitOfWork` implementation this way — the decorator shape — lets a new
cross-cutting behavior layer onto the pattern without modifying the base implementation or any
existing caller that only depends on the plain `IUnitOfWork` interface.

## Adding a second store

A unit of work that needs to coordinate writes against a second, independent store (a different
database, a different connection string) either needs an ambient transaction spanning both
connections (see
[transactions-ambient-vs-explicit.md](transactions-ambient-vs-explicit.md)) or needs to accept that
true atomicity across both stores isn't achievable with a single local transaction, and design
around that explicitly — a compensating action, an outbox, or an eventually-consistent
reconciliation step — rather than assuming adding a second connection to the same explicit
transaction object will work silently. An `IDbTransaction` is scoped to the single connection that
created it.
