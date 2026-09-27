---
name: csharp-cqrs
description: 'Guidance on Command Query Responsibility Segregation (CQRS) as a C#/.NET architectural pattern — separating the code path that changes data (a command, its handler, minimal return value) from the code path that reads data (a query, its handler, a DTO shaped for the caller), at whatever granularity a project actually needs. Covers the core command/query decision axis, writing a command and its write-model handler, writing a query and its read-model handler (including freely bypassing the domain model/repository abstractions for reads), the levels of CQRS from same-database segregation through a genuinely separate, denormalized read store, keeping a separate read store in sync (in-transaction projection vs. asynchronous, eventually-consistent projection via events), wrapping command/query handlers with cross-cutting behaviors (validation, logging, transactions, authorization) without polluting the handler itself, the CQRS-vs-event-sourcing distinction, when CQRS is worth adopting vs. when it is pure overengineering for a CRUD-shaped feature, and testing command handlers differently from query handlers. Use when deciding whether a feature needs separate command/query models, writing or reviewing a command or query handler, deciding how (or whether) to keep a separate read model in sync, distinguishing CQRS from event sourcing, or evaluating whether CQRS fits a given codebase at all. Tool-agnostic: describes handler dispatch, validation pipelines, and read-model projection generically and does not name or require any specific mediator, DI, validation, or ORM library.'
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# CQRS

Command Query Responsibility Segregation (CQRS) answers one question: **should the code path for
changing data look different from the code path for reading data?** Its answer is yes — a command
(an intent to change state, evaluated against business rules, producing a side effect and little or
no return value) and a query (a request for data, with no side effects, returning exactly the shape
a caller needs) get separate types and separate handling logic, instead of one service class whose
methods both mutate and query through the same model.

CQRS is a decision about **request modeling**, not about folder layout, a specific library, or event
sourcing. Everything below is described tool-agnostically — "a handler dispatch mechanism," "a
validation pipeline" — because the pattern itself doesn't require a specific mediator, DI container,
validation library, or ORM; apply it with whatever a given project already uses.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Deciding whether a feature needs CQRS at all, or picking which level of segregation fits | [references/core-concept-and-decision.md](references/core-concept-and-decision.md) |
| Writing or reviewing a command and its write-side handler | [references/commands-and-write-model.md](references/commands-and-write-model.md) |
| Writing or reviewing a query and its read-side handler | [references/queries-and-read-model.md](references/queries-and-read-model.md) |
| Deciding whether a read model needs its own store, and how to keep it in sync | [references/separate-read-models-and-synchronization.md](references/separate-read-models-and-synchronization.md) |
| Adding validation, logging, transactions, or authorization around handlers without bloating them | [references/cross-cutting-pipeline-behaviors.md](references/cross-cutting-pipeline-behaviors.md) |
| Testing a command handler vs. a query handler | [references/testing.md](references/testing.md) |

## Quick start

```csharp
// Command: an intent to change state. Minimal return — an identifier, or nothing.
public sealed record CreateOrderCommand(string CustomerId, IReadOnlyList<OrderLineDto> Lines);

public sealed class CreateOrderHandler(IOrderRepository repository)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Lines); // business rules live here
        await repository.AddAsync(order, ct);
        return order.Id;
    }
}

// Query: a request for data. No side effects, returns exactly the shape the caller needs.
public sealed record GetOrderSummaryQuery(Guid OrderId);
public sealed record OrderSummaryDto(Guid Id, string CustomerName, decimal Total, string Status);

public sealed class GetOrderSummaryHandler(IDbConnection connection)
{
    public Task<OrderSummaryDto?> HandleAsync(GetOrderSummaryQuery query, CancellationToken ct) =>
        connection.QuerySingleOrDefaultAsync<OrderSummaryDto>(
            "SELECT Id, CustomerName, Total, Status FROM OrderSummaries WHERE Id = @OrderId",
            new { query.OrderId });
}
```

`CreateOrderHandler` goes through the domain model (`Order.Create`) and a repository because writes
need business rules enforced. `GetOrderSummaryHandler` queries a flattened, denormalized shape
directly — no domain model, no repository — because reads need none of that. That asymmetry, freely
allowed once commands and queries are separated, is the entire payoff of CQRS.

## Out of scope

- **Event sourcing** — storing state as an append-only sequence of events and rebuilding current
  state by replaying them. CQRS is commonly paired with event sourcing but doesn't require it —
  [references/core-concept-and-decision.md](references/core-concept-and-decision.md) states the
  distinction; the event store, replay, and snapshotting mechanics of event sourcing itself are a
  separate, deep topic of their own.
- **Naming or depending on any specific mediator, DI, validation, or ORM library.** Every mechanism
  here (handler dispatch, validation pipelines, read-model projection) is described generically;
  apply it with whatever libraries a given project already uses.
- **Folder/project organization** (feature folders vs. technical layers) — CQRS is a request-modeling
  decision, orthogonal to how the resulting commands, queries, and handlers get arranged into
  folders or projects.
