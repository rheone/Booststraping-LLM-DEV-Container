# Change Tracking and SaveChanges

## Entity states

Every entity instance the change tracker knows about has one of five `EntityState` values:

- **`Added`** — new, will be `INSERT`ed on the next `SaveChanges`.
- **`Unchanged`** — tracked, matches the database as last known.
- **`Modified`** — tracked, at least one property differs from its original snapshot; will be
  `UPDATE`d.
- **`Deleted`** — tracked, will be `DELETE`d on the next `SaveChanges`.
- **`Detached`** — not tracked at all; the context knows nothing about this instance's relationship
  to the database.

`dbContext.Add(entity)` sets `Added`; `dbContext.Remove(entity)` sets `Deleted`; a property you
mutate on an already-tracked entity flips that entity (and just that property, for the generated
`UPDATE`'s column list) to `Modified` automatically — you do not call `Update()` for an ordinary
mutation of a tracked entity. `dbContext.Update(entity)` is for the specific case of an entity that
arrived from outside the context (deserialized from a request body, detached) that you want treated
as an existing row to overwrite in full; it marks every scalar property `Modified`, not just the
ones that actually changed, which produces a wider `UPDATE` than mutating a tracked instance would.

## SaveChanges

`SaveChanges()`/`SaveChangesAsync()` inspects every tracked entity's state, generates the
corresponding `INSERT`/`UPDATE`/`DELETE` statements, executes them (by default inside an implicit
transaction covering the whole batch), and — on success — resets each surviving entity back to
`Unchanged` and updates any store-generated values (identity columns, computed columns) on the
in-memory instances.

A `DbUpdateConcurrencyException` means a row EF Core expected to update or delete (based on a
concurrency token or the original values it cached) didn't match what's currently in the database —
someone else changed or deleted it first. A `DbUpdateException` wrapping a provider-specific
exception (e.g. a SQL Server unique-constraint violation) means the database rejected the generated
statement; inspect the inner exception for the actual constraint or type mismatch, since EF Core's
own message rarely names the failing column.

## ChangeTracker inspection

```csharp
var pendingChanges = dbContext.ChangeTracker.Entries()
    .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
    .ToList();
```

`ChangeTracker.Entries<T>()` filters to one entity type; `entry.OriginalValues` and
`entry.CurrentValues` expose the before/after snapshot for a `Modified` entry — useful for building
an audit log without hand-rolling change detection.

## AutoDetectChangesEnabled

EF Core scans every tracked entity for property changes automatically before each query and before
`SaveChanges` (`DetectChanges`). For a `DbContext` tracking a very large number of entities in a
tight loop, this automatic scan becomes measurable overhead. Setting
`dbContext.ChangeTracker.AutoDetectChangesEnabled = false` and calling
`dbContext.ChangeTracker.DetectChanges()` explicitly at the points that actually need it avoids the
redundant scans — but skipping detection entirely means a mutation you make won't be picked up by
`SaveChanges` until you call `DetectChanges()` or trigger it another way, so treat this as a
targeted optimization for a known hot path, not a default setting.
