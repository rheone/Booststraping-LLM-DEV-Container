# Respawn

Guidance on Respawn, a third-party library for resetting test databases between integration tests —
the routing table (by situation, not by Respawn version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per Respawn version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `Respawner.CreateAsync`, `RespawnerOptions`, `ResetAsync` |
| `table-schema-exclusion.md` | `TablesToIgnore`, `SchemasToInclude`, `SchemasToExclude`, `WithReseed` |
| `supported-providers.md` | `DbAdapter` for SQL Server, PostgreSQL, MySQL, Oracle, and Informix |
| `fixture-integration-pattern.md` | A generic checkpoint-per-run pattern for test suite setup/teardown |
| `performance-considerations.md` | Table-set scoping, reseed cost, connection reuse, checkpoint caching for large schemas |
| `testing.md` | Verifying reset configuration clears/preserves the right tables, testing across supported providers |

## Scope

Respawn's checkpoint-based database reset API (`Respawn` package): creating a checkpoint, scoping
it to specific tables/schemas, and resetting state between tests against a real, reachable database.
Out of scope: provisioning/containerizing the database itself, schema migration tooling, and
transactional-rollback-based test isolation — see [SKILL.md](SKILL.md#out-of-scope) for the full
list.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing: 7.0.0.
