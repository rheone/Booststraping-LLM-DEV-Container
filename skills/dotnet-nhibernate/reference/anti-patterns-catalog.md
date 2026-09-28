# Anti-Patterns Catalog

A single scannable index of every known bad pattern this skill knows about, for "something feels off, not sure what" review situations. Each entry links to the file with the full explanation and fix.

## Session & lifecycle
- Static/ambient session shared across requests → `session-lifecycle.md`
- Repository opening/closing its own session per call inside a layered architecture → `session-lifecycle.md`
- Background service reusing one injected session across loop iterations → `session-lifecycle.md`
- Exception caught without rolling back the transaction → `session-lifecycle.md`
- `FlushMode.Manual` set without a matching explicit `Flush()` on every path that needs it → `session-lifecycle.md`

## Async
- `Task.WhenAll`/parallel fan-out touching the same `ISession` instance → `async-patterns.md`
- Sync-over-async (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) on an NHibernate call → `async-patterns.md`, run `scripts/detect_sync_over_async.py`
- `async void` on anything that touches NHibernate → `async-patterns.md`
- Async work (API calls, further DB calls) attempted inside a synchronous `IInterceptor` callback → `async-patterns.md`
- Automatic retry that reuses a session/transaction that just failed, instead of opening a fresh one → `async-patterns.md`

## Lazy loading & performance
- Lazy nav property accessed after session close → `lazy-loading-and-fetching.md`
- Loop + lazy access (N+1) → `lazy-loading-and-fetching.md`, run `scripts/detect_n_plus_one.py`
- AutoMapper profile silently triggering lazy loads on entity→DTO mapping → `lazy-loading-and-fetching.md`, run `scripts/detect_automapper_lazy_risk.py`
- Multiple independent queries issued sequentially instead of via `.ToFuture()` → `bulk-and-stateless.md`
- Fetch-joining multiple collections on one query causing cartesian-product blowup without `DistinctRootEntity` → `lazy-loading-and-fetching.md`
- Fetch-joining a collection on a paged (`.Skip()`/`.Take()`) query → `lazy-loading-and-fetching.md` — paging happens before de-duplication, breaks page boundaries regardless of `DistinctRootEntity`

## Mapping & schema
- `.Not.Nullable()` omitted where a field is actually required → `mapping-conventions.md`
- Mixed identifier generation strategies across related entities → `mapping-conventions.md`
- Mapping/schema drift (column exists in one but not the other) → `schema-mapping-roundtrip.md`, run `scripts/detect_mapping_schema_drift.py`
- Rename/type change done as a single atomic step instead of expand/contract → `schema-mapping-roundtrip.md`
- A domain-judgment rule (cascade, nullability) enforced as a blanket Fluent `Convention` instead of decided per-relationship → `fluent-convention-api.md` § Which of your documented conventions belong here
- A `Convention` with an `AcceptanceCriteria` gap silently applying to (or skipping) mappings it shouldn't → `fluent-convention-api.md`
- A new or changed convention with no test verifying it actually takes effect → `fluent-convention-api.md` § Testing conventions

## Relationships
- Neither side or both sides of a bidirectional relationship marked `.Inverse()` → `cascade-and-relationships.md`, run `scripts/detect_cascade_misconfig.py`
- `Cascade.AllDeleteOrphan` on a relationship that isn't true ownership → `cascade-and-relationships.md`
- `AsSet()` used on an entity without meaningful `Equals`/`GetHashCode` → `cascade-and-relationships.md`
- Table-per-concrete-class chosen "for normalization" when polymorphic queries were never actually needed (or vice versa) → `cascade-and-relationships.md`
- `CompositeId()` mapped without the entity overriding `Equals`/`GetHashCode` → `composite-keys.md` — the most common composite-key mistake, and it doesn't throw
- A relationship's FK columns to a composite-keyed entity mapped out of order → `composite-keys.md`

## Custom types
- `IUserType.Equals` using reference equality instead of value equality → `custom-user-types.md`
- `IsMutable` set incorrectly for the wrapped type → `custom-user-types.md`
- `ICompositeUserType` property order out of sync with column mapping → `custom-user-types.md`
- Enum mapped by integer value instead of string name → `custom-user-types.md`
- `ICompositeUserType` reached for where a plain `Component(...)` would have been simpler and safer → `custom-user-types.md` § Three overlapping ways

## Caching & concurrency
- Second-level cache enabled on a frequently-updated, multi-writer entity → `caching-and-concurrency.md`
- `StaleObjectStateException` caught and discarded or blindly overwritten → `caching-and-concurrency.md`
- Pessimistic locking used by default instead of optimistic → `caching-and-concurrency.md`
- Query cache enabled (`.Cacheable()`) without the returned entities also being second-level cached → `caching-and-concurrency.md` § Query cache
- Query cache assumed to be a reliable perf win on a frequently-written table without checking actual hit rate → `caching-and-concurrency.md`, `sql-diagnostics.md`

## Filters, interceptors, bulk
- Native SQL query bypassing a soft-delete or tenant filter → `filters-and-interceptors.md`
- `StatelessSession` used without manually replicating filter conditions → `bulk-and-stateless.md`, `filters-and-interceptors.md`
- Audit/timestamp fields set manually per-service instead of via interceptor → `filters-and-interceptors.md`
- Soft-delete implemented only as a read-side filter, with no delete-interception → real `DELETE` still fires → `filters-and-interceptors.md` § The other half of soft-delete
- `IPreDeleteEventListener` veto return value inverted (`true`/`false` confused) → `filters-and-interceptors.md`
- Cascade-deleted children of a soft-deleted parent hard-deleted anyway → `filters-and-interceptors.md`, `cascade-and-relationships.md`
- A global event listener used to carry per-request state (current user, tenant) instead of a per-session `IInterceptor` → `filters-and-interceptors.md` § IInterceptor vs event listeners
- Audit interceptor assigned via `cfg.SetInterceptor(...)` at factory-build time instead of per-session via `OpenSession(interceptor)` — captures a stale or meaningless "current user" → `filters-and-interceptors.md`
- Mixed entity types saved in an interleaved order, breaking ADO.NET statement batching despite `adonet.batch_size` being set → `bulk-and-stateless.md`
- `.LazyLoad()` on a property with no bytecode enhancement configured — silently has no effect → `lazy-loading-and-fetching.md` § Lazy properties

## Queries
- Custom C# method called inside a `session.Query<T>()` LINQ expression → `query-strategy.md`
- `.Skip()`/`.Take()` without an explicit `.OrderBy()` → `query-strategy.md`
- SQL/HQL built via string concatenation from user input → `query-strategy.md`
- Hand-written SQL "translated" to QueryOver/HQL without verifying join/distinct/subquery semantics → `query-strategy.md`
- New `.hbm.xml` files introduced into a Fluent-first codebase → `query-strategy.md`

If a pattern you're looking at genuinely isn't represented here, that's useful signal on its own — flag it as a possible gap in this skill rather than assuming it's fine because it's not listed.

## Deliberately out of scope

Not gaps — features considered and intentionally not built out. See `mapping-conventions.md` § Where this skill stops for the reasoning:
- `Any()` polymorphic associations
- NHibernate.Envers (full change-history auditing)
