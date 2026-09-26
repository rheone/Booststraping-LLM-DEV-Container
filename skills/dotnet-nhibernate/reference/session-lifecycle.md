# Session & Transaction Lifecycle

For async-specific session/transaction concerns (concurrent operations on one session, sync-over-async, cancellation mid-transaction), see `async-patterns.md` — this file covers lifecycle and boundary placement generally, whether sync or async.

Most of NHibernate's most confusing bugs — `LazyInitializationException`, connection pool exhaustion, partial commits — trace back to the session/transaction boundary being in the wrong place. Get this right first; it makes several other reference files' advice unnecessary to even think about.

## This team's convention: session-per-request (onion architecture)

Given the onion architecture (API → service → repository → domain), the session should be opened once per HTTP request and closed at the end of it, via middleware or DI scope — **not** opened per-repository-call and **not** left ambient/static.

```
Request in
  └─ Session opened (scoped DI lifetime, one ISession per request)
      └─ Transaction opened (either immediately, or lazily on first write — pick one convention and be consistent)
          └─ Controller → Service → Repository (all share the same injected ISession)
      └─ Transaction committed just before the response is written, or rolled back on exception
  └─ Session closed/disposed
Request out
```

**Why this matters for the onion architecture specifically:** if a repository opens and closes its own session per call, then any lazy property the service layer touches *after* the repository call returns will throw `LazyInitializationException`, because the session that would have loaded it is already gone. This is the single most common NHibernate bug in layered architectures — see `lazy-loading-and-fetching.md` for the fetch-strategy side of the fix, but the session boundary is the root cause, and fixing fetch strategy without fixing the boundary is a workaround, not the fix.

## Where the transaction boundary goes

Default to opening the transaction at the **service layer**, not the repository layer, because a single service-layer operation frequently calls multiple repository methods that need to commit atomically together (e.g., "create order" touches an `Order` repository and an `Inventory` repository). If each repository call had its own transaction, a failure partway through leaves inconsistent state.

Exceptions where repository-level transactions are acceptable:
- Genuinely standalone read-only queries with no write side effects
- Background jobs with a single unit of work (see `bulk-and-stateless.md`)

## Anti-patterns to flag in review

- **Ambient/static session** — a `static ISession` or a service-locator-style "current session" accessed from anywhere. Breaks thread safety (sessions aren't thread-safe) and makes the lifetime impossible to reason about. Flag this every time.
- **Opening a session but never explicitly starting a transaction** for write operations — some configurations auto-flush and it "mostly works" until it doesn't (partial writes on exception).
- **Catching and swallowing an exception without rolling back the transaction** — leaves the session in an inconsistent state for the rest of the request if anything downstream still uses it.
- **A background/hosted service holding the same injected `ISession` across multiple loop iterations** instead of a fresh session (or `IServiceScopeFactory`-created scope) per iteration — this is a session-lifetime bug that looks like a memory leak or stale-data bug and is genuinely hard to diagnose from symptoms alone.

## FlushMode

`FlushMode` controls when NHibernate synchronizes in-memory changes to the database, independent of when the transaction commits. This is a distinct concept from the transaction boundary above, and mixing them up causes confusing "why did a partial write happen mid-transaction" bugs.

| FlushMode | Behavior | Use when |
|---|---|---|
| `Auto` (default) | Flushes automatically before query execution if there are pending changes that could affect the query's results, and always before transaction commit | Correct default for almost everything — leave this alone unless you have a specific reason not to |
| `Commit` | Only flushes at transaction commit, never before a query | A query in the middle of a unit of work needs to see the database's pre-change state rather than pending in-memory changes, and you're deliberately relying on that — rare, and worth a comment explaining why when used |
| `Manual` | Never flushes automatically — you call `session.Flush()` explicitly | Bulk/batch scenarios where you want full control over exactly when writes hit the DB, or performance-critical paths where you're batching many changes and want one flush at the end rather than several auto-triggered ones |

**Common mistake to flag in review**: setting `FlushMode.Manual` (often copied from a bulk-import code path) and then forgetting to call `Flush()` before commit — changes silently never persist, with no exception at all, because commit doesn't force a flush under `Manual` the way it does under `Auto`/`Commit`. If you see `FlushMode.Manual`, verify there's an explicit `session.Flush()` call on every path that needs the changes persisted, including error/early-return paths.

## Quick self-check when reviewing a diff

Ask: "if I trace this code path, is there exactly one session open for the duration this entity might be touched, including everywhere it gets mapped or serialized?" If the answer requires tracing through more than 2-3 layers to be sure, that's itself a signal the boundary is drawn in a fragile place, independent of whether it's technically correct today.
