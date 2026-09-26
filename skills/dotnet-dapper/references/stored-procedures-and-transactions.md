# Stored Procedures and Transactions

## Calling a stored procedure

Pass the procedure name as the SQL text and set `commandType: CommandType.StoredProcedure` — without
it, Dapper (and the underlying ADO.NET command) treats the text as a literal SQL statement, not a
procedure name, and the call fails:

```csharp
var parameters = new { CustomerId = customerId, Status = "Pending" };

var orders = await connection.QueryAsync<Order>(
    "GetOrdersByCustomerAndStatus", parameters, commandType: CommandType.StoredProcedure);
```

Output parameters and return values from a stored procedure need `DynamicParameters` rather than an
anonymous object, since an anonymous object has no way to receive a value back after the call (see
[parameterization.md](parameterization.md)).

## Transactions

Dapper doesn't manage transactions itself — you begin one on the connection through ordinary ADO.NET
(`IDbTransaction`) and pass it explicitly to every Dapper call that must participate:

```csharp
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    await connection.ExecuteAsync(
        "UPDATE Accounts SET Balance = Balance - @amount WHERE Id = @fromId",
        new { amount, fromId }, transaction);

    await connection.ExecuteAsync(
        "UPDATE Accounts SET Balance = Balance + @amount WHERE Id = @toId",
        new { amount, toId }, transaction);

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

Every Dapper call meant to be part of the transaction must receive the same `transaction` argument
explicitly — there's no ambient transaction Dapper picks up automatically from the connection. A
call that omits the `transaction` parameter while the connection has an open transaction throws
(most ADO.NET providers reject an untransacted command against a connection with a pending
transaction), which is a useful fail-fast signal that a call was left out of the transaction by
mistake rather than a call silently escaping it.

For a transaction spanning an externally provided `System.Transactions.TransactionScope` instead of
an explicit `IDbTransaction`, you don't pass a `transaction` argument to Dapper calls at all — the
ambient scope enlists the connection automatically at the ADO.NET level, below where Dapper operates.
