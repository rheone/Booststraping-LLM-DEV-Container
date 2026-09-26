# Bulk Operations, Stateless Sessions, and Multi-Query

## "Batching" means three different things here — don't conflate them

This word gets overloaded across NHibernate documentation (and across this skill's other files). Before reading further, know which one applies to your situation:

| Term | What it actually batches | Where it's configured | Covered in |
|---|---|---|---|
| Collection/proxy **batch fetching** (`BatchSize(n)` on a mapping) | Multiple lazy proxies' worth of a relationship into one `IN (...)` SELECT | Per-collection/entity in the Fluent mapping | `lazy-loading-and-fetching.md` |
| ADO.NET **statement batching** (`adonet.batch_size`) | Multiple INSERT/UPDATE/DELETE statements into fewer round trips at flush time | Session factory configuration | This file, below |
| **Future/multi-query batching** (`.ToFuture()`) | Multiple independent SELECT queries into one round trip | Per-query, at the call site | `bulk-and-stateless.md` (this file) § Multi-query / futures |

All three are legitimate and often used together in the same codebase — but they solve different problems, and "we already do batching" is not a meaningful statement without saying which one.

## `StatelessSession`

Use for bulk import/export/update jobs — no first-level cache, no automatic dirty checking, no cascades, no lazy loading of proxies the way a regular session has them. This means:

- **Much lower memory overhead** for processing large numbers of rows (no identity map growing unbounded).
- **No cascades** — if the bulk operation needs related rows touched, do it explicitly; don't assume a `StatelessSession.Insert(entity)` cascades to `entity.Children` the way a regular session's cascade config would.
- **No filters applied** (see `filters-and-interceptors.md`) — soft-delete/tenant filters must be applied manually in any query run through a stateless session.
- **No interceptors fire** — audit-column auto-population via `IInterceptor` (see `filters-and-interceptors.md`) won't happen; set those fields explicitly in bulk code.

```csharp
using var statelessSession = sessionFactory.OpenStatelessSession();
using var tx = statelessSession.BeginTransaction();
foreach (var record in importBatch)
{
    var entity = MapToEntity(record); // set CreatedAtUtc etc. explicitly — no interceptor will do it here
    statelessSession.Insert(entity);
}
tx.Commit();
```

## ADO.NET statement batching

Set `adonet.batch_size` in session factory configuration to batch multiple statements into fewer round trips. Check that the identifier generation strategy is batching-compatible — `Identity` columns force a round trip per insert to retrieve the generated ID, which defeats batching; `HiLo` doesn't have this problem (see `mapping-conventions.md` § Identifier generation).

**Flush ordering matters for whether batching actually engages.** NHibernate groups pending statements by entity type at flush time to batch them — inserting entities of type A, then B, then A again in the same unit of work does not batch the two A-inserts together the way inserting all A's followed by all B's would, because the second group of A-statements is no longer adjacent to the first once grouped by the order operations were queued. If a bulk operation processes a mixed list of entity types in an interleaved order and batching doesn't seem to be engaging (check via `sql-diagnostics.md`), consider grouping saves by type before flushing, rather than assuming `adonet.batch_size` alone guarantees batching regardless of call order.

## Multi-query / futures

When a single unit of work needs several independent query results (e.g. loading a dashboard's several unrelated summary numbers), batch them into one round trip with `.ToFuture()`/`.ToFutureValue()` instead of issuing them as separate sequential queries:

```csharp
var pendingOrders = session.Query<Order>().Where(o => o.Status == OrderStatus.Pending).ToFuture();
var totalRevenue = session.Query<Order>().Where(o => o.Status == OrderStatus.Complete).Select(o => o.Total).Sum().ToFutureValue();
// Both execute in a single round trip on first enumeration of either
var orders = pendingOrders.ToList();
var revenue = totalRevenue.Value;
```

This is an easy, low-risk perf win for read-heavy composite views — worth defaulting to whenever a method issues more than one or two independent queries in sequence with no dependency between them.

### Futures combined with projections (`AliasToBean` + `Future`)

Futures aren't limited to full-entity queries — they compose with QueryOver's projection/result-transformer pipeline too, which is the more common real-world shape for a dashboard or report endpoint that needs several *different DTO-shaped* results in one round trip rather than several full entity graphs:

```csharp
Order orderAlias = null;
OrderSummaryDto summaryAlias = null;

var pendingSummaries = session.QueryOver(() => orderAlias)
    .Where(() => orderAlias.Status == OrderStatus.Pending)
    .SelectList(list => list
        .Select(() => orderAlias.Id).WithAlias(() => summaryAlias.Id)
        .Select(() => orderAlias.OrderNumber).WithAlias(() => summaryAlias.OrderNumber))
    .TransformUsing(Transformers.AliasToBean<OrderSummaryDto>())
    .Future<OrderSummaryDto>();

var revenueTotal = session.QueryOver<Order>()
    .Where(o => o.Status == OrderStatus.Complete)
    .Select(Projections.Sum<Order>(o => o.Total))
    .FutureValue<decimal>();

// Single round trip on first enumeration of either — same batching benefit as the simple case above
var summaries = pendingSummaries.ToList();
var revenue = revenueTotal.Value;
```

**Why this matters over the simple full-entity future example above**: this is the shape that actually shows up in report/dashboard endpoints, where you want several *differently shaped* projected results, not several full entity lists — and it's exactly the combination (`AliasToBean` result transformer + `Future`) that's easy to miss if you only know the two techniques separately. See `templates/queryover-projection.cs` for the standalone `AliasToBean` pattern without futures.

**Caveat**: every future in the same batch executes together on first enumeration of *any* of them — if one future's query depends on a value only known after another future's result is read, that dependency breaks the batching (NHibernate has no way to sequence within one batch) and you should just issue them as two ordinary sequential queries instead of forcing an artificial future batch.
