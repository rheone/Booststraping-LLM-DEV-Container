---
name: dotnet-ef-core
description: Guidance on Entity Framework Core (Microsoft.EntityFrameworkCore, verified current release 10.0.12) — DbContext/DbSet<T> setup, OnConfiguring vs AddDbContext dependency injection registration, provider configuration for SQL Server/Npgsql (PostgreSQL)/SQLite, migrations (Add-Migration/Update-Database, model snapshots), change tracking and SaveChanges entity states, querying (LINQ translation, Include/ThenInclude, split queries, AsNoTracking), relationship configuration (fluent API vs data annotations, owned types), and performance (compiled queries, query splitting behavior, tracking behavior). Use when writing or reviewing a DbContext, configuring an EF Core provider, authoring or troubleshooting a migration, debugging a LINQ query that won't translate, diagnosing N+1 query patterns, or deciding between tracking and no-tracking queries.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Entity Framework Core

Guidance on Entity Framework Core, the object-relational mapper for .NET. Organized by task, not by
EF Core version — the core API surface (`DbContext`, `DbSet<T>`, `SaveChanges`) has been stable
across recent major versions; each reference file notes a version-introduced fact inline where it
matters.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Defining a `DbContext`, declaring `DbSet<T>` properties, choosing `OnConfiguring` vs. `AddDbContext`/`AddDbContextFactory` for DI | [references/core-concepts.md](references/core-concepts.md) |
| Wiring up SQL Server, Npgsql (PostgreSQL), or SQLite as the provider | [references/provider-configuration.md](references/provider-configuration.md) |
| Creating, applying, or troubleshooting a migration; understanding the model snapshot | [references/migrations.md](references/migrations.md) |
| Understanding entity states, when `SaveChanges` issues which SQL, or debugging a tracking-related exception | [references/change-tracking-and-savechanges.md](references/change-tracking-and-savechanges.md) |
| Writing a LINQ query, deciding when to `Include`/`ThenInclude`, choosing a single vs. split query, or turning off tracking for a read-only query | [references/querying.md](references/querying.md) |
| Configuring relationships (one-to-many, many-to-many, owned types) with the fluent API or data annotations | [references/relationships-and-configuration.md](references/relationships-and-configuration.md) |
| Compiling a hot-path query, reasoning about query splitting cost, or tuning tracking behavior for throughput | [references/performance.md](references/performance.md) |
| Testing code that depends on a `DbContext` | [references/testing.md](references/testing.md) |

## Quick start

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasKey(o => o.Id);
    }
}

// Program.cs — DI registration, not OnConfiguring
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Usage
var order = await dbContext.Orders
    .AsNoTracking()
    .FirstOrDefaultAsync(o => o.Id == orderId);
```

The single most common miss: querying with the default tracking behavior for a read-only view (a
GET endpoint, a report) instead of `AsNoTracking()`, which pays the change-tracker snapshot cost for
data that's never saved back. See [references/querying.md](references/querying.md) and
[references/performance.md](references/performance.md).

## Out of scope

- The underlying database engines' own SQL dialects and administration — this skill covers EF Core's
  provider configuration surface, not database server operation.
- Non-relational EF Core providers (e.g. Cosmos DB) — the provider-configuration guidance here
  covers SQL Server, Npgsql/PostgreSQL, and SQLite only.
- Object-to-object mapping between entities and DTOs — out of scope; this skill's querying guidance
  stops at the shape EF Core itself returns.
