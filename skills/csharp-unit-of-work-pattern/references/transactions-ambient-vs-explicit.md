# Transactions: Ambient vs. Explicit

A unit of work needs a transaction underneath it to make its single commit call atomic. There are
two ways to obtain one: an **explicit** transaction the unit of work opens and commits itself, or an
**ambient** transaction that flows implicitly through the call stack via `TransactionScope`.

## Explicit transactions

The unit of work owns a transaction object directly — begins it in its constructor (or on first
use), commits it on `SaveChanges`, rolls it back on failure or disposal without a commit:

```csharp
public sealed class SqlUnitOfWork : IUnitOfWork
{
    private readonly IDbTransaction _transaction;

    public SqlUnitOfWork(IDbConnection connection)
    {
        connection.Open();
        _transaction = connection.BeginTransaction();
    }

    public int SaveChanges()
    {
        try
        {
            int affected = FlushPendingWrites();
            _transaction.Commit();
            return affected;
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
    }

    private int FlushPendingWrites() => 0;
}
```

Every repository the unit of work hands out is constructed against this same transaction object, so
every write any of them makes participates in it automatically. Explicit transactions are visible,
scoped to exactly the connection the unit of work opened, and require no extra runtime
infrastructure — they are the right default for a unit of work that talks to a single database.

## Ambient transactions

`TransactionScope` makes a transaction implicit: code inside a `using (new TransactionScope())`
block automatically enlists any transaction-aware resource it touches, without that code needing a
transaction object passed to it.

```csharp
public int SaveChanges()
{
    using var scope = new TransactionScope(TransactionScopeOption.Required);

    int affected = FlushPendingWrites();

    scope.Complete(); // marks success; omit to roll back everything enlisted in the scope
    return affected;
}
```

`TransactionScopeOption.Required` joins an existing ambient transaction if the caller already
started one, or creates a new one if not — which is what lets a unit of work compose inside a
larger operation that itself opened a scope, without either side needing to know about the other.

Ambient transactions solve a real problem explicit ones don't: coordinating writes across
**multiple physical connections**, or even multiple resource managers, as one atomic unit — a
capability an explicit `IDbTransaction` tied to a single `IDbConnection` doesn't have on its own.
That capability comes with real costs: it can escalate to a distributed transaction coordinator
under the hood when more than one connection enlists, which most single-database projects want to
avoid, and the enlistment is implicit — a reader has to know that *any* transaction-aware call
inside the `using` block participates, not just the ones that look transactional.

## Choosing between them

Use an explicit transaction when the unit of work coordinates repositories against a single
connection — the common case, and the one the interface and repository shapes in
[unit-of-work-interface-and-repositories.md](unit-of-work-interface-and-repositories.md) assume.
Reach for an ambient transaction only when the unit of work must span more than one connection or
resource manager as a single atomic operation, and accept the distributed-transaction-coordinator
cost that can come with it.

## Rollback on partial failure

Regardless of which kind of transaction backs it, `SaveChanges` rolls back and rethrows rather than
swallowing the failure — a caller that catches the exception from `SaveChanges` sees the store
unchanged from before the operation started, never a partially applied set of writes:

```csharp
try
{
    unitOfWork.Orders.Add(order);
    unitOfWork.Inventory.Update(reservedItem);
    unitOfWork.SaveChanges();
}
catch (DbException)
{
    // both the order add and the inventory update were rolled back together
    throw;
}
```

A unit of work that catches an internal failure and returns a falsy result instead of throwing
still must roll back before returning — the transaction's rollback is the mechanism that guarantees
atomicity; the return value or exception is only how the caller finds out about it.
