# Change-Tracking Contexts as Unit of Work

## The observation

A data-access context that tracks every entity it has loaded or been given, records what changed
about each one, and applies all of those changes to the store on a single save call is already a
unit of work — not "similar to one," structurally *is* one. It exposes the same shape the pattern
calls for:

- Accumulates changes across however many entities and entity types a caller touches, without
  writing anything to the store yet.
- Commits everything through one method call.
- Rolls back cleanly (typically by simply not calling that method, or by disposing the context) if
  the operation is abandoned partway through.

```csharp
context.Orders.Add(new Order(customerId, lineItems));
context.InventoryItems.Update(reservedItem);
context.AuditEntries.Add(new AuditEntry("OrderPlaced", orderId));

context.SaveChanges(); // one call, every tracked change commits together
```

This is the same shape as the `IUnitOfWork.SaveChanges()` call in
[unit-of-work-interface-and-repositories.md](unit-of-work-interface-and-repositories.md) — the
context itself is playing the unit-of-work role, and its per-entity-type accessors
(`context.Orders`, `context.InventoryItems`) are playing the coordinated-repository role.

## When wrapping it adds nothing

Introducing a separate `IUnitOfWork` class whose only job is to hold a reference to a change-tracking
context and forward `SaveChanges()` to it adds a layer of indirection with no new capability behind
it — the context was already exposing exactly that contract. This is a redundant wrapper, not a
meaningful abstraction, whenever every method on the wrapper does nothing but delegate to the
context's own equivalent member.

```csharp
// Adds nothing the context didn't already provide:
public sealed class RedundantUnitOfWork : IUnitOfWork
{
    private readonly SomeChangeTrackingContext _context;
    public RedundantUnitOfWork(SomeChangeTrackingContext context) => _context = context;
    public int SaveChanges() => _context.SaveChanges();
}
```

## When wrapping it still earns its place

A thin abstraction over a change-tracking context is still worth introducing when it does real work
beyond forwarding the save call — coordinating more than one context (two separate stores), adding
cross-cutting behavior around every commit (publishing domain events queued during the operation,
centralized logging of what changed), or giving test code a narrower seam to fake than the full
context's surface. The test for whether the wrapper is worth it is the same either way: does this
type do something the thing it wraps doesn't already do?

## Practical implication

Before designing a bespoke `IUnitOfWork`/`IRepository<T>` layer over a change-tracking context,
check whether the context's own save call and per-entity-type accessors already satisfy the
coordination requirement. Building the pattern's plumbing a second time on top of a context that
already implements it is the most common way this pattern gets over-applied.
