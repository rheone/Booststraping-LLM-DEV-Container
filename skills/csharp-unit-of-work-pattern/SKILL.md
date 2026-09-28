---
name: csharp-unit-of-work-pattern
description: Guidance on the Unit of Work design pattern in C# — tracking a set of changes made across multiple repository/data-access operations in memory and committing them to a store as one atomic operation, an IUnitOfWork interface coordinating several IRepository<T> instances against a shared connection/session, the relationship between a unit of work and an ambient transaction (TransactionScope) versus an explicit one it opens and commits itself, rollback on partial failure, and the common real-world observation that a change-tracking data-access context (an object graph that queues inserts/updates/deletes and flushes them on a single save call) is itself already a unit of work. Use when designing a data-access layer that spans multiple repositories, deciding how to make several writes succeed or fail together, reviewing or writing an IUnitOfWork abstraction, or explaining why a change-tracking context doesn't need a bespoke unit-of-work class layered on top of it. Described generically — does not name or require any specific ORM or persistence library.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Unit of Work Pattern

Unit of Work is a **design pattern**, not a package — there is nothing to install and no version to
pin. This skill documents the pattern itself: what problem it solves, how to shape an
`IUnitOfWork` abstraction that coordinates multiple repositories, how it relates to a transaction,
and when a change-tracking data-access context already gives you the pattern for free.

Everything below is described **generically**: "a data-access context," "a repository," "a
store" — never naming a specific ORM or persistence library — because the pattern's shape doesn't
depend on which one a given project uses.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Understanding what problem Unit of Work solves and when you need it | [references/core-concept-and-motivation.md](references/core-concept-and-motivation.md) |
| Designing an `IUnitOfWork` interface that coordinates multiple repositories | [references/unit-of-work-interface-and-repositories.md](references/unit-of-work-interface-and-repositories.md) |
| Writing a reusable, type-parameterized unit-of-work base | [references/generic-unit-of-work.md](references/generic-unit-of-work.md) |
| Deciding between an ambient transaction and an explicit one, or handling rollback on partial failure | [references/transactions-ambient-vs-explicit.md](references/transactions-ambient-vs-explicit.md) |
| Recognizing that a change-tracking data-access context already is a unit of work | [references/change-tracking-contexts-as-unit-of-work.md](references/change-tracking-contexts-as-unit-of-work.md) |
| Testing code that depends on `IUnitOfWork` | [references/testing.md](references/testing.md) |
| Adding a new repository or a new kind of atomic operation without breaking callers | [references/extending.md](references/extending.md) |

## Quick start

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;
    int SaveChanges();
}
```

A caller pulls whichever repositories it needs from the same `IUnitOfWork` instance, mutates them,
and calls `SaveChanges()` exactly once:

```csharp
using IUnitOfWork unitOfWork = unitOfWorkFactory.Create();

IRepository<Order> orders = unitOfWork.Repository<Order>();
IRepository<InventoryItem> inventory = unitOfWork.Repository<InventoryItem>();

orders.Add(new Order(customerId, lineItems));
inventory.Update(reservedItem);

unitOfWork.SaveChanges(); // both writes commit together, or neither does
```

The two writes above touch two different repositories but share one save call — that's the whole
point of the pattern. Start with
[references/core-concept-and-motivation.md](references/core-concept-and-motivation.md) for why that
matters, then [references/unit-of-work-interface-and-repositories.md](references/unit-of-work-interface-and-repositories.md)
for the coordination mechanics.

## Out of scope

- Naming or depending on any specific ORM, database driver, or persistence library. The pattern is
  described generically; apply it with whatever data-access technology a given project already
  uses.
- Distributed transactions across multiple independent databases or services (two-phase commit,
  saga orchestration) — this skill covers a unit of work committing to a single logical store.
- General repository design beyond what a unit of work needs from it (a narrow `Add`/`Update`/
  `Remove`/query surface) — the shape of an individual repository's query methods is a separate
  concern from how several of them commit together.
