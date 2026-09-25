# Cross-cutting concerns without re-layering

Slices maximize independence, but no real application wants every slice to reinvent validation,
logging, error handling, and persistence access from scratch. VSA handles this without collapsing
back into a shared service layer that every feature is forced to route through — the goal is
sharing *infrastructure*, not sharing *business logic*.

## The distinction that keeps VSA from becoming layered again

The failure mode to avoid is recreating a `Services/` layer under a different name: a "shared"
folder that accumulates business rules multiple slices are forced to call through, re-coupling
slices to each other via that shared code. The pattern below keeps sharing scoped to genuinely
cross-cutting, feature-agnostic concerns — the kind of thing every slice needs but none of them
should have an opinion about — while leaving business logic in the slice that owns it.

Signals a piece of code is genuinely cross-cutting (safe to share): it doesn't know what a
"customer" or an "order" is, and it never contains an `if` branch driven by which feature is
calling it. A retry policy, a timing/logging wrapper, and a validation *engine* (as opposed to a
particular feature's validation *rules*) all pass this test.

## Common cross-cutting concerns and how they're typically shared

- **Validation** — the *rules* for one request type live in that slice (`CreateOrderRequest`'s
  validation rules belong in `CreateOrder/`, not in a shared file). The *mechanism* that runs
  validation before a handler executes — discovering the right validator for a request type and
  invoking it — is shared infrastructure. This is often implemented as a small pipeline step that
  wraps handler invocation: run validation, short-circuit with an error response on failure,
  otherwise call the handler.
- **Logging / telemetry** — wrapping every handler invocation with timing and structured logging
  is pure cross-cutting concern: it doesn't need to know anything about what a given slice does,
  just that it started, finished, and how long it took. Implemented once, applied to every slice
  uniformly.
- **Error handling / result shaping** — converting a thrown exception or a failure result into a
  consistent HTTP response shape is another concern no individual slice should reimplement.
  Handled once, at the boundary between the dispatch mechanism (or the entry point) and the
  outside world.
- **Persistence context** — the `DbContext`/connection/unit-of-work object itself is shared
  infrastructure (there's one database), but *how* a given slice queries or writes through it is
  the slice's own business — see [data-access-patterns.md](data-access-patterns.md) for the
  query-object-vs-repository tradeoff this raises.
- **Cross-slice domain invariants** — rules that must hold regardless of which slice is mutating an
  entity (e.g., "an order's total can never be negative") belong on the domain type itself
  (`Order.Create(...)`, `Order.AddLine(...)`) rather than duplicated into every slice's handler.
  This isn't really "cross-cutting infrastructure" so much as recognizing that the domain model is
  legitimately shared — slices share the entities they operate on; they don't have to share the
  code that orchestrates each use case around those entities.

## Two structural mechanisms for sharing

- **Minimal shared base abstractions** — small, narrow interfaces or base types that a handler
  optionally implements to opt into shared behavior (for example, an interface a request type
  implements to mark itself as validatable, which a shared pipeline step checks for). Kept
  intentionally thin: a marker interface or a one-method contract, not a base class with virtual
  methods every handler is expected to override.
- **Pipeline-style composition** — cross-cutting behavior expressed as a chain of steps that wrap
  the handler invocation (validate → log → invoke handler → log result), where each step is
  independent of any specific slice and slices opt in simply by having a request type. This mirrors
  middleware-style composition generically: each concern is one small piece that knows nothing
  about the others or about any individual slice's business rules.

Both mechanisms deliberately avoid a **shared "kernel" folder that grows into a de facto services
layer** — see [pitfalls.md](pitfalls.md#shared-kernel-creep) for what that looks like in practice
and how to catch it before it re-couples the whole codebase.

## A practical boundary test

Before adding something to shared/cross-cutting code, ask: *if every slice that currently calls
this were deleted except one, would this code still make sense on its own, with zero changes?* A
validation pipeline step passes (it doesn't know or care which slice invoked it). A method named
`ProcessOrder` that branches on an order's status fails (it's really one slice's logic wearing a
shared name) and belongs back inside the slice that needs it.
