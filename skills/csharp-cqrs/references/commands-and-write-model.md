# Commands and the Write Model

A command is a data object naming an **intent** — "create this order," "cancel this subscription"
— evaluated against business rules by a handler that then causes a side effect. Everything about a
command's shape follows from that intent, not from whatever columns happen to exist in a table.

## Shaping a command

```csharp
public sealed record CreateOrderCommand(string CustomerId, IReadOnlyList<OrderLineDto> Lines);
public sealed record CancelOrderCommand(Guid OrderId, string Reason);
```

Name a command as a verb phrase describing the intent (`CreateOrderCommand`, not `OrderCommand` or
`SaveOrderCommand`) — a command represents one specific thing a caller is asking to happen, not a
generic "do something to this entity" bucket. Include only the fields the operation actually needs
to decide whether it's valid and what to do — not every field the underlying entity happens to
have.

## The handler

```csharp
public sealed class CreateOrderHandler(IOrderRepository repository)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Lines);
        await repository.AddAsync(order, ct);
        return order.Id;
    }
}
```

The handler's job is to load whatever state it needs, ask the domain model to enforce its own
invariants (`Order.Create` rejecting an empty line list, a negative quantity, an unknown customer),
and persist the result — the handler itself stays a thin orchestration layer, not a place where
business rules get re-implemented inline. Prefer pushing "is this transition allowed" logic onto the
domain model over scattering `if` checks through the handler.

## What a command handler returns

Keep the return value minimal — an identifier for something just created, a status/result
indicator, or nothing (`Task`, not `Task<T>`) for an operation with no natural output. Resist
returning the full, freshly-loaded entity from a command handler "for convenience" — that pulls a
read-shaped concern back into the write path and is exactly the coupling CQRS exists to avoid. A
caller that needs to display the created order's details issues a query for it afterward, using
whatever identifier the command returned.

## Validation and where it belongs

Two validation concerns exist on the write side, and conflating them is a common mistake:

- **Input validation** — is the command itself well-formed (required fields present, formats
  valid)? This can run before the handler even executes, as a pipeline step — see
  [cross-cutting-pipeline-behaviors.md](cross-cutting-pipeline-behaviors.md).
- **Business rule validation** — given the command is well-formed, is this specific transition
  actually allowed right now (can this order be canceled in its current status, does this customer
  have enough remaining balance)? This depends on loaded state and belongs inside the handler or the
  domain model it calls into, not in a generic input-validation pipeline step that has no access to
  that state.

## Transactional boundary

A command handler is typically also the transactional boundary — the unit of work that either
fully commits or fully rolls back. When a command handler needs to update more than one aggregate,
or persist a projection change alongside the primary write (see
[separate-read-models-and-synchronization.md](separate-read-models-and-synchronization.md)), that
work happens inside the same transaction the handler manages, not as an afterthought once the
handler has already returned.
