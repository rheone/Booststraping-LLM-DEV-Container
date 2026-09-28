---
name: dotnet-nhibernate
description: Use this skill for any work involving NHibernate or Fluent NHibernate in a C# codebase — writing or reviewing entity mappings, diagnosing LazyInitializationException/N+1/StaleObjectStateException and other NHibernate exceptions, choosing between QueryOver/HQL/LINQ/native SQL, writing IUserType/ICompositeUserType custom types, reasoning about session/transaction lifecycle, cascade and inverse configuration, second-level caching, schema-vs-mapping drift, or onboarding a developer who is new to NHibernate (especially one coming from EF Core). Trigger this proactively whenever NHibernate, Fluent NHibernate, ClassMap, ISession, QueryOver, or an hbm.xml file is mentioned, even if the user doesn't name the skill directly. Targets NHibernate 5.7+ conventions.
license: Apache-2.0
user-invocable: true
disable-model-invocation: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# NHibernate Toolkit

NHibernate is old enough that public documentation is thin and generic C# knowledge tends to default to EF Core idioms that don't apply here (no `DbContext` change tracking the same way, no code-first migrations, proxies instead of lazy-loading via query re-execution, etc.). This skill exists to keep that from happening and to encode this team's specific Fluent NHibernate conventions.

**Don't try to hold the whole domain in your head at once.** This file is a router. Read the 1-2 reference files that match the task below, do the work, and stop. Loading every reference file for a one-line mapping question wastes context and increases the odds of mixing up conventions from unrelated areas (e.g. applying cache guidance to a query-writing task).

## Step 0: What kind of task is this?

| Situation | Go to |
|---|---|
| Writing a new entity + Fluent mapping from scratch | `reference/mapping-conventions.md`, then `templates/classmap-entity.cs` |
| Deciding whether a house-style rule should be a documented convention or an enforced Fluent `Convention` class | `reference/fluent-convention-api.md` § Which of your documented conventions belong here |
| Entity has (or should have) a composite primary key | `reference/composite-keys.md` — especially the `Equals`/`GetHashCode` requirement, which is the most commonly missed step |
| Given an existing SQL table, need the correct Fluent mapping (or vice versa) | `reference/schema-mapping-roundtrip.md` |
| Setting up a parent/child or many-to-many relationship | `reference/cascade-and-relationships.md` |
| Inheritance (table-per-hierarchy/subclass/concrete) or collection type choice (bag/set/list) | `reference/cascade-and-relationships.md` (§ Inheritance, § Collections) |
| Writing or reviewing a query (which API to use, translating hand-written SQL) | `reference/query-strategy.md` |
| Named queries / hbm.xml question | `reference/query-strategy.md` (§ Named Queries) |
| Deciding where a `Component`, `ICompositeUserType`, or `IUserType` applies to a value object | `reference/custom-user-types.md` § Three overlapping ways — pick the simplest one that works |
| Mapping an enum, value object, money type, or anything needing a custom column conversion | `reference/custom-user-types.md`, then `templates/user-type.cs` |
| Debugging a thrown NHibernate exception | `reference/common-exceptions.md` — look it up by exception type first |
| Something is slow / suspect N+1 / suspect too many round trips | `reference/lazy-loading-and-fetching.md`, consider running `scripts/detect_n_plus_one.py` |
| `LazyInitializationException` specifically | `reference/lazy-loading-and-fetching.md` (§ Session Boundary Failures) |
| Duplicate parent rows from a fetch-joined collection, or several relationships needing the same eager-load shape across many call sites | `reference/lazy-loading-and-fetching.md` (§ Fixing the cartesian-product problem, § FetchProfile) |
| Writing/reviewing an AutoMapper profile that reads from NHibernate entities | `reference/lazy-loading-and-fetching.md` (§ AutoMapper + Proxies) — this is the #1 recurring bug class on this team |
| Deciding where a session/transaction should open and close (repository vs service layer), or a `FlushMode` question | `reference/session-lifecycle.md` |
| Anything `async`/`await` with NHibernate — concurrent operations on one session, sync-over-async (`.Result`/`.Wait()`), cancellation tokens, retries | `reference/async-patterns.md` — read this before writing or reviewing any async repository/service code, not just when something's already broken |
| Debugging with actual generated SQL, turning on logging, confirming an N+1 suspicion with real evidence | `reference/sql-diagnostics.md` |
| Cross-dialect concerns (paging syntax, string/boolean types, collation) if this project targets more than one DB engine | `reference/dialect-notes.md` |
| Second-level cache question, query cache question, or a `StaleObjectStateException` | `reference/caching-and-concurrency.md` |
| Auditing who/when made a change, and getting the current user into that logic | `reference/filters-and-interceptors.md` § Audit integration — this specifically needs a per-session `IInterceptor`, not a global event listener |
| Choosing between `IInterceptor` and an event listener for some cross-cutting concern | `reference/filters-and-interceptors.md` § IInterceptor vs event listeners |
| Soft-delete, multi-tenancy filters, audit columns, auto-timestamps | `reference/filters-and-interceptors.md` — for soft-delete specifically, note this covers BOTH the read-side filter and intercepting the delete call itself |
| Batching several queries (including projected/DTO-shaped ones) into one round trip | `reference/bulk-and-stateless.md` § Multi-query / futures |
| Bulk import/update job, performance-critical batch work | `reference/bulk-and-stateless.md` |
| Writing NHibernate-backed unit/integration tests | `reference/testing-nhibernate.md` |
| New to NHibernate entirely / unfamiliar vocabulary | `reference/glossary.md` first, then whatever task above applies |
| Not sure what's wrong, just "this feels off" | `reference/anti-patterns-catalog.md` — index of every known bad pattern with links |
| Reviewing a PR/diff for correctness before merge | See "Full Review Workflow" below |

## Full review workflow (PR / diff review)

When asked to review NHibernate-related changes rather than write new code, don't just read the diff — run the detectors, they catch things static reading misses:

1. Read `reference/anti-patterns-catalog.md` for the index of what to look for.
2. Run whichever of these apply to the diff (see `scripts/README.md` for usage):
   - `scripts/detect_lazy_after_close.py` — lazy nav property access outside session scope
   - `scripts/detect_n_plus_one.py` — loop + lazy-load access heuristic
   - `scripts/detect_cascade_misconfig.py` — mismatched `Inverse()`/cascade pairs across mapping files
   - `scripts/detect_mapping_schema_drift.py` — mapping vs. actual DB schema (needs a connection string or a schema dump; ask the user if neither is available rather than skipping this check silently)
   - `scripts/detect_sync_over_async.py` — sync-over-async (`.Result`/`.Wait()`) and `async void` on NHibernate-touching code
3. For anything the scripts can't catch (query strategy choices, cascade *semantics* rather than syntax, transaction boundary placement), reason through it using the relevant reference file above.
4. If the diff touches schema-affecting mapping changes (new/renamed/removed columns), read `reference/schema-mapping-roundtrip.md` and specifically check for the expand/contract pattern before flagging a rename as unsafe.

For a deep audit of schema/mapping drift across an entire codebase (not just a diff), see `agents/schema-drift-auditor.md` — this is a larger job best run as its own focused pass rather than folded into a quick review.

## Ground rules

- **Never execute DDL or run schema migrations.** This skill proposes and flags; a human applies schema changes. `detect_mapping_schema_drift.py` reads schema, it never writes it.
- **State your NHibernate version assumption if the user's codebase might be older.** Some guidance here (e.g. `IAsyncSession`, certain LINQ provider fixes) assumes 5.x. Flag if you see signs of a 3.x/4.x codebase (e.g. no async session usage anywhere) since older-version guidance differs.
- **Prefer the team's stated convention over the "generic best practice" when they conflict**, and say so explicitly when they diverge, rather than silently picking one.
- When genuinely uncertain between two valid approaches (e.g. `Cascade.All` vs `Cascade.AllDeleteOrphan` for a given relationship), state the tradeoff rather than guessing — this is a place wrong guesses cause real data loss.
- **Default to async.** Any new repository/service code should be written against the async session API from the start (`reference/async-patterns.md`) — don't write a sync version "for simplicity" and plan to convert later; converting sync-over-async out of an established codebase is far more work than writing async from the outset.

## Reference file index

- `reference/glossary.md` — vocabulary for developers new to NHibernate (Session vs SessionFactory, proxy, transient/detached/persistent)
- `reference/session-lifecycle.md` — session-per-request patterns, transaction boundary conventions, FlushMode
- `reference/async-patterns.md` — concurrent-session hazards, sync-over-async, cancellation, retries
- `reference/lazy-loading-and-fetching.md` — proxies, N+1, fetch strategies, the AutoMapper trap
- `reference/mapping-conventions.md` — Fluent NHibernate house style for ClassMap/SubclassMap/ComponentMap
- `reference/fluent-convention-api.md` — enforcing mechanical mapping rules in code instead of relying on human memory + review
- `reference/schema-mapping-roundtrip.md` — mapping → DDL and DDL → mapping, both directions
- `reference/cascade-and-relationships.md` — cascade/inverse decision tables, inheritance strategies, collection types
- `reference/composite-keys.md` — mapping, equality requirements, querying, caching, and relationship interactions for composite primary keys
- `reference/query-strategy.md` — QueryOver vs HQL vs LINQ vs native SQL, named queries, SQL→QueryOver/HQL translation, NHibernate-specific LINQ extensions
- `reference/custom-user-types.md` — IUserType / ICompositeUserType patterns
- `reference/caching-and-concurrency.md` — second-level cache, optimistic/pessimistic locking
- `reference/filters-and-interceptors.md` — IFilter, IInterceptor, event listeners for cross-cutting concerns
- `reference/bulk-and-stateless.md` — StatelessSession, batching, multi-query/futures
- `reference/testing-nhibernate.md` — test session factory setup, fixture strategy
- `reference/common-exceptions.md` — exception → likely cause → fix lookup table
- `reference/sql-diagnostics.md` — turning on SQL logging, reading generated queries, profiling
- `reference/dialect-notes.md` — where dialect differences bite (paging, string/boolean types, collation)
- `reference/anti-patterns-catalog.md` — master index of bad patterns, cross-linked

## Templates

- `templates/classmap-entity.cs` — starting point for a new entity mapping, not copy-paste-blind — always adapt to the entity's actual relationships
- `templates/user-type.cs` — IUserType skeleton with the interface members actually implemented correctly (this interface is easy to get subtly wrong from memory)
- `templates/automapper-profile-safe.cs` — profile pattern annotated with why it avoids triggering lazy loads
- `templates/repository-base.cs` — async-first repository base class; transaction boundary deliberately left to the service layer
- `templates/queryover-projection.cs` — LINQ and QueryOver projection patterns that avoid full entity loads
- `templates/session-factory-di-setup.cs` — the canonical example tying together interceptor, batch size, cache, and filter registration in one place, since those are discussed separately across several reference files
