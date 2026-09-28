# Second-Level Cache & Concurrency

## Second-level cache: when it's safe

The second-level cache is off by default and shared *across sessions* (unlike the always-on first-level cache scoped to one session). Enabling it for the wrong entity is a correctness bug, not just a perf tuning mistake.

| Entity characteristic | Cache? |
|---|---|
| Reference/lookup data, rarely changes, read constantly (statuses, country lists, product categories) | Yes — strong candidate, `NonstrictReadWrite` or `ReadOnly` cache concurrency strategy |
| Frequently updated by multiple concurrent users (orders, inventory counts) | No, or `ReadWrite` strategy at most with real understanding of the staleness window — default to not caching these |
| Anything where stale reads could cause a business-logic error (e.g. checking inventory before allowing a purchase) | No — read live |

Set this per-entity in the mapping (`Cache.ReadWrite()` etc. in Fluent) — don't flip on second-level caching globally without going through this table entity by entity.

## Query cache — a different cache from the one above

This is the single most common cache-related confusion on this stack: the **second-level entity cache** (above) and the **query cache** are two separate mechanisms that must both be understood on their own terms, and enabling one without the other usually does nothing useful.

- The **entity cache** stores individual entity instances by ID, keyed by type.
- The **query cache** stores the *result set* (specifically, the list of entity IDs and scalar values) of a specific query, keyed by the query plus its parameter values.

Enabling the query cache on a query (`.SetCacheable(true)` in Criteria/QueryOver, `.Cacheable()` via LINQ extension) without also having the entity cache enabled for the entities that query returns means the query cache hit still has to go back to the database (or the first-level cache) to actually materialize each entity by ID — you get the query-plan/filtering benefit but not the full round-trip savings you'd expect. For a query-cached query to actually save a round trip end-to-end, the entities it returns should generally also be second-level cached.

```csharp
var pendingOrders = session.QueryOver<Order>()
    .Where(o => o.Status == OrderStatus.Pending)
    .Cacheable() // opts this specific query into the query cache
    .List();
```

**Invalidation is coarser than people expect**: the query cache invalidates a cached query result whenever *any* row in a table the query touched changes — not just rows matching that specific query's filter. A query cache entry for "pending orders" gets invalidated by literally any write to the `Orders` table, including one that has nothing to do with pending status. On a frequently-written table, this means the query cache may show a very low hit rate in practice even when enabled correctly — profile before assuming it's helping (see `sql-diagnostics.md`), rather than treating "I turned it on" as the end of the story.

**When it's worth it**: queries against tables that are read far more often than written, where the query shape repeats frequently with the same or a small set of parameter values (e.g. a lookup-table-style query, or a dashboard query re-run identically by many users). Same shape of candidate as the second-level entity cache table above — if an entity isn't a good candidate for the entity cache, a query returning it usually isn't a good candidate for the query cache either.

## Optimistic concurrency (`StaleObjectStateException`)

Version columns (`Version(x => x.RowVersion)` in Fluent) enable optimistic locking — NHibernate checks the version on update and throws `StaleObjectStateException` if another transaction modified the row since this entity was loaded.

**This exception is not a bug to suppress — it's the concurrency control working as intended.** The correct handling depends on the situation:

- **User-facing edit conflict** (two people editing the same record): catch it, reload the current state, and surface a "this was changed since you loaded it" conflict to the user rather than silently overwriting or silently retrying with stale data.
- **Background job retry-safe operation**: catch it, reload, reapply the intended change, retry with a bounded retry count — but only if the operation is genuinely idempotent/safe to reapply against fresh state.
- **Never**: catch and discard, or catch and blindly re-save with the stale in-memory values — this defeats the entire point of the version check and reintroduces the lost-update problem it exists to prevent.

## Pessimistic locking

Use `LockMode.Upgrade`/`session.Get<T>(id, LockMode.Upgrade)` (or QueryOver's `.Lock()`) sparingly — it holds a DB-level lock for the duration of the transaction, which is a real throughput cost under load. Prefer optimistic locking as the default; reach for pessimistic locking specifically for short, high-contention critical sections (e.g. decrementing a limited inventory count) where retry-on-conflict would be worse than blocking briefly.
