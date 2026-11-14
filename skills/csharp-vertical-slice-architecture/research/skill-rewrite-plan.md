# Plan: rewrite `csharp-vertical-slice-architecture`

Status: **approved 2026-11-13, implemented as 2.0.0**. Kept as the design record. Open decisions are marked `DECIDE`. They are being settled in a one-question-at-a-time
interview. Facts marked `VERIFY` wait on
[vertical-slice-architecture-dotnet-research.md](vertical-slice-architecture-dotnet-research.md).

## 1. Diagnosis of the current skill (v1.0.0)

- `SKILL.md` + 8 reference files, about 740 lines. All of it is reference. There are no steps, so an agent
  asked to "add a slice" or "migrate this project" must invent its own process each run.
- Missing topics: bootstrapping a project, adding a slice, migration steps, dependency rules ("what may
  touch what"), architecture tests, layout options, comparisons with Clean/Onion/Hexagonal/N-tier, record
  DTO conventions, concrete validation and error-handling shapes, where domain/services/data sources live.
- It has no `evals/`, so changes cannot be measured.
- Fully tool-agnostic. This makes bootstrapping steps and architecture-test code hard to write, because
  both need concrete APIs (`dotnet new`, an architecture-test library, a test host). See `DECIDE-1`.

## 2. Constraints from this repo

- This repo's C# skill-authoring rules: insularity (no naming of other skills unless a stated dependency),
  present tense / active voice / second person, a testing reference in every skill, `SKILL.md` is a router,
  `README.md` is human-facing.
- `writing-for-agents` rules: steps end on a checkable completion criterion; disclose what only some
  branches need; use leading words; state the positive target, not the ban.
- Root `.editorconfig`: LF, UTF-8, trailing newline. markdownlint (MD013 off), codespell.

## 3. Branches the skill must serve

The description triggers on these branches, and `SKILL.md` routes each one:

| Branch | Content type | Target file |
| --- | --- | --- |
| Design: "how should I lay out this feature/project?" | reference | `project-layouts.md`, `slice-anatomy.md` |
| Bootstrap a new VSA project | steps | `workflow-bootstrap-project.md` |
| Add a new slice | steps | `workflow-add-slice.md` |
| Migrate a layered project to VSA | steps | `workflow-migrate.md` |
| Review/audit an existing VSA codebase | steps + checklist | `workflow-review.md`, `code-smells.md` |
| Explain or compare (VSA vs. Clean/Onion/…) | reference | `comparisons.md` |

## 4. Proposed file tree

```text
csharp-vertical-slice-architecture/
├── SKILL.md                         # router: leading words, the 5 dependency rules, routing table
├── README.md                        # human-facing
├── references/
│   ├── core-principles.md           # definition, coupling axis, leading words, when VSA fits
│   ├── project-layouts.md           # layout options A–E with trees and trade-offs
│   ├── slice-anatomy.md             # request/response records, handler, endpoint, validator, flow
│   ├── dependency-rules.md          # "what may touch what" matrix, cross-slice communication
│   ├── patterns.md                  # mediator (generic), CQRS, pipeline/decorator, filters, REPR
│   ├── shared-logic.md              # shared kernel, domain, services, infrastructure, promotion rule
│   ├── data-access.md               # DbContext as unit of work, repository trade-off, query per slice
│   ├── validation-and-errors.md     # input validation vs. invariants, Result vs. exceptions, ProblemDetails
│   ├── cross-cutting-concerns.md    # logging, auth, transactions, caching, observability
│   ├── comparisons.md               # Clean, Onion, Hexagonal, N-tier, monolith, modular monolith
│   ├── code-smells.md               # smells, symptoms, refactor
│   ├── testing.md                   # unit, integration, test layout
│   └── architecture-tests.md        # rule catalog + example code
├── workflows/
│   ├── workflow-bootstrap-project.md
│   ├── workflow-add-slice.md
│   ├── workflow-migrate.md
│   └── workflow-review.md
├── assets/templates/                # DECIDE-6
│   ├── slice.cs.tmpl
│   └── ArchitectureTests.cs.tmpl
├── research/                        # not loaded by the skill
└── evals/                           # skill-creator test cases
```

## 5. Content outline per topic

### Core principles and leading words

- Definition: organize by request/use case; **minimize coupling between slices, maximize coupling within a
  slice** (Bogard, `VERIFY`).
- Leading words used across all files: *slice*, *feature*, *shared kernel*, *promotion*, *seam*,
  *flow*. Each is defined once in `core-principles.md`.
- Fit: CRUD-heavy line-of-business apps and APIs fit well. Very small apps, libraries, and pure
  computation engines fit poorly.

### Project layouts (examples with trees)

- **A. Single project, folder per feature, file per type.** `Features/Orders/CreateOrder/{CreateOrder.cs,
  CreateOrderEndpoint.cs, CreateOrderValidator.cs}`.
- **B. Single project, one file per slice.** A `static class CreateOrder` that nests `Command`,
  `Response`, `Validator`, `Handler`, and `MapEndpoint`.
- **C. Two projects.** `App.Api` (host, composition root) and `App.Features` (slices + shared kernel).
- **D. Modular monolith.** `Modules/Ordering/Features/...`, one project per module, slices inside each
  module, public module contracts only.
- **E. Hybrid with a domain project.** `App.Domain` for rich entities and invariants; slices orchestrate.
- Test projects mirror the feature tree: `tests/App.Features.Tests/Orders/CreateOrder/...`.

### Example architecture description (template the skill teaches)

A short `ARCHITECTURE.md` block the agent writes into a project: the layout option used, the dependency
rules, where shared code goes, how a slice is registered, how to run architecture tests.

### Dependency rules ("what touches what")

| From \ To | Own slice | Other slice | Shared kernel | Domain | Infrastructure (DbContext, clients) | Host / composition root |
| --- | --- | --- | --- | --- | --- | --- |
| Slice | yes | **no** (use events or a shared query) | yes | yes | yes | **no** |
| Shared kernel | no | no | yes | yes (DECIDE) | no | no |
| Domain | no | no | yes | yes | **no** | no |
| Infrastructure | no | no | yes | yes | yes | no |
| Host | yes (registration only) | yes (registration only) | yes | yes | yes | yes |

Cross-slice communication options, in order of preference: duplicate the small thing; promote it to the
shared kernel after the third use; publish an in-process notification or domain event; call a shared
read query. A handler calling another handler is a smell.

### Flow

`HTTP request -> endpoint (binding, auth metadata) -> [filters / pipeline: logging, validation,
transaction] -> handler -> DbContext/domain -> response record -> TypedResults / ProblemDetails`.
Draw it as a Mermaid sequence diagram in `slice-anatomy.md`.

### High-level patterns (generic, no library names in rules)

Mediator (dispatch by request type), CQRS (optional; VSA does not require it), pipeline / decorator
behaviors, endpoint filters, REPR, records as DTOs, notifications. Each one gets: what it is, when it
earns its place, when a direct call is simpler.

### Abstractions

Keep: `DbContext` (already a unit of work and repository), `TimeProvider`, typed HTTP clients, a small
`ISender`-style dispatcher only if a pipeline is needed. Avoid by default: generic `IRepository<T>`,
service-per-entity, mapping layers for 1:1 shapes, interfaces with one implementation that is never faked.

### Unit of work vs. repository

The EF Core `DbContext` is the unit of work and `DbSet<T>` is the repository (`VERIFY` Microsoft wording).
A repository still earns its place for: an aggregate with complex load rules reused by many slices, a
non-EF store, or a hard seam for unit tests. Transaction boundary: one `SaveChangesAsync` per command
handler, or a transaction pipeline behavior.

### Record DTOs

`sealed record` for request and response; positional or init-only; no EF entities in responses; one
response type per slice (no shared `OrderDto` reused by 6 slices); nullable annotations on; mapping by
hand or with a projection in the query.

### Validation

Two layers: input shape at the edge (DataAnnotations, .NET 10 built-in Minimal API validation `VERIFY`,
or a validator class in a pipeline step) and business invariants in the domain or handler. Errors map to
`ValidationProblem` / `ProblemDetails`.

### Cross-cutting concerns

Middleware (whole app), endpoint filters / route groups (per area), pipeline behaviors (per request type),
decorators. Covers logging, auth, transactions, caching, idempotency, observability (per-slice
`ActivitySource` and metric names).

### Shared logic: where things belong

| Thing | Location |
| --- | --- |
| Entity with invariants | `Domain/` (or `Features/<Area>/` if only one area uses it) |
| `DbContext`, migrations, entity configuration | `Infrastructure/Persistence/` |
| External API client | `Infrastructure/<Service>/` |
| Logic used by one slice | inside the slice |
| Logic used by 2 slices in one area | `Features/<Area>/Shared/` |
| Logic used across areas (3+ uses, stable) | `Shared/` kernel |
| Pipeline behaviors, filters | `Shared/Behaviors/` or `Infrastructure/` |

### Comparisons

Table: VSA vs. N-tier, Clean, Onion, Hexagonal, "big ball of mud" monolith, modular monolith. Columns:
primary axis, coupling direction, cost to add a feature, abstraction count, test style, team fit.
State what VSA keeps (dependency inversion where it pays) and what it drops (mandatory layer projects).

### Code smells and what to avoid

Handler calls handler; `Common/` or `Shared/` grows faster than features; generic repository; one DTO
shared by many slices; EF entities in responses; slice references another slice's types; logic in
endpoint; inconsistent slice size; anemic pass-through services; "VSA folders, layered code" (a
`Features/` folder that still calls `OrderService`).

### Testing

Integration-first through the host (`WebApplicationFactory`, real database in a container); unit tests for
handlers with complex logic and for domain invariants; architecture tests as a separate project.

### Architecture tests to suggest

1. A type in `Features.<A>` does not depend on any type in `Features.<B>`.
2. Domain does not depend on Features, Infrastructure, or ASP.NET Core.
3. Shared kernel does not depend on Features.
4. Handlers are `sealed` and `internal` (DECIDE).
5. Request/response types are records.
6. Endpoint types do not reference `DbContext` directly (logic lives in the handler) (DECIDE).
7. No type in Features returns an EF entity from a public method.
8. Every request type has exactly one handler.
9. Every slice folder contains an endpoint (or message consumer) — an orphan check.
10. Types named `*Repository` / `*Service` are not in `Features` (optional, strict mode).

### Bootstrapping steps (outline)

New project: pick layout -> `dotnet new` -> create `Features/`, `Shared/`, `Infrastructure/` -> slice
registration (assembly scan or explicit `MapXxx` per slice) -> pipeline/filters -> architecture test
project -> write `ARCHITECTURE.md` -> first slice. Completion: `dotnet build` and `dotnet test` green,
architecture tests run.

New slice: name the use case (verb + noun) -> create folder -> request/response records -> validator ->
handler -> endpoint -> register -> integration test -> architecture tests green.

### Migration steps (outline)

Inventory endpoints -> pick a low-risk vertical -> create `Features/` beside the layers -> move one use
case end to end (strangler) -> inline the service method into the handler -> delete dead layer code ->
add architecture tests in "new code only" mode -> repeat -> remove empty layer folders. Completion per
step: behavior unchanged (existing tests green), no new dependencies from Features to legacy layers.

## 6. What you have not mentioned (proposed additions)

1. **Insularity vs. overlap.** This repo already has CQRS, mediator, repository, unit-of-work, and Minimal
   API skills. Insularity forbids pointing at them, so this skill must stand alone. How much of those
   topics to restate is a scope decision.
2. **Library naming.** The current skill names no libraries. Bootstrapping and architecture tests need
   concrete APIs. The MediatR license change (2025) is a reason to teach a hand-rolled dispatcher.
3. **Target .NET version.** .NET 10 changes validation for Minimal APIs (`VERIFY`).
4. **Host types other than HTTP.** Message consumers, background jobs, gRPC, Blazor pages, and CLI
   commands as slices.
5. **Authorization per slice**, including resource-based authorization inside handlers.
6. **Observability per slice**: activity names, metric tags, log scopes keyed by slice name.
7. **API versioning and OpenAPI** per slice (tags, operation IDs, summaries).
8. **Integration events and the outbox**; domain events vs. integration events.
9. **Idempotency and concurrency** for command slices.
10. **EF Core migrations ownership**: migrations are global, not per slice.
11. **Native AOT / trimming**: reflection-based dispatch and assembly scanning vs. source-generated or
    explicit registration.
12. **Ownership**: `CODEOWNERS` per feature folder; feature flags per slice.
13. **Agent navigability**: one folder holds the whole change, which suits AI agents. Can be a selling
    point in `core-principles.md`.
14. **Pagination, sorting, and filtering contracts** shared across query slices.
15. **Multi-tenancy** as a cross-cutting concern.

## 7. Value-added additions

- `workflow-review.md` with a mechanical checklist, so "review this VSA codebase" gives the same process
  every run.
- Templates: a slice file and an architecture-test class.
- A "should this be shared?" decision tree (Mermaid flowchart).
- Example `ARCHITECTURE.md` section the agent writes into a target project.
- A `vsa` option for `commands/init` (this repo's scaffold script).
- `evals/` cases: new slice, migrate a layered controller, review a smelly layout, explain VSA vs. Clean.
- Description tuning with the skill-creator trigger loop after the content settles.

## 8. Open decisions

Tracked in the interview. Answers are recorded here as they arrive.

- **D1 Concreteness:** tool-agnostic rules and principles. Where a mechanism needs an implementation,
  list the options side by side: Microsoft first-party (Minimal API filters, DataAnnotations / .NET 10
  validation, EF Core, `WebApplicationFactory`) and common third-party (validator libraries,
  architecture-test libraries, mediator libraries, containers for tests). No single option is the
  default.
- **D2 Overlap:** the skill stands alone. Assume no other skill is installed. Cover mediator, CQRS,
  repository, unit of work, pipelines, and Minimal API mechanics as deep as an agent needs to act on
  them inside a VSA codebase. Name no other skill.
- **D3 Packaging:** one model-invoked skill. `SKILL.md` routes to `references/` and `workflows/`.
- **D4 Layout choice:** in an existing project, detect and follow its layout. In a new project, ask the
  user to pick a layout option; suggest A (folder per slice, file per type).
- **D5 Entry points:** all of them are slices: HTTP endpoints, message/event consumers, background and
  scheduled jobs, UI (Blazor / Razor Pages / MVC), gRPC methods, CLI commands. Adds
  `references/entry-points.md` (one section per entry-point type: layout, flow, registration, test).
- **D6 Target:** .NET 10 and .NET 11, C# 14 and C# 15. Use C# 15 `union` types and closed hierarchies
  for handler results, error types, and closed request/message sets (exhaustive dispatch). Not for
  domain state. `VERIFY` preview status and syntax before writing.
- **D7 Expected failures:** describe both result/union types and exceptions mapped to ProblemDetails,
  with trade-offs. No default: follow the target repo, else ask. Wherever exceptions appear, frame them
  as for exceptional cases only. Write no example that throws for an expected outcome (validation, not
  found, conflict).
- **D8 Dispatch:** in a new project, present direct handler injection, a hand-rolled mediator with
  pipeline behaviors, and third-party mediator libraries with trade-offs (including license and AOT
  impact), then ask. In an existing project, follow its convention.
- **D9 Architecture tests:** bootstrap and migration workflows offer the rule set and the library
  options; the user decides whether to add them.
- **D10 Slice isolation:** follow the repo. In an empty repo, suggest area-level sharing (no
  slice-to-slice references; `Features/<Area>/Shared/` allowed; cross-area only through shared kernel,
  domain, or events) and offer strict and relaxed as alternatives.
- **D11 Data access:** follow the repo; in a new project, ask. Options presented: DbContext direct in
  handlers; repository per aggregate; specifications; and a unit-of-work abstraction that exposes the
  data-source providers a slice needs (database context, other stores, external API clients) behind one
  commit boundary.
- **D12 Migration coupling:** the migration workflow asks at the start: temporary calls to legacy
  services tracked as debt (migration log + architecture-test allow-list), or inline on move. In both,
  legacy code never calls into Features.
- **D13 Extras now:** templates (slice, architecture-test class, `ARCHITECTURE.md` section); review
  workflow with a mechanical checklist and a "should this be shared?" Mermaid decision tree; evals with
  the current v1 skill as baseline, then description tuning. Not now: the `commands/init` VSA option.
  Any script the skill bundles or tells the agent to write is a .NET file-based app (`dotnet run x.cs`),
  not Python, PowerShell, or bash.
- **D14 Extra scope:** all four bundles get real coverage: ops per slice (authorization, observability,
  OpenAPI/versioning, feature flags, CODEOWNERS); messaging and consistency (domain vs. integration
  events, outbox, idempotency, concurrency, transaction boundaries); platform and build (AOT/trimming,
  registration, migrations ownership); API contracts and agents (pagination/filter contracts,
  multi-tenancy, agent navigability). ORM options cover EF Core and NHibernate (`ISession` as unit of
  work) — follow the repo, else ask. `VERIFY` NHibernate facts (current release, `ISession`/transaction
  guidance) — not yet researched.
- **D15 DDD depth:** placement and seams only: where aggregates, value objects, and domain events live
  relative to slices, how a handler uses them, and the "refactor toward the domain when smells appear"
  path. Aggregate design itself is out of scope.
- **D16 Migration delivery (app):** the migration workflow asks the user how to deliver (per use case,
  per area, staged PRs). Invariant for every step: internal restructuring keeps the app's externally
  observable behavior unchanged (routes, contracts, status codes, side effects). Completion per step
  includes proving that, for example with existing tests or contract/snapshot tests captured first.
- **D18 Standing rules (user, 2026-11-13):** written into `SKILL.md` and applied by every workflow:
  1. Bundled scripts, and scripts the agent writes for the user, are .NET file-based apps unless the
     user directs otherwise.
  2. Use `union` types only when the project targets C# 15 or later. Below C# 15, use a closed record
     hierarchy (abstract record, private constructor, nested sealed cases).
  3. Keep the project's existing libraries and dependencies. When a suitable dependency already exists
     (validator, mediator, ORM, test framework, architecture-test library), use it. Add or replace a
     dependency only when the user instructs it.
- **D17 Skill delivery (this repo):** open. Ask the user when the content is ready (one PR as 2.0.0,
  staged PRs, or keep v1 file names).

## 10. File tree after decisions

Supersedes section 4.

```text
csharp-vertical-slice-architecture/
├── SKILL.md                          # router, leading words, dependency rules, "follow repo, else ask" rule
├── README.md
├── references/
│   ├── core-principles.md            # definition, coupling axis, fit, agent navigability
│   ├── project-layouts.md            # A–E + Bogard's Pages/<Area>/<UseCase> + modular monolith
│   ├── entry-points.md               # HTTP, consumers, jobs, UI, gRPC, CLI as slices (D5)
│   ├── slice-anatomy.md              # records, handler, endpoint, flow diagram
│   ├── dependency-rules.md           # matrix, isolation levels (D10), cross-slice communication
│   ├── patterns.md                   # dispatch options (D8), CQRS, pipelines, filters, REPR
│   ├── results-and-errors.md         # unions / closed hierarchies (D6), exceptions (D7), ProblemDetails
│   ├── validation.md                 # edge vs. invariants; DataAnnotations, AddValidation, validator libs
│   ├── shared-logic.md               # shared kernel (Evans meaning), shared infrastructure, domain (D15)
│   ├── data-access.md                # EF Core, NHibernate, UoW + providers, repository, specs (D11, D14)
│   ├── cross-cutting-concerns.md     # middleware, filters, behaviors, decorators; auth, observability
│   ├── messaging-and-consistency.md  # events, outbox, idempotency, concurrency (D14)
│   ├── platform-and-build.md         # AOT/trimming, registration, migrations ownership (D14)
│   ├── api-contracts.md              # pagination/filter contracts, OpenAPI, versioning, multi-tenancy
│   ├── comparisons.md                # Clean, Onion, Hexagonal, N-tier, monolith, modular monolith
│   ├── code-smells.md                # smells, symptoms, refactor
│   ├── testing.md                    # integration-first, unit where logic is, test layout
│   └── architecture-tests.md         # rule catalog; ArchUnitNET, NetArchTest(+eNhanced), NsDepCop, reflection
├── workflows/
│   ├── workflow-bootstrap-project.md # asks: layout, dispatch, data access, isolation, arch tests
│   ├── workflow-add-slice.md
│   ├── workflow-migrate.md           # asks: legacy coupling (D12), delivery (D16); behavior invariant
│   └── workflow-review.md            # mechanical checklist + "should this be shared?" diagram
├── assets/templates/
│   ├── slice.cs.tmpl
│   ├── ArchitectureTests.cs.tmpl
│   └── ARCHITECTURE.md.tmpl
├── research/
└── evals/
```

Every workflow starts with the same detection step: read the target repo's existing conventions (layout,
dispatch, data access, error style, isolation, tests) and follow them; ask only for what the repo does
not answer. Bundled scripts are .NET file-based apps (D13).

## 9. Research findings that change this plan

From [vertical-slice-architecture-dotnet-research.md](vertical-slice-architecture-dotnet-research.md):

- .NET 11 GA 2026-11-10 (STS); .NET 10 is LTS. C# 15 unions and `closed` classes need no preview flag on
  `net11.0`; treat both as .NET 11-only. On .NET 10, use an abstract record with a private constructor.
- Union traps to teach: a `default` union holds null and the switch can throw; name cases after the
  domain (`OrderCreated`, not `Created`) to avoid collisions with ASP.NET Core result types; the
  endpoint maps the union with an exhaustive switch to `Results<...>`.
- Bogard's reference sample is CRUD, Razor Pages, `Pages/<Area>/<UseCase>`, nested types, no
  `Features/` folder, transaction committed by a filter. Add this as a layout option and drop any "CRUD
  fits poorly" claim.
- Testing default flips to integration-first against a real database (EF Core team guidance; Bogard
  sample). Unit tests are for complex handler and domain logic. Discourage the in-memory provider and
  `DbSet` mocks.
- "Duplication is cheaper than the wrong abstraction" is Sandi Metz (2016), not Bogard.
- "Shared kernel" in Evans means a small shared domain subset. Rename the infrastructure folder concept
  (for example *shared infrastructure*) and keep *shared kernel* for its DDD meaning.
- Global query filters (named filters in EF 10) cover soft delete and tenancy; they are not reasons for
  a repository.
- .NET 10 `AddValidation`: Minimal APIs only, source generator only sees the calling assembly, missing
  metadata skips validation silently. .NET 11 adds async validators and union validation.
- Architecture-test options: ArchUnitNET 0.13.5 (`Slices().Matching(...).Should().NotDependOnEachOther()`),
  NetArchTest (no release since 2021, prefix matching trap), NetArchTest.eNhancedEdition, NsDepCop
  (build-time analyzer).
- MediatR v13+ dual license, current 15.0.0, reflection dispatch; `Mediator` (source generator, MIT)
  claims Native AOT support.
