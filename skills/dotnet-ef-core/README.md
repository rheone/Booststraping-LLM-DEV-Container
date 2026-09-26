# Entity Framework Core

Entity Framework Core is the object-relational mapper most .NET codebases reach for to talk to a
relational database. This skill covers configuring a `DbContext`, writing and troubleshooting LINQ
queries, managing migrations, and understanding when EF Core tracks entities versus when it doesn't.

## When to reach for it

- Deciding whether `OnConfiguring` or `AddDbContext`/`AddDbContextFactory` is the right way to wire
  up a `DbContext` in a given app.
- A LINQ query throws at runtime because part of it can't translate to SQL, or a query pulls back
  far more data than expected.
- Debugging why `SaveChanges` did (or didn't) persist a change, or why an entity's tracked state
  isn't what you expected.
- Adding, applying, or troubleshooting a migration, or reconciling the model snapshot with the
  actual database schema.
- A read-heavy endpoint feels slow and you're deciding between `AsNoTracking`, a compiled query, or
  a split query.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as reviewing a
`DbContext`, debugging a LINQ translation error, or authoring a migration. You can also invoke it
directly as `/dotnet-ef-core`.

## What it covers

| Topic | Reference |
| --- | --- |
| DbContext/DbSet setup, OnConfiguring vs. DI registration | [references/core-concepts.md](references/core-concepts.md) |
| Configuring SQL Server, Npgsql (PostgreSQL), or SQLite | [references/provider-configuration.md](references/provider-configuration.md) |
| Creating, applying, and troubleshooting migrations | [references/migrations.md](references/migrations.md) |
| Entity states and what SaveChanges does with them | [references/change-tracking-and-savechanges.md](references/change-tracking-and-savechanges.md) |
| Writing queries, Include/ThenInclude, split queries, AsNoTracking | [references/querying.md](references/querying.md) |
| Configuring relationships and owned types | [references/relationships-and-configuration.md](references/relationships-and-configuration.md) |
| Compiled queries and tracking-behavior tuning | [references/performance.md](references/performance.md) |
| Testing code that depends on a DbContext | [references/testing.md](references/testing.md) |

## Example prompts

- "Why is this LINQ query throwing an exception saying it can't be translated to SQL?"
- "Set up a DbContext for this ASP.NET Core project using SQL Server, registered through DI."
- "This report endpoint is slow: should I be using AsNoTracking here?"
