# Parameterization

## Anonymous objects

The common case: pass an anonymous object whose property names match the SQL's named parameters:

```csharp
var orders = await connection.QueryAsync<Order>(
    "SELECT Id, CustomerId, Total FROM Orders WHERE CustomerId = @customerId AND Total > @minTotal",
    new { customerId, minTotal = 100m });
```

Dapper matches `@customerId` and `@minTotal` in the SQL text to properties of the same name on the
object, regardless of the underlying provider's actual parameter-prefix convention — you always
write `@name` in the SQL text Dapper sees, and Dapper translates it to whatever the connection's
provider expects.

## DynamicParameters

Reach for `DynamicParameters` when you need parameters built up conditionally, need to specify an
explicit `DbType`/size (rather than letting Dapper infer it from the CLR value), or need output/
return-value parameters — none of which an anonymous object can express:

```csharp
var parameters = new DynamicParameters();
parameters.Add("CustomerId", customerId, DbType.Int32);
if (minTotal.HasValue)
{
    parameters.Add("MinTotal", minTotal.Value, DbType.Decimal);
}

var orders = await connection.QueryAsync<Order>(sql, parameters);
```

## Output parameters

```csharp
var parameters = new DynamicParameters();
parameters.Add("CustomerId", customerId);
parameters.Add("NewOrderId", dbType: DbType.Int32, direction: ParameterDirection.Output);

await connection.ExecuteAsync(
    "InsertOrder", parameters, commandType: CommandType.StoredProcedure);

int newOrderId = parameters.Get<int>("NewOrderId");
```

You read an output parameter's value from the same `DynamicParameters` instance after the command
executes, via `Get<T>(name)` — not from the `Execute`/`Query` call's own return value, which is
unrelated (affected row count for `Execute`, mapped rows for `Query`).

## Table-valued parameters

For SQL Server, passing a set of rows as a single parameter (avoiding either row-by-row calls or a
delimited-string workaround) uses `DataTable` shaped to match a SQL Server user-defined table type,
wrapped so Dapper sends it as a table-valued parameter:

```csharp
var table = new DataTable();
table.Columns.Add("Id", typeof(int));
foreach (var id in orderIds)
{
    table.Rows.Add(id);
}

var parameters = new DynamicParameters();
parameters.Add("OrderIds", table.AsTableValuedParameter("dbo.IntList"));

var orders = await connection.QueryAsync<Order>(
    "SELECT o.* FROM Orders o JOIN @OrderIds ids ON o.Id = ids.Id", parameters);
```

`AsTableValuedParameter` requires a matching user-defined table type to already exist in the target
database (`CREATE TYPE dbo.IntList AS TABLE (Id INT)`) — Dapper doesn't create it for you.

## The rule that prevents SQL injection

Every value that varies per call belongs in a parameter, never concatenated or interpolated into the
SQL text — including values you believe are "safe" (an internally generated ID, an enum's string
value). String interpolation into a SQL literal (`$"WHERE Id = {orderId}"`) is exactly the shape of
bug that turns into a SQL-injection vulnerability the moment the value's source changes upstream
(from a hard-coded constant to user input) without the query itself being revisited. The only
exception is a value that can't be parameterized at all because it's part of the SQL structure
itself (a table or column name chosen dynamically, an `ORDER BY` direction) — validate those against
an explicit allow-list of known-safe literal values before splicing them in, never pass them through
unchecked.
