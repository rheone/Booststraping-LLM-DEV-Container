# Supported Providers

## `DbAdapter`

`RespawnerOptions.DbAdapter` tells Respawn which SQL dialect and system-catalog queries to use when
inspecting the schema and building its deletion plan. Respawn supports:

- `DbAdapter.SqlServer` — Microsoft SQL Server (and Azure SQL Database)
- `DbAdapter.Postgres` — PostgreSQL
- `DbAdapter.MySql` — MySQL (and MySQL-compatible engines)
- `DbAdapter.Oracle` — Oracle Database
- `DbAdapter.Informix` — IBM Informix

You set `DbAdapter` explicitly on `RespawnerOptions` rather than relying on Respawn to infer it from
the connection string or connection type:

```csharp
var respawner = await Respawner.CreateAsync(connectionString, new RespawnerOptions
{
    DbAdapter = DbAdapter.Postgres,
});
```

## Provider-specific behavior differences

Each `DbAdapter` implementation queries that engine's own system catalog for table, column, and
foreign key metadata, since there is no single ANSI-SQL-portable way to enumerate this across
engines. This means:

- **Identity/auto-increment reseeding** (`WithReseed`) uses each engine's own mechanism
  (`DBCC CHECKIDENT` on SQL Server, sequence manipulation on PostgreSQL, and so on) and its exact
  behavior — whether it resets to `0` or `1`, whether it requires elevated permissions — follows
  that engine's own semantics rather than a single Respawn-defined behavior.
- **Temporal table support** (`CheckTemporalTables`) is specific to SQL Server's system-versioned
  tables; it has no equivalent on the other supported engines.
- **Case sensitivity of table/schema names** in `TablesToIgnore`/`SchemasToInclude` follows the
  target engine's own identifier casing rules (PostgreSQL's default lowercase folding versus SQL
  Server's typically case-insensitive collation), which matters when a name you pass doesn't match
  what Respawn finds in the catalog.

## Connecting through a driver/provider package

Respawn itself takes a connection string or an open `DbConnection` — it does not bundle a specific
ADO.NET provider. You still reference the appropriate driver package for your database (e.g. a SQL
Server or PostgreSQL ADO.NET client) in the test project so a connection can be opened at all;
`DbAdapter` only controls Respawn's own SQL dialect choice, not how the connection itself is
established.

## Verifying support for your exact engine version

Provider support in Respawn tracks each engine's system-catalog shape, which can change across major
engine versions. If you're targeting an unusual or very new engine version, verify current support
against the package's own release notes rather than assuming every historical or upcoming version of
a listed engine behaves identically to the version most commonly tested against it.
