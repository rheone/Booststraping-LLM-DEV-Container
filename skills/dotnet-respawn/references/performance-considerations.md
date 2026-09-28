# Performance Considerations

## Where the cost actually is

`Respawner.CreateAsync` is the expensive operation — it queries the target database's system
catalog for every table, column, and foreign key relationship in scope, then computes a deletion
order that respects those dependencies. On a schema with hundreds of tables and a dense foreign key
graph, this inspection can take a noticeably longer time than any single `ResetAsync` call.
`ResetAsync` itself, once the plan exists, is comparatively cheap: it executes a predetermined set
of delete statements against tables that already have the correct order computed.

The practical consequence: **create one `Respawner` and reuse it for the entire test run** (or per
fixture scope, per [fixture-integration-pattern.md](fixture-integration-pattern.md)) rather than
calling `CreateAsync` before every individual test. Recreating it per test pays the full schema
inspection cost on every single test, which on a large schema can dominate total suite run time far
more than the actual data deletion does.

## Scoping to reduce inspection cost

`SchemasToInclude` and `TablesToIgnore` (see
[table-schema-exclusion.md](table-schema-exclusion.md)) don't just change what gets deleted — they
also shrink what `CreateAsync` has to inspect and compute dependency ordering for. On a database
with a large schema outside your application's own tables (a shared instance, a reporting schema,
an audit-log schema with no foreign keys back into your application data), scoping to only the
schemas or tables your tests actually touch reduces both the inspection cost and the per-reset
delete cost.

## `WithReseed` cost

Resetting identity/auto-increment columns is an additional operation per table, on top of the
delete itself, and its cost scales with the number of tables in scope rather than the number of
rows. If your tests don't assert on specific numeric IDs, leaving `WithReseed` off avoids this cost
entirely — see [table-schema-exclusion.md](table-schema-exclusion.md#withreseed) for when it's worth
paying for.

## Connection reuse

Opening a new database connection has its own latency, separate from Respawn's own work. Reuse the
same connection (or a pooled connection string) across `CreateAsync` and every subsequent
`ResetAsync` call rather than opening a fresh connection object for each — most ADO.NET providers
pool connections by connection string automatically, but confirm your test setup isn't defeating
that pooling (e.g. by varying the connection string per test, which most pooling implementations key
on exactly).

## When per-test reset itself becomes the bottleneck

If `ResetAsync` itself — not `CreateAsync` — dominates suite run time even after scoping tables and
schemas tightly, that's usually a sign the schema has more tables in scope than a given test class
actually needs, rather than a sign Respawn itself is slow: revisit whether every test class sharing
one fixture actually needs the full scope that fixture resets, or whether some test classes would be
better served by a narrower-scoped `Respawner` targeting only the tables they touch.
