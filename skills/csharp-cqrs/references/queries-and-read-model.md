# Queries and the Read Model

A query is a request for data with **no side effects** — issuing the same query twice must never
change what a third query sees. Once that guarantee holds, a query handler is free to take
shortcuts a command handler never can: skip the domain model, skip repository abstractions, and
shape the result exactly like the caller needs it.

## Shaping a query and its result

```csharp
public sealed record GetOrderSummaryQuery(Guid OrderId);
public sealed record OrderSummaryDto(Guid Id, string CustomerName, decimal Total, string Status);

public sealed record ListOrdersForCustomerQuery(string CustomerId, int Page, int PageSize);
public sealed record OrderListItemDto(Guid Id, DateOnly PlacedOn, decimal Total, string Status);
```

Name the result type for the view it serves (`OrderSummaryDto`, `OrderListItemDto`), not for the
underlying entity (`OrderDto`) — a query's output is a projection shaped for one specific caller's
needs, and different callers of the same underlying data legitimately want different projections
returned as different, independently-evolving types.

## The handler: bypass the domain model freely

```csharp
public sealed class GetOrderSummaryHandler(IDbConnection connection)
{
    public Task<OrderSummaryDto?> HandleAsync(GetOrderSummaryQuery query, CancellationToken ct) =>
        connection.QuerySingleOrDefaultAsync<OrderSummaryDto>(
            "SELECT o.Id, c.Name AS CustomerName, o.Total, o.Status " +
            "FROM Orders o JOIN Customers c ON c.Id = o.CustomerId WHERE o.Id = @OrderId",
            new { query.OrderId });
}
```

This handler never constructs an `Order` domain object — it queries straight into the DTO shape,
joining whatever tables the view actually needs. This is the core permission CQRS grants the read
side: a query handler answers "what does the caller need to see," which is a data-shaping problem,
not "is this a valid `Order`," which is the write side's problem entirely. Loading a full aggregate
graph just to read three of its fields for display is exactly the cost this bypass avoids.

An ORM-based projection reaches the same result without raw SQL:

```csharp
public Task<OrderSummaryDto?> HandleAsync(GetOrderSummaryQuery query, CancellationToken ct) =>
    dbContext.Orders
        .Where(o => o.Id == query.OrderId)
        .Select(o => new OrderSummaryDto(o.Id, o.Customer.Name, o.Total, o.Status))
        .FirstOrDefaultAsync(ct);
```

Projecting with `.Select()` directly into the DTO, rather than loading full `Order`/`Customer`
entities and mapping afterward, lets the query translate into a single SQL statement selecting only
the needed columns — the read side's performance profile benefits from exactly the same "narrow
projection" freedom whether reached with raw SQL or an ORM's query API.

## Paging, sorting, and filtering

Query parameters (page number, sort field, filter criteria) belong on the query object itself, not
bolted onto a shared "get all" method with optional parameters growing over time:

```csharp
public sealed record ListOrdersForCustomerQuery(string CustomerId, int Page, int PageSize, string? StatusFilter = null);
```

Each distinct read shape a UI needs is a legitimate reason for its own query type — resist the pull
toward one generic, heavily-parameterized query handler serving every screen; that reintroduces the
one-shared-model problem CQRS exists to avoid, just on the read side instead of the write side.

## What never belongs in a query handler

A query handler that also writes — updating a "last viewed" timestamp, incrementing a view counter,
lazily creating a missing record — breaks the no-side-effects guarantee the entire read side depends
on, and reintroduces exactly the ambiguity ("does calling this change anything?") CQRS's split is
meant to eliminate. If a read needs to trigger a side effect, that's a command, dispatched
separately from the query that happens to run alongside it.
