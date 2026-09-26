---
name: dotnet-dapper
description: Guidance on Dapper (verified current release 2.1.89, Apache-2.0), the micro-ORM extension methods over IDbConnection — Query/QueryAsync/QueryFirstOrDefault/Execute/ExecuteScalar, connection lifecycle (Dapper never owns or opens the connection for you), parameterization with anonymous objects and DynamicParameters (including output and table-valued parameters), multi-mapping and multiple result sets (splitOn, QueryMultiple), stored procedures via CommandType.StoredProcedure, transactions via IDbTransaction, buffered vs. unbuffered queries, and SQL-injection/performance pitfalls. Use when writing or reviewing Dapper queries, mapping a multi-table join result, wiring a stored procedure call, or debugging a Dapper parameterization or connection-lifecycle bug.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Dapper

Guidance on Dapper, the micro-ORM that extends `IDbConnection` with mapping-aware query methods.
Organized by task, not by Dapper version — its extension-method surface has been stable across
recent releases; each reference file notes a version fact inline where it matters.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Calling `Query`/`QueryAsync`/`QueryFirstOrDefault`/`Execute`/`ExecuteScalar`, or managing when the connection opens/closes | [references/core-concepts.md](references/core-concepts.md) |
| Passing parameters safely (anonymous objects, `DynamicParameters`, output parameters, table-valued parameters) | [references/parameterization.md](references/parameterization.md) |
| Mapping a join across multiple types, or reading more than one result set from one command | [references/multi-mapping-and-multiple-results.md](references/multi-mapping-and-multiple-results.md) |
| Calling a stored procedure, or wrapping a call in a transaction | [references/stored-procedures-and-transactions.md](references/stored-procedures-and-transactions.md) |
| Deciding buffered vs. unbuffered reads, or diagnosing a performance/SQL-injection issue | [references/buffered-queries-and-performance.md](references/buffered-queries-and-performance.md) |
| Testing code that calls Dapper | [references/testing.md](references/testing.md) |

## Quick start

```csharp
using var connection = new SqlConnection(connectionString);

var orders = await connection.QueryAsync<Order>(
    "SELECT Id, CustomerId, Total FROM Orders WHERE CustomerId = @customerId",
    new { customerId });
```

The single most common miss: string-concatenating a value into the SQL text instead of passing it as
a parameter — the SQL-injection and query-plan-cache-bloat mistake Dapper's parameter support exists
specifically to avoid. See
[references/buffered-queries-and-performance.md](references/buffered-queries-and-performance.md)
and [references/parameterization.md](references/parameterization.md).

## Out of scope

- Any specific ADO.NET provider's connection string format or driver-level configuration — this
  skill covers what you do with an already-configured `IDbConnection`, not how to construct one for
  a particular database engine.
- Schema migrations and change tracking — Dapper has no migration or change-tracking machinery of
  its own; you execute whatever SQL you write, and this skill's scope stops at that boundary.
