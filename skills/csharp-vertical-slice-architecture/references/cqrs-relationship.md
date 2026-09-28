# VSA and CQRS: complementary, not the same idea

This distinction gets blurred constantly in practice, so it's worth stating precisely: **VSA is a
code-organization axis; CQRS is a command/query-separation axis.** They answer different
questions and neither requires the other.

## What each pattern actually is

- **Vertical Slice Architecture** answers: *how do I arrange files and folders in my codebase?*
  Its answer: by feature, not by technical layer. It says nothing about whether reads and writes
  should be modeled differently.
- **Command Query Responsibility Segregation (CQRS)** answers: *should the code path for changing
  data look different from the code path for reading data?* Its answer: yes — commands (which
  cause side effects) and queries (which return data without side effects) get separate models,
  separate types, and often separate handling logic, rather than a single service class with
  methods that both mutate and query. CQRS is about the read/write axis, at any level of
  granularity — it says nothing about how those command/query handlers are arranged in folders.

Neither pattern implies the other:

- You can slice by feature **without** CQRS: a slice's handler can freely read and write in the
  same method with a single combined request/response shape, exactly the way a layered service
  method often does. Nothing about "one folder per feature" forces a read/write split.
- You can apply CQRS **without** slicing by feature: a traditional layered codebase can still have
  a `Commands/` folder and a `Queries/` folder (or separate command/query service classes) while
  keeping every other technical-role folder (`Controllers/`, `Repositories/`) exactly as before.
  That's CQRS with zero vertical slicing.

## Why they're commonly combined in practice

They compose naturally because CQRS's command/query split happens to line up with the natural unit
of a slice: a command handler for one specific command is usually a complete, self-contained slice
(input DTO → validation/business logic → side effect, little or no return value beyond an
identifier/status), and a query handler for one specific query is likewise usually a complete,
self-contained slice (input DTO → data retrieval/shaping → response DTO, no side effects). Since
each slice in VSA is already scoped to exactly one use case, that use case is naturally *already*
either a pure command or a pure query in the overwhelming majority of real applications — so
adopting VSA tends to surface a CQRS-shaped seam even for teams that didn't set out to apply CQRS
deliberately.

This is why the two show up together so often in .NET material: a codebase organized as
`Features/CreateOrder/`, `Features/CancelOrder/`, `Features/GetOrderById/` is, by construction,
already segregating commands from queries at the folder level, even before anyone writes down
"we're doing CQRS." A handler-dispatch mechanism that distinguishes command messages from query
messages at the type level (common in .NET request/handler patterns) reinforces this further, but
the segregation exists in the folder structure regardless of what dispatch mechanism — if any — is
used to route requests to handlers.

## Where they genuinely diverge

- **CQRS without VSA** is common in codebases that want read/write separation (e.g., a
  denormalized read model vs. a normalized write model) but are not ready to restructure their
  entire folder layout around features — the read/write split can be layered in as
  `Commands/`/`Queries/` subfolders under an otherwise-unchanged layered structure.
- **VSA without CQRS** is common for CRUD-shaped features where read/write separation adds no
  value — a slice that both validates and immediately returns the persisted entity in one round
  trip doesn't need a distinct command model and query model; a single request/handler/response
  triad covering both concerns is simpler and loses nothing.
- **CQRS with a genuinely separate read model** (e.g., a dedicated read-optimized store or
  projection, distinct from the write-side aggregate) is an application-architecture decision
  about *data modeling*, independent of whether the code that populates or queries that read model
  is organized by feature or by layer.

## Practical guidance

When deciding how to structure a new codebase, treat these as two separate, orthogonal decisions:

1. **Folder organization**: features or layers? (VSA's question.)
2. **Request modeling**: one shape that both reads and writes, or separate command/query shapes?
   (CQRS's question.)

Most teams that adopt VSA end up answering "separate command/query shapes" for (2) simply because
it falls out naturally from slicing by use case — but that's a consequence of how most
applications' use cases are naturally shaped, not a rule VSA imposes. A slice is free to be neither
a pure command nor a pure query (e.g., an upsert-and-return-result operation) without violating
VSA in any way.
