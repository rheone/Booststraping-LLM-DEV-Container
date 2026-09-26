# Migrations

## Creating a migration

From the CLI (the `dotnet-ef` global tool):

```bash
dotnet ef migrations add AddOrderStatus
```

From the Visual Studio Package Manager Console (the PowerShell cmdlet form some guidance still
shows): `Add-Migration AddOrderStatus`. Both drive the same underlying migrations infrastructure;
the CLI form works in any terminal and does not require Visual Studio.

Each migration is a generated C# class with `Up` and `Down` methods describing the schema
transition, plus a `.Designer.cs` file capturing that migration's snapshot of the model at the time
it was created. Review the generated `Up`/`Down` code before applying it — EF Core's diff against
the previous snapshot is usually correct, but a renamed property that should have been a column
rename can come out as a drop-and-add pair that would silently discard data on a real database.

## Applying a migration

```bash
dotnet ef database update
```

or `Update-Database` in the Package Manager Console. This applies every pending migration up to the
latest, in order, tracked via the `__EFMigrationsHistory` table EF Core creates in the target
database. Passing a specific migration name (`dotnet ef database update PreviousMigrationName`)
rolls the database forward or back to that named migration instead of always the latest — useful
for testing a `Down` method or reverting a bad deployment.

## The model snapshot

`<ContextName>ModelSnapshot.cs` (generated alongside your migrations) is EF Core's record of what
the model looked like after the last migration was created. Every `migrations add` diffs your
current `OnModelCreating` configuration against this snapshot, not against the live database schema
— the snapshot is the source of truth for "what has already been migrated," and the live database is
assumed to match it. If the live database has drifted from what the snapshot says (someone ran a
manual `ALTER TABLE`, or a migration was applied out of band), the next generated migration diffs
against the stale snapshot and produces a script that doesn't match what the database actually
needs.

Do not hand-edit the model snapshot file to "fix" a diff — treat a snapshot/database mismatch as a
signal to add a corrective migration instead, so the migration history stays the single audit trail
of every schema change.

## Generating a SQL script instead of applying directly

```bash
dotnet ef migrations script
```

produces the idempotent SQL for all migrations (or a specific range with `FromMigration` and
`ToMigration` arguments) without touching a live database — the mechanism for handing a DBA a
reviewable script, or for a deployment pipeline that applies schema changes as a separate step from
the application deploy.

## Multiple DbContexts in one project

When a project has more than one `DbContext`, pass `--context <ContextName>` (or `-Context` in the
Package Manager Console form) to every migrations command so the tooling knows which context's
migrations folder and model snapshot to operate on.
