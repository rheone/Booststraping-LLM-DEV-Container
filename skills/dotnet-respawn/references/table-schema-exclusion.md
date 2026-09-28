# Table and Schema Exclusion

## `TablesToIgnore`

Lists tables Respawn excludes from its deletion plan entirely — rows in these tables survive every
`ResetAsync` call. Use this for reference/lookup data, seed data, and framework-managed tables that
your tests depend on existing and don't want re-seeded on every reset:

```csharp
var respawner = await Respawner.CreateAsync(connectionString, new RespawnerOptions
{
    DbAdapter = DbAdapter.SqlServer,
    TablesToIgnore = new Table[]
    {
        "__EFMigrationsHistory",
        "Roles",
        "Countries",
    },
});
```

An ignored table is also excluded from the *dependency analysis* Respawn performs on every other
table — Respawn does not try to delete rows that reference an ignored table's primary key, since it
assumes those rows are meant to persist. If a table you didn't ignore has a foreign key pointing at
an ignored table, verify that's the relationship you intend; Respawn treats the ignored table as
stable ground truth for that FK.

`Table` supports schema-qualified names (`new Table("dbo", "Roles")`) for databases where an
unqualified table name is ambiguous across schemas.

## `SchemasToInclude` / `SchemasToExclude`

Schema-level filtering, applied before any table-level filtering:

```csharp
var respawner = await Respawner.CreateAsync(connectionString, new RespawnerOptions
{
    DbAdapter = DbAdapter.Postgres,
    SchemasToInclude = new[] { "public" },
});
```

`SchemasToInclude` scopes the entire deletion plan to only the listed schemas — every other schema
is invisible to Respawn, as if it didn't exist. `SchemasToExclude` does the opposite: every schema
except the listed ones is included. Set only one of the two; they express the same intent from
opposite directions, and using both together on the same options instance is a configuration
mistake rather than a way to combine allow/deny lists.

Schema scoping is the right first move on a database that hosts more than one application's tables
(a shared instance with a schema per service) — it keeps Respawn from ever considering, let alone
deleting, another service's data.

## `WithReseed`

When `true`, Respawn resets identity/auto-increment columns back to their seed value as part of the
reset, so the next inserted row in a freshly-reset table gets the same ID a test expects from a
clean database (e.g. the first inserted order always getting ID `1`). Support for this varies by
provider — verify it against the specific `DbAdapter` you're using (see
[supported-providers.md](supported-providers.md)) before relying on predictable IDs in assertions,
since not every provider's reseed mechanism behaves identically.

Leaving `WithReseed` off (the default) is cheaper per reset and is the right choice whenever tests
assert on data by a stable natural key or a freshly-queried ID rather than assuming a specific
numeric value.

## Choosing between ignoring a table and excluding a schema

Reach for `SchemasToInclude`/`SchemasToExclude` when the boundary is architectural (a shared
database, a separate reporting schema you never touch in tests). Reach for `TablesToIgnore` when the
boundary is about specific reference data within the schema your tests actually exercise. Combining
both is normal: scope to your application's schema first, then ignore the handful of reference
tables inside it that should survive every reset.
