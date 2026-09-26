# Core Concepts

## `Respawner.CreateAsync`

You create a `Respawner` once per test run (or once per test-suite process, depending on your
fixture scope — see [fixture-integration-pattern.md](fixture-integration-pattern.md)) by pointing it
at an open connection or connection string and a `RespawnerOptions`:

```csharp
var respawner = await Respawner.CreateAsync(connectionString, new RespawnerOptions
{
    DbAdapter = DbAdapter.SqlServer,
});
```

`CreateAsync` inspects the target database's schema — every table, its columns, and its foreign key
relationships — and builds an internal deletion plan ordered so that dependent rows are deleted
before the rows they reference. This inspection is the expensive part of using Respawn, which is
why you build one `Respawner` and reuse it across every test rather than recreating it before each
test — see [performance-considerations.md](performance-considerations.md) for the cost breakdown.

## `RespawnerOptions`

The options object controls what `CreateAsync` includes in its plan:

| Property | Controls |
| --- | --- |
| `DbAdapter` | Which database engine's SQL dialect and system-catalog queries to use — see [supported-providers.md](supported-providers.md) |
| `TablesToIgnore` | Tables excluded from the deletion plan entirely — see [table-schema-exclusion.md](table-schema-exclusion.md) |
| `SchemasToInclude` / `SchemasToExclude` | Schema-level inclusion/exclusion, applied before table-level filtering |
| `WithReseed` | Whether to reset identity/auto-increment columns back to their seed value after deleting rows |
| `CheckTemporalTables` | Whether to detect and correctly handle SQL Server temporal (system-versioned) tables during reset |

## `ResetAsync`

Once you have a `Respawner`, you call `ResetAsync(connectionString)` (or an overload accepting an
open `DbConnection`) before or between tests to actually delete the data:

```csharp
await respawner.ResetAsync(connectionString);
```

`ResetAsync` executes the deletion plan built at `CreateAsync` time — it does not re-inspect the
schema, so it stays fast on repeated calls as long as the schema itself hasn't changed since the
`Respawner` was created. If your test suite runs a migration mid-suite (uncommon, but possible in a
suite that tests migrations themselves), create a new `Respawner` afterward rather than continuing
to use one built against the old schema shape.

## What gets reset, precisely

Respawn deletes rows; it does not drop tables, alter schema, or run `TRUNCATE` (delete order matters
because Respawn respects foreign keys, whereas `TRUNCATE` cannot run against a table with
incoming foreign key references without first disabling constraints). The end state after
`ResetAsync` is every included table empty, with identity columns reset only if `WithReseed` is set
and the provider supports it.
