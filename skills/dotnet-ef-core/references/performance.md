# Performance

## Compiled queries

`EF.CompileQuery` (or `EF.CompileAsyncQuery`) pre-compiles a query's expression-tree-to-SQL
translation once, then reuses the compiled delegate on every call instead of re-running LINQ
translation each time:

```csharp
private static readonly Func<AppDbContext, int, Task<Order?>> GetOrderById =
    EF.CompileAsyncQuery((AppDbContext context, int id) =>
        context.Orders.FirstOrDefault(o => o.Id == id));

var order = await GetOrderById(dbContext, orderId);
```

The translation EF Core performs internally is already cached per unique query shape in current
versions, so `EF.CompileQuery`'s benefit is narrower than it once was — it mainly pays off for a
query executed extremely frequently (a hot path called per-request at high volume) where even the
cache lookup and parameter binding overhead is measurable. Reach for it only after profiling shows
query translation/setup cost, not preemptively on every query in the codebase.

## Query splitting cost

`AsSplitQuery()` (see [querying.md](querying.md)) trades a single query's cross-product row
multiplication for multiple database round trips. On a low-latency connection to the database (same
data center, same host) the extra round trips are cheap relative to the row multiplication they
avoid; on a high-latency connection (a database reached across a slower network hop) the added round
trips can outweigh the savings. Measure with your actual data volumes and network topology rather
than defaulting one way for every query with more than one `Include`.

## Tracking behavior

Tracking a query result costs a snapshot copy and change-tracker bookkeeping per entity, paid
whether or not you ever mutate the result. For any query whose result is never written back through
this context — a read-only API response, a report, a page of search results — `AsNoTracking()`
removes that cost entirely (see [querying.md](querying.md)). For a query that loads a large number
of entities you genuinely intend to mutate and save, tracking is the correct default; the
optimization is specifically about identifying the read-only paths and marking them, not disabling
tracking globally.

`ChangeTracker.QueryTrackingBehavior` can be set to `NoTracking` at the context level as a default,
with `AsTracking()` opting individual queries back in — a defensible choice for a `DbContext` used
almost exclusively for reads (a reporting context), inverted from the framework default.

## Avoiding accidental client-side evaluation

A LINQ operator EF Core's provider can't translate causes either a translation exception (current
EF Core versions, for most cases) or — for the narrower set of operations still allowed to evaluate
client-side — a silent partial evaluation where filtering happens in memory after the untranslatable
part. Either way, the fix is the same: move the untranslatable expression after a `ToList`/
`AsEnumerable` boundary deliberately (accepting the cost) or rewrite it into a form the provider can
translate (a computed column, a different LINQ operator, a raw SQL fragment via `FromSqlInterpolated`
for the specific query).

## Batching

`SaveChanges` batches multiple `INSERT`/`UPDATE`/`DELETE` statements from one call into as few round
trips as the provider supports rather than one round trip per statement — this is automatic and not
something you configure per query; the actionable performance lever on your side is how many
changes you accumulate before calling `SaveChanges` (a single `SaveChanges` after a batch of changes
outperforms calling it once per change in a loop).
