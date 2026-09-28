# Dapper

Dapper is the micro-ORM that adds mapping-aware query methods directly onto `IDbConnection`. This
skill covers writing and parameterizing Dapper queries, mapping multi-table joins, calling stored
procedures, and avoiding its common performance and SQL-injection pitfalls.

## When to reach for it

- Writing a query with `Query`/`QueryAsync`/`QueryFirstOrDefault`/`Execute` and deciding how the
  connection should open and close around it.
- Mapping the result of a join across two or more types, or pulling multiple result sets out of one
  command.
- Passing parameters into a query, especially when tempted to concatenate a value into the SQL
  string instead.
- Calling a stored procedure or wrapping a sequence of Dapper calls in a transaction.
- Deciding whether a query should buffer its results or stream them, or diagnosing a slow query path.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as reviewing a
Dapper query or debugging a parameterization bug. You can also invoke it directly as
`/dotnet-dapper`.

## What it covers

| Topic | Reference |
| --- | --- |
| Query/QueryAsync/Execute and connection lifecycle | [references/core-concepts.md](references/core-concepts.md) |
| Anonymous-object and DynamicParameters, output and table-valued parameters | [references/parameterization.md](references/parameterization.md) |
| Mapping joins across types and reading multiple result sets | [references/multi-mapping-and-multiple-results.md](references/multi-mapping-and-multiple-results.md) |
| Stored procedures and transactions | [references/stored-procedures-and-transactions.md](references/stored-procedures-and-transactions.md) |
| Buffered vs. unbuffered queries, SQL-injection and performance pitfalls | [references/buffered-queries-and-performance.md](references/buffered-queries-and-performance.md) |
| Testing code that calls Dapper | [references/testing.md](references/testing.md) |

## Example prompts

- "Write a Dapper query that joins orders to customers and maps the result to an OrderSummary."
- "Is it safe to build this WHERE clause by concatenating the search term into the SQL string?"
- "Call this stored procedure with Dapper and wrap it in a transaction."
