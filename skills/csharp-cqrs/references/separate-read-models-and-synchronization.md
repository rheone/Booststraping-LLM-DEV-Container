# Separate Read Models and Synchronization

Level 3 of CQRS — a read side served from its own store, distinct from the write side's — trades
simplicity for a read model that can be shaped, indexed, and scaled completely independently of the
write side's schema. That trade only pays off once the read and write sides' shapes or load
profiles have genuinely diverged; see
[core-concept-and-decision.md](core-concept-and-decision.md) for when that's actually true. This
file covers how to keep the two sides in sync once a separate read store is warranted.

## In-transaction projection: simplest, strongly consistent

```csharp
public sealed class CreateOrderHandler(IOrderRepository repository, IOrderSummaryProjector projector)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Lines);
        await repository.AddAsync(order, ct);          // write side
        await projector.UpsertAsync(order, ct);         // read side, same transaction
        return order.Id;
    }
}
```

The command handler updates both the write-side aggregate and the read-side projection inside the
same transaction (or unit of work), so a caller who reads immediately after a successful command
always sees the update. This is strongly consistent and the simplest synchronization strategy
available, at the cost of coupling the write transaction to the read store's availability and
latency — a slow or unavailable read store now blocks every write.

## Asynchronous projection via events: eventually consistent

```csharp
public sealed class CreateOrderHandler(IOrderRepository repository, IEventPublisher publisher)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Lines);
        await repository.AddAsync(order, ct);
        await publisher.PublishAsync(new OrderCreated(order.Id, order.CustomerId, order.Total), ct);
        return order.Id;
    }
}

// A separate consumer, running independently of the command's request/response cycle.
public sealed class OrderSummaryProjectionHandler(IOrderSummaryProjector projector)
{
    public Task HandleAsync(OrderCreated @event, CancellationToken ct) =>
        projector.UpsertAsync(@event.OrderId, @event.CustomerId, @event.Total, ct);
}
```

The command handler publishes an event describing what happened and returns without waiting for the
read side to update — a separate consumer, running on its own schedule, applies the projection
afterward. This decouples the write path's latency and availability from the read store entirely,
at the cost of a window (typically milliseconds to seconds, depending on the messaging
infrastructure) during which the read side hasn't caught up yet.

## Living with eventual consistency

- **The caller that just wrote often needs to see its own write immediately** — a common pattern is
  to have the command's response carry enough data for the UI to render the just-created/updated
  state directly (the command handler already has the domain object in hand), rather than
  immediately re-querying a read model that may not have caught up yet.
- **Idempotent projection handlers** — a projection consumer should tolerate receiving the same
  event more than once (message redelivery, retries) without corrupting the read model; an upsert
  keyed by the entity's identifier, rather than an unconditional insert, handles this naturally.
- **Rebuilding a read model from scratch** — because the read store is a derived projection, not a
  source of truth, it should always be rebuildable by replaying the relevant write-side data or
  event history. Treat the ability to drop and repopulate a read store as a basic operational
  requirement of adopting this level of CQRS, not an edge case.
- **Testing implications** — an integration test asserting on the read side after issuing a command
  needs to account for the synchronization strategy: poll or await the projection for the
  asynchronous case rather than asserting immediately, since asserting right after publishing an
  event races the projection consumer. See [testing.md](testing.md).

## Deciding you don't need this yet

If the honest answer to "what would break if the read model briefly lagged behind the write model by
a few seconds" is "nothing a user would notice," that's a strong signal level 2 (same database,
different projection) already delivers everything this level would, without taking on eventual
consistency, a second store to operate, or synchronization failure modes to handle.
