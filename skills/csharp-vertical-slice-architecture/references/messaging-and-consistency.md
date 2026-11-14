# Messaging and consistency

## Domain events vs. integration events

| | Domain event | Integration event |
| --- | --- | --- |
| Scope | In process, one app | Across modules, services, or processes |
| Raised by | An entity (`order.Cancel()` adds `OrderCancelled`) | A slice or an outbox processor after commit |
| Handled by | Handlers in other areas of the same app | Consumers (which are slices) in other modules/services |
| Transport | In memory (dispatcher, mediator notification) | Broker, queue, or in-process bus for a modular monolith |
| Consistency | Same transaction if dispatched before commit | Eventual |
| Lives in | `Domain/<Aggregate>/Events/` | A contracts project or `Features/<Area>/Contracts/` |

Use domain events to make side effects across aggregates explicit. Dispatching them right before
`SaveChanges` keeps every change in one transaction; dispatching after commit gives eventual consistency
(<https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation>).
The ardalis `min-clean` template dispatches from an EF Core `SaveChanges` interceptor.

## Domain events in a sliced app

```csharp
// Domain: the entity records events.
public abstract class Entity
{
    private readonly List<IDomainEvent> _events = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;
    protected void Raise(IDomainEvent e) => _events.Add(e);
    public void ClearDomainEvents() => _events.Clear();
}

public sealed record OrderCancelled(Guid OrderId, DateTimeOffset At) : IDomainEvent;
```

- Handlers for a domain event live in the area that reacts: `Features/Billing/RefundOnOrderCancelled/`.
  Each handler is a slice whose edge is the event.
- Dispatch from shared infrastructure (a `SaveChanges` interceptor, an NHibernate event listener, or a
  unit-of-work commit), never from the raising slice.
- Choose before-commit (one transaction, the handler can fail the command) or after-commit (eventual, the
  command succeeds regardless) per repo, and apply it uniformly.

## Outbox

Writing to the database and publishing to a broker are two resources. The outbox makes them one: the
slice writes the message as a row in the same transaction as the entity changes, and a background
processor publishes committed rows. Azure's CQRS guidance names the transactional outbox and idempotent
consumers for events published from writes (<https://learn.microsoft.com/azure/architecture/patterns/cqrs>).

```csharp
order.Cancel(clock.GetUtcNow());
outbox.Add(new OrderCancelledIntegrationEvent(order.Id, order.CustomerId));   // a row in the same DbContext
await db.SaveChangesAsync(ct).ConfigureAwait(false);                         // one commit
```

- The outbox table and its processor are shared infrastructure.
- If the repo uses a messaging library with a built-in outbox, use it (standing rule 2).
- The processor delivers at least once. Consumers must be idempotent.

## Consumers as slices

A message consumer is an edge, like an HTTP endpoint. Its slice has a message record, a handler, and a
mapping from outcome to acknowledge, retry, or dead-letter. See [entry-points.md](entry-points.md).

## Idempotency

| Edge | Mechanism |
| --- | --- |
| HTTP command | `Idempotency-Key` header; a filter or step stores key + response; a repeat returns the stored response |
| Message consumer | Store processed message IDs in the same transaction as the handler's changes; skip a repeat |
| Natural idempotency | Design the command so repeating it changes nothing (`SetStatus(Shipped)` rather than `Increment`) |

Prefer natural idempotency. Add the key mechanism only to slices that need it, through endpoint metadata.

## Concurrency

- Use optimistic concurrency tokens (`[Timestamp]` / `IsRowVersion()` in EF Core; `<version>` in
  NHibernate) on aggregates that two users can edit at once.
- A command slice that edits such an aggregate accepts the expected version in its request and returns a
  conflict outcome (409) when it does not match. Catching the ORM's concurrency exception at that one
  point and returning an outcome is the exceptional-to-expected boundary: the exception stays inside the
  handler.
- Queries return the current version so clients can send it back.

## Transactions across slices

A slice never commits work that belongs to another slice. When one use case must change two aggregates
atomically, it is one slice that changes both (through domain methods), or it is a domain event handled
before commit. When atomicity across modules is needed, redesign toward eventual consistency with
integration events, or move the boundary.
