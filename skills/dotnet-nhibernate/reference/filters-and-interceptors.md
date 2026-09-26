# Filters, Interceptors, and Event Listeners

For cross-cutting concerns (soft-delete, multi-tenancy, audit columns, auto-timestamps) — don't reinvent these per-repository. NHibernate has first-class mechanisms; use them consistently rather than scattering `WHERE IsDeleted = 0` by hand across every query.

## `IInterceptor` vs. event listeners — these are not interchangeable

Earlier revisions of this file used these somewhat loosely. They're genuinely different mechanisms with different scopes:

| | `IInterceptor` | Event listener (`IPreInsertEventListener`, etc.) |
|---|---|---|
| Scope | One instance per **session** (or a single shared default for the factory) | Registered once at **configuration/session-factory** level, fires for every session |
| Can hold per-request state (e.g. "current user")? | Yes, if assigned per-session (see below) — this is the whole reason to reach for it over an event listener | No — a listener instance is shared across all sessions/requests; don't store request-scoped state as fields on it |
| Covers | A broad set of lifecycle hooks on one object: `OnSave`, `OnFlushDirty`, `OnLoad`, `OnPrepareStatement` (SQL logging, see `sql-diagnostics.md`), `AfterTransactionBegin`/`AfterTransactionCompletion`, etc. | One specific event each — `IPreInsertEventListener`, `IPreUpdateEventListener`, `IPreDeleteEventListener`, `IPostLoadEventListener`, etc. Multiple listeners for the same event all run. |
| Good for | Anything needing per-request context (current user, correlation ID), or several related hooks best kept in one class | A single, stateless, cross-cutting rule that's the same for every session — like the soft-delete veto below, which needs no per-request state |

**Rule of thumb**: if the logic needs to know *who* is making the change (current user, tenant), use a per-session `IInterceptor` (see Audit integration below). If the logic is the same regardless of who's asking (soft-delete veto, a fixed validation rule), an event listener registered once at configuration time is simpler and avoids any risk of accidentally leaking state between sessions.

## Audit integration: getting the current user into audit logic

The audit interceptor example in this file (further below) sets `CreatedAtUtc`, which needs no external context — but a real audit trail usually also needs `ModifiedBy`, which requires knowing who's making the request. This is where the interceptor-vs-listener distinction above matters in practice: an event listener registered once at configuration time has no way to know which user issued which request, because one listener instance is shared across every session. An `IInterceptor` assigned **per-session** can carry that context:

```csharp
public class AuditInterceptor : EmptyInterceptor
{
    private readonly string _currentUser;

    // Constructed fresh per-session (see DI wiring below), so this is safe to hold as a field —
    // unlike an event listener, this instance's lifetime matches one request, not the whole app.
    public AuditInterceptor(string currentUser) => _currentUser = currentUser;

    public override bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
    {
        if (entity is IAuditable auditable)
        {
            SetIfPresent(propertyNames, state, nameof(IAuditable.CreatedAtUtc), DateTime.UtcNow);
            SetIfPresent(propertyNames, state, nameof(IAuditable.CreatedBy), _currentUser);
            return true;
        }
        return false;
    }

    private static void SetIfPresent(string[] names, object[] state, string propName, object value)
    {
        var idx = Array.IndexOf(names, propName);
        if (idx >= 0) state[idx] = value;
    }
}
```

```csharp
// DI wiring — assign the interceptor when the session is opened, not at factory-build time,
// so it can be constructed with THIS request's user rather than a shared/stale value.
services.AddScoped(sp =>
{
    var currentUser = sp.GetRequiredService<ICurrentUserAccessor>().UserName; // your own abstraction over ClaimsPrincipal/HttpContext
    var interceptor = new AuditInterceptor(currentUser);
    return sp.GetRequiredService<ISessionFactory>().OpenSession(interceptor);
});
```

**Common mistake**: registering the audit interceptor via `cfg.SetInterceptor(new AuditInterceptor(...))` at session-factory build time (see `templates/session-factory-di-setup.cs`) — the factory is built once at startup, long before any request/user exists, so this either captures a meaningless value or throws trying to resolve a scoped service from a singleton-lifetime configuration step. Per-session assignment via `OpenSession(interceptor)`, as above, is what actually gets you correct per-request "who did this" data.

## `IFilter` — soft-delete and multi-tenancy

Define once via `FilterDefinition`, enable per-session:

```csharp
// Mapping-time
HasFilter("SoftDelete"); // applied on the relevant ClassMap
// Configuration-time
mapper.FilterDefinition("SoftDelete", def => def.WithCondition("IsDeleted = 0"));
// Session-time — enable once per session, typically in the same middleware that opens the session
session.EnableFilter("SoftDelete");
```

**Critical review point**: a filter is silently bypassed by anything that skips the normal query path — native SQL, `IgnoreQueryFilters()`-equivalent explicit opt-outs, and (this is the one people forget) `StatelessSession` doesn't respect filters the same way a regular session does. If a bulk job or background service uses `StatelessSession` (see `bulk-and-stateless.md`) against a soft-delete-filtered or tenant-filtered table, flag this explicitly — it needs manual `WHERE` clauses since the filter won't protect it.

For multi-tenancy specifically: prefer the filter approach (row-level `TenantId` filter) over hoping every hand-written query remembers the `WHERE TenantId = @current`. Audit any repository method that takes a raw `session.CreateSQLQuery` path for whether it needs the tenant condition added manually, since native SQL bypasses filters entirely.

## The other half of soft-delete: intercepting the delete itself

`IFilter` (above) handles the *read* side of soft-delete — hiding already-deleted rows from queries. It does nothing to stop an ordinary `session.Delete(entity)` call from issuing a real `DELETE` statement. For soft-delete to actually work end-to-end, something needs to turn a delete call into an update:

```csharp
public class SoftDeleteEventListener : IPreDeleteEventListener
{
    public bool OnPreDelete(PreDeleteEvent @event)
    {
        if (@event.Entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedAtUtc = DateTime.UtcNow;

            // Re-issue as an update instead of letting the delete proceed.
            @event.Session.Update(@event.Entity);

            return true; // true = veto the actual delete
        }
        return false; // false = allow the real delete to proceed for non-soft-deletable entities
    }
}
```

Register this alongside any other event listeners at session-factory configuration time (see `templates/session-factory-di-setup.cs`).

**Things to get right, and to check for in review:**
- **Return value semantics are inverted from what people expect** — `true` means "veto the delete" (i.e., the soft-delete succeeded, don't actually delete), `false` means "let the real delete happen." Getting this backwards either hard-deletes everything (defeating the point) or silently blocks all deletes including ones that were meant to be real (e.g. a genuine data-cleanup job).
- **Cascade interaction**: if the soft-deleted entity has `Cascade.AllDeleteOrphan` children (see `cascade-and-relationships.md`), NHibernate's cascade delete still fires against those children *before* this listener gets a chance to veto the parent's delete — meaning children can get hard-deleted even though the parent was soft-deleted. Decide deliberately whether children should cascade to soft-delete too (requiring the same `ISoftDeletable` handling on them) or whether hard-deleting children of a soft-deleted parent is actually acceptable, rather than discovering the mismatch in production.
- **`StatelessSession` bypasses event listeners entirely** (same caveat as filters, see `bulk-and-stateless.md`) — a bulk job using `StatelessSession.Delete(...)` will hard-delete regardless of this listener. Bulk jobs need to implement soft-delete explicitly (an `UPDATE` statement) rather than relying on this mechanism.

## `IInterceptor` / event listeners — audit columns and auto-timestamps

For timestamp-only fields with no per-user context needed (`CreatedAtUtc` alone, no `CreatedBy`), a simpler shared interceptor or event listener is fine — the per-session pattern above is only necessary once "who" matters, not just "when":

```csharp
public class TimestampInterceptor : EmptyInterceptor
{
    public override bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
    {
        if (entity is IAuditable auditable)
        {
            var idx = Array.IndexOf(propertyNames, nameof(IAuditable.CreatedAtUtc));
            state[idx] = DateTime.UtcNow;
            return true; // tells NHibernate the state array was modified
        }
        return false;
    }
}
```

**Common mistake**: forgetting the `return true` (or equivalent) that tells NHibernate the `state` array was mutated — without it, the change is silently discarded even though the interceptor code "ran."

## Where this fits with the onion architecture

Keep interceptor/filter registration in the infrastructure/persistence layer's session-factory configuration, not scattered into service-layer code — this is exactly the kind of cross-cutting concern the onion architecture's outer layers should own, so the domain and service layers stay unaware that soft-delete or audit tracking even exist as a mechanism.
