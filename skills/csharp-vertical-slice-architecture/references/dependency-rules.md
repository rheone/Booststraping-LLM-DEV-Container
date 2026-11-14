# Dependency rules

## Pick the isolation level

Follow the repo. Read its architecture tests, analyzer configuration (`config.nsdepcop`), `ARCHITECTURE.md`,
and existing cross-references between feature folders. If the repo has no convention, suggest
**area-level sharing** and offer the other two levels.

| Level | Slice → other slice | Slice → own area's `Shared/` | Slice → other area | Fits |
| --- | --- | --- | --- | --- |
| **Strict** | no | no area `Shared/` exists | no | Large teams, modules on the way to separate services |
| **Area-level sharing** (suggested) | no | yes | no (go through domain, shared infrastructure, or events) | Most apps |
| **Relaxed** | may reuse another slice's request/response records and queries | yes | yes, read-only types | Small teams, early prototypes |

In every level: a handler never calls another handler, and nothing in shared code references a slice.

## Full matrix (area-level sharing)

Rows depend on columns. ✔ allowed, ✘ forbidden, ○ registration only.

| From \ To | Same slice | Other slice | Own area `Shared/` | Other area | Domain | Shared kernel | Shared infrastructure | Host |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slice | ✔ | ✘ | ✔ | ✘ | ✔ | ✔ | ✔ | ✘ |
| Area `Shared/` | ✘ | ✘ | ✔ | ✘ | ✔ | ✔ | ✔ | ✘ |
| Domain | ✘ | ✘ | ✘ | ✘ | ✔ | ✔ | ✘ | ✘ |
| Shared kernel | ✘ | ✘ | ✘ | ✘ | ✘ | ✔ | ✘ | ✘ |
| Shared infrastructure | ✘ | ✘ | ✘ | ✘ | ✔ | ✔ | ✔ | ✘ |
| Host / composition root | ○ | ○ | ○ | ○ | ✔ | ✔ | ✔ | ✔ |
| Test project | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |

Notes:

- **Slice → shared infrastructure** is disputed in the sources. Bogard's ContosoUniversity and Wolverine
  have handlers use the `DbContext` directly. The ardalis `min-clean` template forbids slices from
  depending on infrastructure and routes them through domain-defined repositories. Which one applies is a
  data-access decision; see [data-access.md](data-access.md). When the repo forbids it, the slice depends
  on interfaces in the domain (or a unit-of-work abstraction), and infrastructure implements them.
- **Domain → infrastructure** is forbidden in every level. Entity configuration (EF Core
  `IEntityTypeConfiguration<T>`, NHibernate mappings) lives in infrastructure, not on the entity.
- **Shared infrastructure knows no slice.** A pipeline step that checks `request is CreateOrderRequest`
  is a slice concern placed in the wrong spot. Use metadata, marker interfaces, or slice-local filters.
- **The host** composes. It calls each area's `MapXxx` extension or scans for registrations. It holds no
  business logic.

## Cross-slice communication

When one use case needs something another use case also needs, prefer the options in this order:

| Option | Use when | Cost |
| --- | --- | --- |
| 1. Duplicate the small piece | It is a few lines, or the two copies may diverge | Two places to update |
| 2. Promote to area `Shared/` | Two slices in one area share a proven, identical rule | Coupling inside the area only |
| 3. Promote to the domain | The logic is a business rule about an entity | Domain grows; keep it behavior, not queries |
| 4. Shared query (method on `DbContext`, extension on `IQueryable<T>`, read-model service) | Several areas need the same read | A hidden service layer if it grows; keep each query small and named |
| 5. In-process domain event or notification | A write in one slice causes a side effect another area owns | Transaction scope must be chosen; harder to trace |
| 6. Integration event through a broker / outbox | The side effect is in another module or service, or must survive a crash | Eventual consistency, idempotent consumers, outbox |

Avoid: one handler calling another handler, directly or through a mediator `Send`. Bogard: "I'm not a fan
of this approach – it gets quite confusing" (2016), and "I don't ever see `Send` outside of a
controller/page" (2022). If two edges need the same use case, they both call the same handler; the use
case is one slice with two edges.

Exception seen in Microsoft's eShop sample: an `IdentifiedCommandHandler` re-sends the inner command to
add idempotency. That is a pipeline concern implemented as a wrapper, not one use case calling another.

## Allowed references during migration

While layered code still exists (see the migration workflow):

- Legacy code never references Features.
- Whether a slice may call a legacy service is a per-migration decision. When allowed, list every such
  call in the migration log and in an architecture-test allow-list, so the debt shrinks visibly.

## Enforcing the rules

Rules without enforcement erode. Offer the user one of these, using a library the repo already has when
there is one:

- Architecture tests in a test project ([architecture-tests.md](architecture-tests.md)).
- A build-time analyzer such as NsDepCop, which fails the build on an illegal namespace dependency.
- Assembly boundaries plus `internal` (Layouts D, E, F in [project-layouts.md](project-layouts.md)), which
  the compiler enforces.
