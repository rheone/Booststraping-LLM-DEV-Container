# Async Patterns

NHibernate 5.x has full async support on `ISession` (`GetAsync`, `SaveAsync`, `Query<T>().ToListAsync()`, `SaveOrUpdateAsync`, transaction `CommitAsync`/`RollbackAsync`, etc.). A modern ASP.NET Core app built on this stack should be async end-to-end through the repository and service layers. This file didn't exist in the first draft of this skill and should have — sync-over-async on a session is at least as common a production incident as the lazy-loading issues in `lazy-loading-and-fetching.md`, and it fails in uglier ways (thread pool starvation, deadlocks) rather than a clean exception.

## The core rule: `ISession` is not thread-safe, async or not

This is the rule that makes NHibernate's async story different from "just add `Async` to method names." A single `ISession` must not have two operations in flight against it concurrently — including two `await`ed calls running "at the same time" via `Task.WhenAll`.

```csharp
// WRONG — same session, two concurrent operations. This corrupts session state
// unpredictably (not always an exception — sometimes silently wrong results).
var ordersTask = session.Query<Order>().ToListAsync();
var customersTask = session.Query<Customer>().ToListAsync();
await Task.WhenAll(ordersTask, customersTask);
```

```csharp
// RIGHT — sequential awaits on the same session are fine; the session just isn't
// touched by two calls simultaneously.
var orders = await session.Query<Order>().ToListAsync();
var customers = await session.Query<Customer>().ToListAsync();
```

```csharp
// RIGHT (if you genuinely need concurrency) — separate sessions per concurrent operation.
// Note: this means separate transactions too; only do this for genuinely independent reads.
async Task<(List<Order>, List<Customer>)> LoadBothAsync(ISessionFactory factory)
{
    using var s1 = factory.OpenSession();
    using var s2 = factory.OpenSession();
    var ordersTask = s1.Query<Order>().ToListAsync();
    var customersTask = s2.Query<Customer>().ToListAsync();
    await Task.WhenAll(ordersTask, customersTask);
    return (await ordersTask, await customersTask);
}
```

If you see `Task.WhenAll`, `Task.Run`, or any fan-out/parallel pattern touching the same injected `ISession` instance, flag it — this is a correctness bug, not a style preference, and it often doesn't throw consistently, which makes it a nasty one to catch in testing. Prefer `.ToFuture()`/`.ToFutureValue()` (see `bulk-and-stateless.md`) for the common case of "I want several independent reads efficiently" — that gets you one round trip on one session without any concurrency hazard at all.

## Sync-over-async on a session (`.Result` / `.Wait()` / `GetAwaiter().GetResult()`)

Calling `.Result` or `.Wait()` on an async NHibernate call blocks the calling thread while it waits for the async operation to complete. In an ASP.NET Core context (no synchronization context by default, unlike old ASP.NET Framework) this is less likely to deadlock outright than the classic WebForms/MVC `.Result` deadlock, but it still:

- Ties up a thread pool thread doing nothing, which under load causes thread pool starvation — the app gets slow and unresponsive well before it errors outright, which makes it a hard incident to diagnose from symptoms alone.
- Is a strong signal the surrounding code wasn't actually built async — if you see one sync-over-async call, look at the whole call chain; it usually means an async method got bolted onto a sync call chain instead of the async-ness being threaded all the way up.

Fix: make the caller async and propagate `await` up the call stack — including through the onion architecture's layers. A repository method returning `Task<Order>` that gets called with `.Result` in the service layer just moved the sync-over-async problem up one level; the service method needs to be `async Task<...>` too, and so on up to the controller action.

`scripts/detect_sync_over_async.py` does a syntactic scan for `.Result`/`.Wait()`/`GetAwaiter().GetResult()` on expressions that look like NHibernate calls — see `scripts/README.md`.

## `async void` — never on anything NHibernate touches

Standard async guidance applies with extra force here: `async void` swallows exceptions in a way that's especially dangerous with NHibernate, because a failed save or a failed transaction commit that gets silently swallowed can leave the session/transaction in an inconsistent state with nothing in the logs pointing at why. The only legitimate `async void` is a top-level event handler; anything in the repository/service/onion layers should be `async Task`.

## Cancellation tokens

Thread `CancellationToken` through repository and service methods and pass it to the NHibernate async overloads (`ToListAsync(cancellationToken)`, `SaveAsync(entity, cancellationToken)`, etc.) — most of them accept one. A request-scoped `HttpContext.RequestAborted` token propagated down means a client disconnect actually cancels the in-flight query rather than letting it run to completion for no one. This matters more than it sounds for expensive reporting-style queries specifically.

**Caveat**: cancelling a query mid-transaction leaves that transaction in a state that needs an explicit rollback, not just letting the cancelled `Task` fall out of scope — make sure cancellation paths still reach a `finally`/`using`-based rollback, the same discipline as any other exception path (see `session-lifecycle.md`).

## Async and interceptors/filters

`IInterceptor`'s synchronous methods (`OnSave`, `OnFlushDirty`, etc. — see `filters-and-interceptors.md`) don't have async equivalents in the way session operations do; they run synchronously as part of the flush regardless of whether the surrounding session call was awaited. Don't try to do async work (an outbound API call, another DB call) inside an interceptor callback — if audit/side-effect logic needs to be async, do it as a separate step after `SaveAsync` completes, not inside the interceptor.

## Async and `StatelessSession`

`StatelessSession` also has async overloads (`InsertAsync`, `UpdateAsync`, etc. as of recent NHibernate versions) — use them for bulk jobs the same way, and the same single-session-no-concurrent-operations rule applies. Don't parallelize a bulk import across `Task.WhenAll` against one stateless session for the same reason as above; if you want parallel bulk throughput, use multiple stateless sessions (one per worker/partition of the batch), not one shared across concurrent tasks.

## Transient fault handling / retries

NHibernate has no built-in equivalent to EF Core's `IExecutionStrategy` for automatically retrying transient DB errors (connection drops, transient timeouts). If the team wants retry-on-transient-failure behavior, it needs to be added explicitly — typically a Polly retry policy wrapping the repository call — and it needs to be retry-safe with NHibernate's session/transaction lifecycle specifically:

- **Never retry by reusing the same session/transaction that just failed** — a session that threw is not guaranteed to be in a usable state afterward. Retry by opening a fresh session (and, if inside a `using`, letting the failed one dispose) and redoing the full unit of work, not just the last operation.
- Only wrap genuinely idempotent operations in an automatic retry, or ones where retrying will hit the same optimistic-concurrency check that would prevent a double-apply (see `caching-and-concurrency.md`) — a blind retry around a non-idempotent write can double-apply a side effect if the first attempt actually succeeded server-side but the client-side confirmation was what failed.
