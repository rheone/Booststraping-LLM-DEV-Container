# Provider Configuration

Each relational database needs its own EF Core provider package, registered through the matching
`Use*` extension method inside your `AddDbContext` (or `OnConfiguring`) delegate.

## SQL Server

Package: `Microsoft.EntityFrameworkCore.SqlServer`.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
```

`EnableRetryOnFailure()` turns on the SQL Server provider's execution strategy for transient-fault
retries — worth enabling whenever the database is reachable over an unreliable network (managed
cloud SQL Server, Azure SQL). An execution strategy that retries changes how you must scope manual
transactions: wrap the whole retryable unit (including `BeginTransaction`) inside
`ExecutionStrategy.ExecuteAsync`, not just the `SaveChanges` call, or the retry will re-run a
partially committed transaction.

## Npgsql (PostgreSQL)

Package: `Npgsql.EntityFrameworkCore.PostgreSQL`.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
```

PostgreSQL identifiers are case-sensitive when quoted and the Npgsql provider lower-cases unquoted
identifiers by convention; a hand-written migration or raw SQL fragment that mixes casing
assumptions from a SQL Server background is a common source of "relation does not exist" errors.
The provider also maps some CLR types differently than SQL Server (e.g. `DateTime` vs. `timestamp`
vs. `timestamptz` — PostgreSQL distinguishes timezone-aware and naive timestamp columns explicitly,
where SQL Server's `datetime2` does not), so a column type mapping copied from SQL Server guidance
does not always carry over unchanged.

## SQLite

Package: `Microsoft.EntityFrameworkCore.Sqlite`.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
```

SQLite has a narrower type system than SQL Server or PostgreSQL (it stores values in a small set of
storage classes and applies type affinity rather than enforcing a declared column type strictly).
Some relational operations EF Core would otherwise translate to SQL fail against SQLite specifically
because the engine doesn't support them (e.g. certain `DateTimeOffset` operations, some decimal
arithmetic) — a query that works against SQL Server or PostgreSQL is not guaranteed to translate the
same way against SQLite. Treat SQLite as a genuine target with its own translation quirks, not
merely a lightweight stand-in, if production also targets it; if SQLite is only used for fast local
development against a different production provider, be aware the two won't always exercise
identical query-translation code paths.

## Multiple providers, one context

A single `DbContext` type is normally wired to one provider per running application. Supporting more
than one provider for the same context (e.g. SQLite for local dev, SQL Server for production)
means switching which `Use*` call the `AddDbContext` delegate makes based on configuration — not
calling more than one `Use*` method against the same options builder.
