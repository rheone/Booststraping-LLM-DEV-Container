# Dapper

Task-organized guidance on Dapper — the routing table (by task, not Dapper version) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per version

| File | Covers |
| --- | --- |
| `core-concepts.md` | Query, QueryAsync, QueryFirstOrDefault, Execute, ExecuteScalar; connection lifecycle |
| `parameterization.md` | anonymous-object parameters, DynamicParameters, output parameters, table-valued parameters |
| `multi-mapping-and-multiple-results.md` | multi-mapping a join across types, splitOn, QueryMultiple for multiple result sets |
| `stored-procedures-and-transactions.md` | CommandType.StoredProcedure, IDbTransaction |
| `buffered-queries-and-performance.md` | buffered vs. unbuffered (streaming) queries, SQL-injection pitfalls, query-plan-cache considerations |
| `testing.md` | Testing code that calls Dapper |

## Scope

Dapper (the `Dapper` NuGet package) as an extension-method layer over `IDbConnection`. Out of scope:
any specific ADO.NET provider's own connection setup, and schema migration/change-tracking, which
Dapper does not provide.

Each reference file notes a Dapper version fact inline where relevant; version is not the
file-splitting axis for this skill (see SKILL.md for why).

## Verified facts (as of 2026-09-26)

- **Current latest release: Dapper 2.1.89** (published September 23, 2026). Source: the NuGet
  Gallery package page (nuget.org/packages/dapper).
- **License: Apache-2.0.** Source: the NuGet Gallery package page's license metadata.

These facts were verified via live web search against nuget.org at the time this skill was written;
re-verify before relying on the exact version number.
