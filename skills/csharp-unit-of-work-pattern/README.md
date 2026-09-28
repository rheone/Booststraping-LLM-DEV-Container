# Unit of Work Pattern

You track a set of changes made across multiple repository or data-access operations in memory and
commit them to a store as one atomic operation. It covers an `IUnitOfWork` interface coordinating
several repositories against a shared connection, ambient versus explicit transactions, rollback on
partial failure, and recognizing when a change-tracking data-access context already gives you the
pattern.

## When to reach for it

- A single business operation needs to write to more than one repository and you need those writes
  to succeed or fail together.
- You're designing or reviewing an `IUnitOfWork` abstraction and want to get the coordination
  contract right.
- You're deciding between an ambient transaction and one your code opens and commits explicitly, or
  need a rollback strategy for a partial failure.
- You already have a change-tracking data-access context and want to know whether you need a
  bespoke unit-of-work class layered on top of it at all.

## Using it

This skill is model-invoked: it fires automatically when your prompt matches its situation, such as
coordinating multiple repository writes or reviewing transaction handling. You can also invoke it
directly as `/csharp-unit-of-work-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The problem and when you need the pattern | [references/core-concept-and-motivation.md](references/core-concept-and-motivation.md) |
| An `IUnitOfWork` interface coordinating multiple repositories | [references/unit-of-work-interface-and-repositories.md](references/unit-of-work-interface-and-repositories.md) |
| A reusable, type-parameterized unit-of-work base | [references/generic-unit-of-work.md](references/generic-unit-of-work.md) |
| Ambient vs. explicit transactions, and rollback on failure | [references/transactions-ambient-vs-explicit.md](references/transactions-ambient-vs-explicit.md) |
| Recognizing a change-tracking context as already a unit of work | [references/change-tracking-contexts-as-unit-of-work.md](references/change-tracking-contexts-as-unit-of-work.md) |
| Testing code that depends on `IUnitOfWork` | [references/testing.md](references/testing.md) |
| Adding a new repository or atomic operation | [references/extending.md](references/extending.md) |

## Example prompts

- "I'm updating an `Order` and decrementing `InventoryItem` stock in the same operation. How do I
  make sure both writes commit together or neither does?"
- "Should my `IUnitOfWork` open its own transaction, or rely on an ambient `TransactionScope`?"
- "My data context already tracks changes and flushes them on `SaveChanges`. Do I actually need a
  separate unit-of-work class on top of that?"
