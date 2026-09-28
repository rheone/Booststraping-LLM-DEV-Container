# Core Concept and Motivation

## The problem

A single business operation frequently touches more than one kind of data at once: placing an
order reserves inventory, records the order itself, and writes an audit entry. If each of those
three writes commits independently, a failure after the second write leaves the store in a state no
business rule ever intended — inventory reserved for an order that was never recorded. The store
has no way to know those three writes were supposed to be one operation, because nothing told it
they were related.

## The pattern

Unit of Work tracks every change a business operation makes — inserts, updates, and deletes, across
however many repositories or data-access objects are involved — and defers all of them until a
single, explicit commit point. That commit point either applies every tracked change or applies
none of them. The pattern gives you exactly one place to say "this operation is done, make it
durable" instead of one commit call per write.

Concretely, a unit of work:

- Exposes access to the repositories a caller needs, all sharing one underlying connection or
  session so their writes participate in the same transaction.
- Accumulates the changes those repositories make without touching the store yet.
- Commits everything through a single method (`SaveChanges`, `Commit`, `CompleteAsync` — the name
  varies, the one-call contract doesn't).
- Rolls back cleanly if the commit fails partway through, leaving the store exactly as it was
  before the operation started.

## When you need it

You need a unit of work when a single logical operation writes through more than one repository
and those writes must succeed or fail together. A handler that only ever touches one repository
and calls its own single-entity save method has no coordination problem to solve — introducing a
unit of work there adds a layer with nothing to coordinate.

You do not need a unit of work merely because a method reads from several repositories — read-only
fan-out has no atomicity requirement, since nothing is being committed. The pattern is about write
coordination specifically.

## When you don't need to build one yourself

A data-access context that already tracks pending changes across every entity it manages, and
flushes them all on one save call, already behaves as a unit of work — see
[change-tracking-contexts-as-unit-of-work.md](change-tracking-contexts-as-unit-of-work.md) for how
to recognize this and avoid wrapping a redundant abstraction around it.
