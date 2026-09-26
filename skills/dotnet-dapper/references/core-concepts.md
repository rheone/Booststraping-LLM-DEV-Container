# Core Concepts

## The extension methods

Dapper adds extension methods to `IDbConnection` — there is no Dapper-specific connection type or
context object. You construct an ordinary ADO.NET connection (`SqlConnection`, `NpgsqlConnection`,
`SqliteConnection`, etc.) and call Dapper's methods directly on it:

- **`Query<T>` / `QueryAsync<T>`** — runs a SQL statement and maps each row to `T`, returning all
  rows as a list.
- **`QueryFirstOrDefault<T>` / `QueryFirstOrDefaultAsync<T>`** — maps only the first row (or
  `default(T)` if there are none); `QuerySingleOrDefault`/`QuerySingleOrDefaultAsync` is the same but
  throws if more than one row comes back, when exactly zero-or-one is a correctness invariant, not
  just an optimization.
- **`Execute` / `ExecuteAsync`** — runs a statement that doesn't return rows (an `INSERT`, `UPDATE`,
  `DELETE`, or a stored procedure with only output parameters) and returns the affected row count.
- **`ExecuteScalar<T>` / `ExecuteScalarAsync<T>`** — runs a statement and returns a single scalar
  value (a `COUNT(*)`, a newly generated identity), converted to `T`.

```csharp
using var connection = new SqlConnection(connectionString);

var order = await connection.QueryFirstOrDefaultAsync<Order>(
    "SELECT Id, CustomerId, Total FROM Orders WHERE Id = @id", new { id = orderId });

var affected = await connection.ExecuteAsync(
    "UPDATE Orders SET Total = @total WHERE Id = @id", new { id = orderId, total });

var count = await connection.ExecuteScalarAsync<int>(
    "SELECT COUNT(*) FROM Orders WHERE CustomerId = @customerId", new { customerId });
```

Dapper maps columns to properties by name (case-insensitively) — a query's column list and the
target type's public settable properties need matching names, not matching order. A column with no
matching property is ignored; a non-nullable property with no matching column throws only if you're
mapping to a constructor parameter that requires it (see below), not automatically otherwise.

## Constructor mapping

Dapper maps to a parameterized constructor (including a C# record's primary constructor) when the
target type has one and no public parameterless constructor, matching constructor parameter names to
column names the same way it matches property names:

```csharp
public sealed record Order(int Id, int CustomerId, decimal Total);

var orders = await connection.QueryAsync<Order>("SELECT Id, CustomerId, Total FROM Orders");
```

## Connection lifecycle

**Dapper never owns, opens, or closes the connection for you.** Every extension method opens the
connection if it isn't already open, but whether you provide an already-open connection or a closed
one, the responsibility for eventually closing/disposing it is yours — Dapper's methods do not
dispose the `IDbConnection` you pass in. The common pattern is a `using` (or `await using`) block
scoped to the unit of work:

```csharp
await using var connection = new SqlConnection(connectionString);
var orders = await connection.QueryAsync<Order>(sql, parameters);
// connection disposed here
```

Passing an already-open connection to multiple Dapper calls in sequence (inside a service method,
across a transaction) reuses that one connection instead of opening a new one per call — the pattern
for grouping several statements that need to share a connection or a transaction (see
[stored-procedures-and-transactions.md](stored-procedures-and-transactions.md)).

Dapper does not pool connections itself — connection pooling is the underlying ADO.NET provider's
job (enabled by default for most providers' connection strings). Opening and disposing a connection
per unit of work is cheap specifically because the provider's pool recycles the underlying physical
connection rather than actually establishing a new one each time.
