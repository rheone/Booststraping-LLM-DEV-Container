---
name: dotnet-respawn
description: Guidance on Respawn, a third-party library for resetting test databases between integration tests in C#/.NET (current stable release 7.0.0). Covers Respawner.CreateAsync/RespawnerOptions checkpoint configuration, table/schema inclusion/exclusion (TablesToIgnore, SchemasToInclude/SchemasToExclude), supported database providers (SQL Server, PostgreSQL, MySQL, Oracle, Informix) via DbAdapter, a generic fixture-integration pattern for resetting state between tests, and performance considerations for large schemas (reseeding cost, table-set scoping, connection reuse). Use when writing, reviewing, or debugging integration test infrastructure that needs a clean, deterministic database state before or between test runs against a real or containerized database.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Respawn

Guidance on Respawn, a third-party library for resetting test databases between integration tests.
Current stable release as of this writing: **7.0.0**. Organized by concern/topic, not by Respawn version — its core
`Respawner.CreateAsync`/`RespawnerOptions` API has been stable across its recent major lines, so
each reference file notes a version-introduced fact inline rather than splitting files by version
tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Setting up a checkpoint for the first time | `Respawner.CreateAsync`, `RespawnerOptions`, `ResetAsync` | [references/core-concepts.md](references/core-concepts.md) |
| Keeping reference/lookup data or a schema out of the reset | `TablesToIgnore`, `SchemasToInclude`, `SchemasToExclude`, `WithReseed` | [references/table-schema-exclusion.md](references/table-schema-exclusion.md) |
| Confirming Respawn supports your database engine | `DbAdapter` (SQL Server, PostgreSQL, MySQL, Oracle, Informix) | [references/supported-providers.md](references/supported-providers.md) |
| Wiring a reset into your test suite's setup/teardown | A generic checkpoint-per-run fixture pattern | [references/fixture-integration-pattern.md](references/fixture-integration-pattern.md) |
| Resets are slow against a large schema | Table-set scoping, reseed cost, connection reuse, checkpoint caching | [references/performance-considerations.md](references/performance-considerations.md) |
| Verifying your reset configuration actually clears/preserves the right tables | Asserting on post-reset row counts, testing across supported providers | [references/testing.md](references/testing.md) |

## Quick start

```csharp
var respawner = await Respawner.CreateAsync(connectionString, new RespawnerOptions
{
    DbAdapter = DbAdapter.SqlServer,
    TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
});

// Before or between each test:
await respawner.ResetAsync(connectionString);
```

`Respawner.CreateAsync` inspects the schema once and builds a deletion plan that respects foreign
key dependency order; `ResetAsync` then deletes all data outside the excluded tables/schemas on
every call, without needing to rebuild that plan each time.

## Out of scope

- Provisioning or containerizing the database itself (e.g. spinning up a database in a container
  for the test run) — Respawn resets an already-running, already-reachable database; it does not
  start or manage the database process.
- Schema migration tooling — Respawn assumes the schema it's resetting already matches what the
  code under test expects; applying migrations is a separate concern.
- Transactional-rollback-based test isolation (wrapping each test in an uncommitted transaction) —
  a different isolation strategy with its own tradeoffs, not a Respawn feature.
