# Query Strategy: QueryOver, HQL, LINQ, Native SQL, Named Queries

NHibernate ships four query APIs. Picking consistently — rather than mixing freely per-developer-preference — matters for reviewability.

## Decision table

| Situation | Use |
|---|---|
| Standard CRUD-shaped queries, filters, joins expressible in LINQ | `session.Query<T>()` (LINQ provider) — default choice, most readable, best IDE support |
| Complex dynamic query built up conditionally across several branches | QueryOver — its fluent, strongly-typed builder composes more cleanly than building a LINQ expression tree by hand across conditionals |
| Truly dynamic query shape driven by runtime data (e.g. a generic search/filter endpoint with arbitrary field combinations) | QueryOver with `Restrictions.Conjunction()`/`Disjunction()`, or Criteria API for the most dynamic cases |
| Something the LINQ provider can't translate, or translates inefficiently (see pitfalls below) | HQL, written explicitly, with a comment explaining why LINQ wasn't used |
| Set-based bulk update/delete, vendor-specific SQL features, or a query that's already been hand-tuned by a DBA | Native SQL via `session.CreateSQLQuery(...)`, mapped back with `.AddEntity()`/`.SetResultTransformer()` as needed |

Don't default to native SQL for convenience — it bypasses the LINQ/HQL provider's parameterization safety net if not done carefully (see SQL injection note below) and loses the mapping layer's type safety. Reach for it when there's a concrete reason (performance, vendor-specific feature, pre-existing DBA-authored query), not as a first resort.

**SQL injection note**: any native SQL or HQL built via string concatenation from user input is a real vulnerability, not a theoretical one — always use parameterized queries (`.SetParameter(...)`), the same discipline as raw ADO.NET.

## NHibernate-specific LINQ extensions

`session.Query<T>()` returns an `IQueryable<T>`, but NHibernate adds extension methods (in the `NHibernate.Linq` namespace) beyond standard LINQ that don't exist on `IQueryable` from any other provider — easy to miss if your LINQ intuition comes from EF Core or LINQ-to-Objects, since these won't show up unless that namespace is imported:

| Extension | Purpose |
|---|---|
| `.ToFuture()` / `.ToFutureValue()` | Batch this query with others into one round trip — see `bulk-and-stateless.md` |
| `.Fetch(x => x.Nav)` / `.FetchMany(x => x.Collection)` | Eager-fetch a relationship for this query — see `lazy-loading-and-fetching.md` |
| `.ThenFetch(x => x.Nested)` | Chain a second-level eager fetch off a `.Fetch()`/`.FetchMany()` call, for a nested relationship (`Order` → `Customer` → `Address`) in one query |
| `.Cacheable()` | Opt this query into the query cache — see `caching-and-concurrency.md` § Query cache |
| `.CacheMode(CacheMode.X)` | Override cache read/write behavior for this specific query |
| `.Timeout(seconds)` | Per-query command timeout override, distinct from the connection-level timeout |

Importing `NHibernate.Linq` is what makes these available — if a query needs one of these and it's not resolving, check the `using` statement before assuming the feature doesn't exist in the installed version.

## LINQ provider translation pitfalls

Things that compile and work against `IEnumerable` (LINQ-to-Objects) but behave differently or throw against `session.Query<T>()`:

- **Client-side method calls inside the query expression** (custom C# methods, most string manipulation beyond the handful the provider translates) — throws at execution time, not compile time. If you see a custom method inside a `.Where(...)` against `session.Query<T>()`, that's a red flag to check whether it actually translates.
- **`GroupBy` combined with a projection that references non-grouped, non-aggregated columns** — SQL semantics require this to fail, but the error message is often unhelpful; if you see this pattern, rewrite to project only grouped/aggregate columns.
- **Outer joins expressed via conditional navigation** (`x.Optional != null ? x.Optional.Foo : null`) can translate to an inner join in some situations depending on provider version — if an outer join is required, prefer an explicit `.Fetch`/join construct or QueryOver's explicit join modes rather than relying on the LINQ provider to infer it.
- **Paging (`.Skip()`/`.Take()`) without an explicit `OrderBy`** — result order is undefined without one; this is an easy one to miss in code review since it "usually" returns consistent order until it doesn't (e.g. after an index change).

## Translating hand-written SQL to QueryOver/HQL

When asked to convert an existing SQL query, don't do a literal mechanical translation and assume it's equivalent — check each of these explicitly:

1. **Joins with filter conditions in the `ON` clause vs. the `WHERE` clause** — these are NOT equivalent for outer joins (filtering in `WHERE` turns a `LEFT JOIN` into an effective inner join by discarding NULL rows). Confirm which the original SQL intended before translating.
2. **`GROUP BY` with aggregates** — QueryOver's grouping API (`.SelectGroup(...)`) and HQL's `group by` need the projection list to match exactly; double check against the original `SELECT` list.
3. **`DISTINCT`** — NHibernate applies `DISTINCT` differently when eager-fetching a collection in the same query (needed to de-duplicate the cartesian product from a fetch join) — if the original SQL's `DISTINCT` was for a different reason, verify the translated query's distinct behavior actually matches.
4. **Subqueries** — correlated subqueries translate fine to QueryOver's `.WithSubquery`, but always verify against a real result set rather than assuming structural equivalence, since subquery correlation is one of the easier things to get subtly wrong in translation.

State explicitly in the PR/response which of the above you checked, rather than presenting a translated query as a drop-in equivalent without having verified it.

## Projections (avoiding full entity loads)

For read-heavy paths (especially ones feeding AutoMapper — see `lazy-loading-and-fetching.md`), project directly to the target shape instead of loading full entities:

```csharp
var results = session.Query<Order>()
    .Where(o => o.Status == OrderStatus.Pending)
    .Select(o => new OrderSummaryDto
    {
        Id = o.Id,
        CustomerName = o.Customer.Name, // pulled via a join in the generated SQL, not a lazy load — because it's in the projection, not touching a materialized proxy
        Total = o.LineItems.Sum(li => li.Price)
    })
    .ToList();
```

This sidesteps lazy-loading and proxy concerns entirely for the read path, since nothing is materialized as a tracked/proxied entity.

## Named Queries (hbm.xml and alternatives)

This team is Fluent-first. Stance on `.hbm.xml` named queries:

- **Don't introduce new `.hbm.xml` files.** If a named, reusable query is needed, prefer a well-named method on the repository wrapping a QueryOver/LINQ query, or (for something genuinely fixed and performance-critical) a constant HQL/SQL string defined alongside the repository, not a separate XML mapping file.
- **Existing `.hbm.xml` named queries in legacy code**: leave them in place unless the surrounding code is already being substantially reworked — don't do an opportunistic drive-by conversion as part of an unrelated change, since it expands the diff's blast radius for no functional benefit and reviewers can't easily verify translation equivalence in a large unrelated PR.
- If you do touch one, apply the same translation-verification checklist above before treating it as equivalent to a Fluent/LINQ replacement.
