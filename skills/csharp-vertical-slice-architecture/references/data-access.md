# Data access

## Choose the approach

Follow the repo: if it uses EF Core, NHibernate, Dapper, or a document store, keep it (standing rule 2). If
slices already go through repositories, keep that; if they use the context directly, keep that. In a new
project, present these options and ask:

| Option | Slice depends on | Strengths | Costs |
| --- | --- | --- | --- |
| **ORM session directly** | `DbContext` or NHibernate `ISession` | Simplest code; each slice writes exactly the query it needs; no extra types | Unit tests need a real database; slices see every table |
| **Repository per aggregate** | `IOrderRepository` defined in the domain | Domain-shaped API; stubbable for unit tests; supports several stores | More types; queries leak into many narrow methods; never return `IQueryable<T>` from it |
| **Specifications** | A generic repository plus specification objects | Reusable query objects; keeps repository narrow | Indirection; a library or custom evaluator |
| **Unit of work exposing providers** | `IUnitOfWork` with the data sources a slice needs | One commit boundary across the context and other sources; one seam for tests | You own the abstraction; atomicity across non-transactional stores is limited |
| **Micro-ORM / SQL per slice** | `DbConnection` / `NpgsqlDataSource` and a micro-ORM | Full control of read SQL; fast | Hand-written SQL per slice; mapping by hand |

Mixing is normal: commands through the ORM session, a few hot queries through SQL.

## Facts that decide it

- "DbContext is a combination of the Unit Of Work and Repository patterns"
  (<https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext>).
- "Repositories shouldn't be mandatory." Using the `DbContext` directly results in the simplest code;
  custom repositories help when you need to mock data access in unit tests
  (<https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design>).
- A `DbContext` is designed for a single unit of work. In a web app, one request is usually one unit of
  work, and `AddDbContext` registers the context as scoped
  (<https://learn.microsoft.com/ef/core/dbcontext-configuration/>).
- For a reliable test double, "a repository layer is the only approach allowing the comprehensive and
  reliable stubbing/mocking of the data layer", and such a repository cannot expose `IQueryable`-returning
  methods. Repositories also help when the app supports more than one database
  (<https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy>).
- Soft delete and tenant isolation are not reasons for a repository. EF Core global query filters enforce
  both at the context, and EF Core 10 adds named filters you can disable one at a time:
  `HasQueryFilter("SoftDelete", ...)`, `IgnoreQueryFilters(["SoftDelete"])`
  (<https://learn.microsoft.com/ef/core/querying/filters>).
- Bogard's sample and Wolverine use the context directly and recommend against wrapping it. The ardalis
  `min-clean` template forbids slices from touching infrastructure and uses repositories plus
  specifications. Both are maintained, current references.

## EF Core in a slice

```csharp
internal sealed class CancelOrderHandler(AppDbContext db, TimeProvider clock)
{
    public async Task<CancelOrderResult> HandleAsync(Guid orderId, CancellationToken ct = default)
    {
        Order? order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct).ConfigureAwait(false);
        if (order is null)
        {
            return new OrderNotFound(orderId);
        }

        if (order.ShippedAt is { } shippedAt)
        {
            return new OrderAlreadyShipped(orderId, shippedAt);
        }

        order.Cancel(clock.GetUtcNow());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new OrderCancelled(orderId);
    }
}
```

- Commands: track, change through entity methods, `SaveChangesAsync` once.
- Queries: `AsNoTracking()` and `Select` straight into the response record.
- Bulk changes: `ExecuteUpdateAsync` / `ExecuteDeleteAsync`. They bypass the change tracker and entity
  methods, so use them only where no invariant lives on the entity.
- Shared queries across areas: a method on the context or an `IQueryable<T>` extension
  (`db.Orders.ForCustomer(id)`), returning projections.

## NHibernate in a slice

NHibernate's `ISession` is its unit of work; `ISessionFactory` is the expensive, thread-safe object built
once. NHibernate 5.7.1 is the current release on NuGet (checked 2026-11-13), and its API has async
counterparts (`GetAsync`, `ToListAsync`, `CommitAsync`).

```csharp
// Registration: one factory, one session per request scope.
services.AddSingleton<ISessionFactory>(_ => BuildSessionFactory(configuration));
services.AddScoped(sp => sp.GetRequiredService<ISessionFactory>().OpenSession());

internal sealed class CancelOrderHandler(ISession session, TimeProvider clock)
{
    public async Task<CancelOrderResult> HandleAsync(Guid orderId, CancellationToken ct = default)
    {
        using ITransaction tx = session.BeginTransaction();

        Order? order = await session.GetAsync<Order>(orderId, ct).ConfigureAwait(false);
        if (order is null)
        {
            return new OrderNotFound(orderId);
        }

        if (order.ShippedAt is { } shippedAt)
        {
            return new OrderAlreadyShipped(orderId, shippedAt);
        }

        order.Cancel(clock.GetUtcNow());
        await tx.CommitAsync(ct).ConfigureAwait(false);
        return new OrderCancelled(orderId);
    }
}
```

- Run every read and write inside a transaction. Disposing an uncommitted transaction rolls it back, so
  early returns are safe.
- Commit in one place: in the handler (above) or in a pipeline step that wraps every command (as
  ContosoUniversity does with EF Core). Choose one per repo.
- Queries: `session.Query<Order>()` (LINQ) with `Select` into the response record, or QueryOver/HQL when
  the repo uses them.
- Mappings (`ClassMapping<T>`, Fluent NHibernate, or XML) live in infrastructure, never on entities.

## Unit of work exposing providers

Use this when a slice's commit spans more than the ORM session (a document store, a blob store, a search
index), or when the repo wants slices to depend on an abstraction instead of the concrete context.

```csharp
// Shared infrastructure abstraction. Slices depend on this; infrastructure implements it.
public interface IUnitOfWork
{
    AppDbContext Database { get; }       // or ISession, or a domain-defined store interface
    IBlobStore Files { get; }
    IOutbox Outbox { get; }

    Task CommitAsync(CancellationToken ct = default);
}

internal sealed class UnitOfWork(AppDbContext database, IBlobStore files, IOutbox outbox) : IUnitOfWork
{
    public AppDbContext Database => database;
    public IBlobStore Files => files;
    public IOutbox Outbox => outbox;

    public async Task CommitAsync(CancellationToken ct = default)
    {
        // The outbox writes rows into the same DbContext, so they commit atomically with the entities.
        await database.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
```

Rules:

- Expose providers the slice needs, not one property per table. A provider is a data source (context,
  session, store), not a repository per entity.
- Only resources that share one transaction commit atomically. A database and a blob store do not. For
  non-transactional providers, write the database first and use the outbox (or a compensating action) for
  the rest. See [messaging-and-consistency.md](messaging-and-consistency.md).
- Register it scoped, so every slice in one request shares one instance.
- In tests, replace providers, not the unit of work.

## Repository per aggregate

Add one for a named reason: unit tests must stub data access, the app supports several stores, or many
slices load one aggregate with the same complex rules.

```csharp
// Domain/Orders/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> FindAsync(Guid id, CancellationToken ct = default);
    void Add(Order order);
}
```

- Define the interface in the domain; implement it in infrastructure.
- Repositories serve commands (load and save aggregates). Queries project directly, through a read
  service or the session, not through the repository.
- Do not return `IQueryable<T>`: it leaks the ORM and defeats stubbing.
- A generic `IRepository<T>` over an ORM repeats what the context already does. Avoid it unless the repo
  already standardizes on it.

## Transactions

One command slice is one unit of work. Pick one boundary per repo and apply it everywhere:

| Boundary | How |
| --- | --- |
| Handler | One `SaveChangesAsync` (implicit transaction) or one `BeginTransaction`/`Commit` per handler |
| Pipeline step / filter | A step opens the transaction before the handler and commits after it; handlers never save (ContosoUniversity) |
| Unit of work | The handler calls `IUnitOfWork.CommitAsync` once |

Queries need no write transaction. Use no-tracking reads, or NHibernate `ReadOnly` / a read-only
transaction when the repo does.

## Migrations

Schema migrations are global, not per slice. Keep them in `Infrastructure/Persistence/Migrations/` (or the
repo's existing location). A slice that needs a schema change adds one migration named for the change
(`AddOrderCancelledAt`), not for the slice. See [platform-and-build.md](platform-and-build.md).
