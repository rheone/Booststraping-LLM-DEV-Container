# Entity Framework Core

Task-organized guidance on Entity Framework Core — the routing table (by task, not EF Core version)
is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per version

| File | Covers |
| --- | --- |
| `core-concepts.md` | DbContext, DbSet\<T>, OnConfiguring vs. AddDbContext/AddDbContextFactory DI registration |
| `provider-configuration.md` | UseSqlServer, UseNpgsql (PostgreSQL), UseSqlite, connection string and provider package setup |
| `migrations.md` | Add-Migration / dotnet ef migrations add, Update-Database / dotnet ef database update, the model snapshot |
| `change-tracking-and-savechanges.md` | EntityState (Added/Modified/Deleted/Unchanged/Detached), ChangeTracker, SaveChanges/SaveChangesAsync |
| `querying.md` | LINQ-to-SQL translation, Include/ThenInclude, single vs. split queries, AsNoTracking/AsNoTrackingWithIdentityResolution |
| `relationships-and-configuration.md` | Fluent API (OnModelCreating) vs. data annotations, one-to-many/many-to-many, owned types (OwnsOne/OwnsMany) |
| `performance.md` | Compiled queries (EF.CompileQuery), query splitting cost tradeoffs, tracking vs. no-tracking throughput |
| `testing.md` | Testing code that depends on a DbContext |

## Scope

Entity Framework Core (the `Microsoft.EntityFrameworkCore` package family) targeting relational
providers: SQL Server, Npgsql/PostgreSQL, and SQLite. Out of scope: non-relational providers (e.g.
Cosmos DB), the database engines' own administration, and object-to-DTO mapping beyond what EF Core
itself returns from a query.

Each reference file notes an EF Core version fact inline where relevant; version is not the
file-splitting axis for this skill (see SKILL.md for why).

## Verified facts (as of 2026-09-26)

- **Current latest release: EF Core 10.0.12** (10.0 GA'd November 11, 2025), MIT-licensed. Source:
  the NuGet Gallery package page (nuget.org/packages/microsoft.entityframeworkcore) and the
  `dotnet/efcore` GitHub repository license.
- **Microsoft.EntityFrameworkCore.Sqlite**: current latest 10.0.12. Source: NuGet Gallery
  (nuget.org/packages/microsoft.entityframeworkcore.sqlite).
- **Npgsql.EntityFrameworkCore.PostgreSQL** (the community-maintained PostgreSQL provider): current
  latest 10.0.3, PostgreSQL License (a permissive license with no commercial tier). Source: NuGet
  Gallery (nuget.org/packages/npgsql.entityframeworkcore.postgresql).
- EF Core 11 is in pre-release (RC as of this writing) with a planned November 2026 GA; this skill
  documents the 10.0 GA surface.

These facts were verified via live web search against nuget.org and github.com at the time this
skill was written; re-verify before relying on exact version numbers.
