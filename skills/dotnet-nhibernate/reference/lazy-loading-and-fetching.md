# Lazy Loading, Proxies, and Fetch Strategy

This is the highest-value file in this skill — the majority of real production incidents traced back to NHibernate on this kind of team come from here.

## How lazy loading actually works

A lazy-mapped reference or collection is populated with a dynamic proxy, not the real data, at load time. The proxy only hits the database the moment something beyond the identifier is accessed (a property read, `Count`, enumeration, etc.). This means:

- Loading an entity is cheap even if it has many lazy relationships — until something touches them.
- Whether a given line of code causes a DB round trip depends on *whether the session is still open*, not just on what the code does.

## Session Boundary Failures (`LazyInitializationException`)

This is thrown when code accesses a lazy property/collection after the owning session has closed. It is a symptom, not the root cause — the root cause is almost always the session boundary being drawn in the wrong place (see `session-lifecycle.md`).

**Where this actually happens in an onion architecture:**
1. Repository loads an entity with the session still open, returns it up to the service layer.
2. Service layer does some work, then — often several method calls later, sometimes after an `await` that yields control — accesses a lazy nav property.
3. If the session closed in between (because the repository call scope ended it, or because of a `using` block around the session), this throws.

**The fix is almost never "catch the exception."** Options in order of preference:
1. Fix the session boundary so it spans the full unit of work (see `session-lifecycle.md`).
2. If the boundary is correct but you just don't need the lazy data most of the time, explicitly eager-fetch it for the specific query that needs it (see Fetch Strategies below) rather than widening the session lifetime further than necessary.
3. As a last resort for genuinely optional data, wrap the specific access in `NHibernateUtil.IsInitialized(entity.Prop)` check and handle the unloaded case explicitly — but treat this as a smell, not a pattern to repeat.

## N+1 Queries

Classic pattern: loop over a collection, access a lazy nav property inside the loop.

```csharp
// N+1: one query for orders, then one query PER order for its line items
var orders = session.Query<Order>().Where(o => o.Status == OrderStatus.Pending).ToList();
foreach (var order in orders)
{
    decimal total = order.LineItems.Sum(li => li.Price); // triggers a lazy load, once per order
}
```

Fixes, in order of preference for this shape of problem:
- **Query-level fetch join** — `session.Query<Order>().Fetch(o => o.LineItems)` (LINQ), or `.Fetch(SelectMode.Fetch)` in QueryOver, or a `join fetch` in HQL. Pulls the related collection in the same round trip. Watch for cartesian-product blowup if you fetch-join multiple collections on the same query — see **Fixing the cartesian-product problem** below, and prefer `.FetchMany` + `.ToFuture()` batching (see `bulk-and-stateless.md`) over stacking multiple collection fetch-joins on one query where possible.
- **Batch-size mapping** — set `BatchSize(n)` on the collection/entity mapping so NHibernate loads N proxies' worth of the lazy relationship in one `IN (...)` query instead of one query per proxy. Good default improvement with zero call-site changes when the access pattern is unpredictable.
- **Projection instead of full entity load** — if you only need a few fields, don't load the entity graph at all; project straight to a DTO (see `query-strategy.md`).
- **A `FetchProfile`** — see below, when the same eager-loading shape is needed by several different call sites.

`scripts/detect_n_plus_one.py` does a heuristic first-pass scan for the loop-plus-lazy-access shape — it will have false positives/negatives, treat it as a prompt to look closer, not a verdict.

## Fixing the cartesian-product problem (`DistinctRootEntity`)

Fetch-joining a one-to-many collection duplicates the parent row once per child row in the raw SQL result set — fetch-joining *two* collections on the same query multiplies that further (a genuine cartesian product). Without correction, NHibernate hands back duplicate parent objects (or, worse, a correct-looking but bloated in-memory row count that silently breaks pagination math).

Fix by telling NHibernate to de-duplicate the root entity in the result:

```csharp
// QueryOver
var orders = session.QueryOver<Order>()
    .Fetch(o => o.LineItems).Eager
    .TransformUsing(Transformers.DistinctRootEntity)
    .List();

// Criteria
var orders = session.CreateCriteria<Order>()
    .SetFetchMode("LineItems", FetchMode.Eager)
    .SetResultTransformer(CriteriaSpecification.DistinctRootEntity)
    .List<Order>();
```

**Caveats worth flagging in review:**
- `DistinctRootEntity` de-duplicates in application memory *after* the full (bloated) result set has already come back from the database — it fixes correctness, not the underlying row-count cost. If the duplication factor is large (fetch-joining several sizeable collections), this is a sign to switch to `.ToFuture()`-batched separate queries instead of one big fetch-joined query, not just paper over it with the transformer.
- **Never fetch-join a collection on a paged query** (`.Skip()`/`.Take()`) — the database applies paging to the duplicated row set, not the logical entity set, so you get incorrect page boundaries regardless of `DistinctRootEntity` (which only de-duplicates client-side, after paging already happened server-side). Page first with a non-fetch-joined query, then fetch the specific page's related data separately.

## `FetchProfile` — reusable named eager-loading shapes

If several different call sites need the same relationship eager-loaded, rather than repeating `.Fetch(...)` everywhere (or, worse, having some call sites remember it and others forget), define a `FetchProfile` once and toggle it per use case:

```csharp
// Configuration-time (Fluent)
.ExposeConfiguration(cfg => cfg.AddFetchProfile("OrderWithLineItems", fp =>
    fp.Fetch<Order>(o => o.LineItems).Eager()));

// Call-site
session.EnableFetchProfile("OrderWithLineItems");
var order = session.Get<Order>(id); // LineItems now eager for the rest of this session
```

Prefer this over scattering `.Fetch()` across many query variants when the *reason* for eager-loading is a stable, nameable use case ("the order detail page always needs line items") rather than a one-off. Downside: the profile is enabled per-session, so it affects every subsequent load in that session, not just one query — don't reach for this when only one specific query needs the eager load; use a query-level `.Fetch()` there instead.

## AutoMapper + Proxies (the #1 recurring bug on this stack)

This is specific to the combination this team uses (NHibernate entities → AutoMapper → DTOs in the onion architecture's outbound mapping step), and it's subtle enough to deserve its own section.

**The trap:** an AutoMapper profile that maps an entity to a DTO will trigger a lazy load for *every mapped property that touches a lazy nav property or collection*, even ones the DTO didn't really need to expose meaningfully (e.g. mapping `order.Customer.Name` when `Customer` is lazy pulls the whole `Customer` row). If this mapping happens outside the originating session's scope — which is easy to do if mapping happens in the service layer after the repository call "returns" — it throws `LazyInitializationException` from inside AutoMapper's generated code, which is a confusing place to debug from because the stack trace points at AutoMapper, not at your query.

Even when the session *is* still open, mapping-triggered lazy loads are a silent perf problem: a DTO mapping step that looks like an in-memory operation can issue dozens of queries.

**Fix approach, in order of preference:**
1. **Project directly to the DTO shape in the query** (QueryOver/LINQ `.Select(...)` or `ProjectTo<T>()` if using `AutoMapper.QueryableExtensions`) instead of loading full entities and mapping after. This avoids the proxy graph entirely for the read path — see `query-strategy.md` § Projections.
2. If you do need the full entity graph (e.g. it's also being used for a write), explicitly eager-fetch exactly the properties the mapping profile touches, and keep the mapping profile and the fetch strategy in sync — comment the profile noting which properties require eager fetch.
3. **Audit AutoMapper profiles for lazy-property access as a matter of course** — `scripts/detect_automapper_lazy_risk.py` cross-references mapping profiles against entity mappings to flag properties in a profile that correspond to lazy-mapped members in the source entity, so this can be caught in review rather than in production.

## Fetch Strategy Decision Table

| Situation | Use |
|---|---|
| Need related data for most/all rows in this specific query | Query-level fetch join (`.Fetch`/`.FetchMany`) |
| Related data is needed unpredictably across many call sites | `BatchSize` on the mapping |
| Only need a handful of scalar fields, not the full graph | Projection to DTO (skip entity loading of the relation entirely) |
| Related collection could be very large (e.g. thousands of rows) | Don't eager-fetch the whole collection at all — page it with a separate query |
| One-off diagnostic/debugging query | `NHibernateUtil.Initialize(entity.Prop)` to force-load explicitly, understanding the cost |

## `Get` vs `Load`

- `session.Get<T>(id)` — hits the DB immediately, returns `null` if missing. Use when you need to branch on existence.
- `session.Load<T>(id)` — returns a proxy without hitting the DB, throws `ObjectNotFoundException` on first real access if missing. Use when you only need the entity to set up a relationship (e.g. `order.CustomerId = id` equivalent via `order.Customer = session.Load<Customer>(id)`) and you're confident the ID is valid — this avoids an unnecessary round trip.

## Extra-lazy collections

Ordinary lazy collections load the *entire* collection the first time anything touches it, even if all you needed was `.Count` or a `.Contains(x)` check. Mark a collection `.LazyLoad("Extra")` (Fluent: `.Extra.LazyLoad()` depending on version — check the current Fluent NHibernate API surface) to let common operations avoid loading the whole thing:

```csharp
HasMany(x => x.LineItems).Extra.LazyLoad();
```

With this set, `order.LineItems.Count` issues a `SELECT COUNT(*)` instead of loading every row, and `order.LineItems.Contains(item)` issues a targeted existence check instead of a full load. Enumerating the collection (`foreach`, `.ToList()`, etc.) still triggers a full load as normal.

**Worth it when**: a large collection is frequently checked for count/containment but only occasionally actually enumerated in full (e.g. showing "142 items" in a UI badge without ever rendering all 142 unless the user expands the section). Not worth the added mapping complexity for small collections or ones that are enumerated every time they're touched anyway — extra-lazy adds a query for the count/contains case, so if you were about to enumerate the whole thing regardless, it's pure overhead.

## Lazy properties

Beyond whole-entity proxies and lazy collections, individual scalar properties can be marked lazy — useful for a large column (a BLOB, a big text field) that most queries for the entity don't need:

```csharp
Map(x => x.ProfileImageBytes).LazyLoad();
```

**This requires bytecode enhancement** (build-time IL weaving) to work — unlike proxy-based entity/collection laziness, which NHibernate implements via dynamic subclassing at runtime, lazy properties on a *non-virtual* property need the assembly enhanced at build time, or the property must be `virtual` and part of a lazy group NHibernate can intercept. Check the project's build already has bytecode enhancement configured before assuming `.LazyLoad()` on a property will actually defer loading — if it's not configured, this silently has no effect rather than throwing, which makes it an easy thing to add and believe is working when it isn't. Verify with `sql-diagnostics.md` (confirm the property doesn't appear in the initial SELECT) rather than assuming from the mapping alone.
