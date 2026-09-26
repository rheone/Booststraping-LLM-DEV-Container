# Multi-Mapping and Multiple Result Sets

## Multi-mapping a join

When a query joins tables and you want the result mapped into more than one related object (e.g. an
`Order` with its `Customer` attached) rather than one flat type, pass a mapping function and tell
Dapper where one mapped type's columns end and the next one's begin:

```csharp
var sql = @"
    SELECT o.Id, o.Total, c.Id, c.Name
    FROM Orders o
    JOIN Customers c ON c.Id = o.CustomerId";

var orders = await connection.QueryAsync<Order, Customer, Order>(
    sql,
    (order, customer) =>
    {
        order.Customer = customer;
        return order;
    },
    splitOn: "Id");
```

`splitOn` names the column where Dapper stops mapping columns to the first type (`Order`) and starts
mapping the rest to the next type (`Customer`) — it splits on the first occurrence of a column with
that name after the first type's columns, so a query joining more than two types with a repeated
column name (typically `Id`) needs `splitOn` to list each split point in order:
`splitOn: "Id,Id"` for a three-type map. Get the column order in the `SELECT` list and the type
order in the generic arguments consistent with each other and with `splitOn` — a mismatch produces
values silently mapped into the wrong properties rather than an exception, since Dapper is mapping
positionally within each split segment.

## QueryMultiple

For unrelated result sets from one round trip (a batch of separate `SELECT` statements, or a stored
procedure that returns more than one result set), `QueryMultiple` gives you a grid reader you pull
each result set from in order:

```csharp
using var multi = await connection.QueryMultipleAsync(
    "SELECT * FROM Orders WHERE CustomerId = @customerId; " +
    "SELECT * FROM Customers WHERE Id = @customerId;",
    new { customerId });

var orders = (await multi.ReadAsync<Order>()).ToList();
var customer = await multi.ReadFirstOrDefaultAsync<Customer>();
```

You must read the result sets in the same order the SQL produces them — `QueryMultiple` doesn't let
you skip ahead or re-read a set once you've moved past it. This is the pattern for a single round
trip that needs to return a parent entity plus one or more related collections without the row
multiplication a multi-mapped `JOIN` would produce for unrelated shapes.

## Choosing between the two

Multi-mapping is for one result set whose rows are already joined and need splitting into related
objects. `QueryMultiple` is for genuinely separate queries (potentially against unrelated tables,
with no join between them) that you want executed together as one round trip. Reaching for
multi-mapping on unrelated queries forces an unnecessary `JOIN`; reaching for `QueryMultiple` on a
single already-joined result set means writing the query twice instead of once.
