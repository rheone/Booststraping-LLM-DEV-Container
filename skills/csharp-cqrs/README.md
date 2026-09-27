# CQRS

Guidance on Command Query Responsibility Segregation (CQRS): separating the code path that changes
data from the code path that reads it, at whatever level a feature actually needs — from a simple
type-level split through a fully separate, denormalized read store.

## When to reach for it

- You're deciding whether a feature actually needs separate command/query models, or whether that's
  overengineering for something genuinely CRUD-shaped.
- You're writing a command handler and unsure how much validation/business logic belongs in it vs.
  in the domain model it calls into.
- You're writing a query handler and wondering whether it's safe to bypass the domain model and
  repository abstractions entirely.
- You're deciding whether a read model needs its own store, and if so, how to keep it in sync with
  the write side.
- You keep hearing CQRS and event sourcing mentioned together and want to know whether one requires
  the other.

## Using it

This skill is model-invoked: it activates automatically when you're designing, writing, or
reviewing command/query handlers, or deciding whether CQRS fits a feature. You can also invoke it
directly by asking for it or typing `/csharp-cqrs`.

## What it covers

| Topic | Reference |
| --- | --- |
| The core command/query decision, the three levels of CQRS, when it's worth it vs. overengineering | [references/core-concept-and-decision.md](references/core-concept-and-decision.md) |
| Writing a command and its write-side handler, validation placement, transactional boundaries | [references/commands-and-write-model.md](references/commands-and-write-model.md) |
| Writing a query and its read-side handler, bypassing the domain model for reads | [references/queries-and-read-model.md](references/queries-and-read-model.md) |
| A separate read store and keeping it in sync — in-transaction vs. event-driven projection | [references/separate-read-models-and-synchronization.md](references/separate-read-models-and-synchronization.md) |
| Validation, logging, transactions, and authorization as pipeline behaviors around handlers | [references/cross-cutting-pipeline-behaviors.md](references/cross-cutting-pipeline-behaviors.md) |
| Testing a command handler vs. a query handler vs. an eventually-consistent read model | [references/testing.md](references/testing.md) |

## Example prompts

- "Does this 'update user profile' feature actually need separate command and query models, or am I
  overengineering it?"
- "Is it okay for my query handler to skip the repository and query the database directly?"
- "How do I keep a separate read-model table in sync with my write side without blocking every
  command on it?"
