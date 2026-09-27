# Core Concept and Decision

CQRS segregates two responsibilities that a traditional CRUD-shaped service class usually mixes
into one model: **causing a side effect** (a command) and **answering a question with no side
effect** (a query). Once separated, each side is free to be shaped, validated, and optimized for
what it actually does, instead of both being forced through one shared representation of "the
entity."

## The three levels of CQRS, from lightest to heaviest

1. **Same model, split by intent.** A single service class's `CreateOrder(...)`/`GetOrder(...)`
   methods become a `CreateOrderCommand`/`CreateOrderHandler` pair and a
   `GetOrderQuery`/`GetOrderHandler` pair, still reading and writing the same tables through the
   same domain model. The only change is that command and query are now distinct types with
   distinct handlers — no new infrastructure, no new data store. This is CQRS's entire value for
   the overwhelming majority of features: a clear, type-level signal of which operations mutate
   state and which don't.
2. **Same database, different model per side.** Commands still go through the domain model and its
   invariants; queries bypass the domain model and repository abstractions entirely, querying a
   flattened, denormalized projection of the same underlying tables (a SQL view, a set of `.Select()`
   projections) shaped exactly like what the caller needs. No second database — just two different
   ways of reading the one database that already exists.
3. **Separate read store.** The read side is served from its own store — a different database
   engine, a cache, a search index — populated by projecting write-side changes into it, either
   synchronously in the same transaction or asynchronously via events. This is the heaviest level,
   introduces eventual consistency between write and read sides, and is covered in
   [separate-read-models-and-synchronization.md](separate-read-models-and-synchronization.md).

Nothing requires jumping straight to level 3. Most applications get CQRS's real benefit — commands
and queries no longer forced through one shared model — at level 1 or 2, with no additional
infrastructure and no consistency tradeoff at all.

## When CQRS earns its place

- A feature has meaningfully different **shapes** on the read and write side — writes accept a
  small, validated input and enforce cross-field business rules; reads need a wide, denormalized,
  UI-ready projection joining several aggregates. Forcing both through one model means either the
  write side carries read-only convenience properties it doesn't need, or the read side pays for
  loading a full aggregate graph just to display a few fields.
- Read and write **load profiles** genuinely diverge — a feature is read thousands of times per
  write, and the read query benefits from caching, a search index, or a different data store that
  the write side has no reason to share.
- The domain has **non-trivial business rules** on the write side (state transitions, cross-field
  validation, invariants that must hold before persisting) that a query has no business enforcing
  or even loading enough data to check.

## When CQRS is overengineering

- A feature is genuinely CRUD-shaped: create/read/update/delete a single record with no invariants
  beyond "the required fields are present." Splitting `CreateThing`/`GetThing`/`UpdateThing` into
  separate command/query types here adds a type and a handler for each operation without removing
  any complexity — the split exists to manage complexity that isn't present yet.
- A team reaches for level 3 (a separate read store) before load or shape divergence actually
  demands it. Eventual consistency is a real cost — a caller who just issued a command can briefly
  read stale data from the projection — and paying that cost for a feature with no genuine
  read/write asymmetry buys nothing back.

The decision to adopt CQRS, and at which level, is made **per feature**, not once for an entire
application — a codebase can have some slices at level 1 and others at level 3 depending on each
one's actual read/write asymmetry.

## CQRS vs. event sourcing

CQRS and event sourcing are frequently adopted together but answer different questions. CQRS asks
"should reads and writes use different models?" Event sourcing asks "how is the write side's state
persisted?" — as an append-only log of events rather than as current-state rows, with current state
derived by replaying events. A codebase can apply CQRS with a perfectly ordinary current-state
write-side database (the common case) or pair it with event sourcing for the write side while still
projecting a denormalized read model from the resulting event stream (a common combination, but a
separate decision with its own consistency, replay, and snapshotting concerns not covered here).
