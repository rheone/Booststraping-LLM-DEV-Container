---
name: csharp-vertical-slice-architecture
description: 'Vertical Slice Architecture (VSA) for C#/.NET 10 and 11: organizing code by feature/use case ("slice") instead of by technical layer. Covers project and folder layouts, slice anatomy (request/response records, handler, endpoint), dependency rules between slices, shared infrastructure and domain, mediator/CQRS/pipeline/REPR patterns, results with C# 15 unions or closed hierarchies, validation, data access (DbContext, NHibernate ISession, unit of work, repository), cross-cutting concerns, messaging, AOT, testing, and architecture tests that enforce slice isolation. Includes step-by-step workflows to bootstrap a VSA project, add a slice, migrate a layered (Controllers/Services/Repositories) project to slices, and review a VSA codebase for smells. Use when adding a feature, endpoint, command, query, message consumer, or job to a project with a Features/ or Pages/ folder; when designing or restructuring a .NET project layout; when comparing VSA with Clean, Onion, Hexagonal, N-tier, or modular monolith; or when writing architecture tests for feature boundaries.'
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# C# Vertical Slice Architecture

Vertical Slice Architecture (VSA) is an architectural style, not a package. You organize code around
distinct requests, one *slice* per use case, and you group everything that use case needs (input,
logic, data access, output) in one place. The governing rule is Jimmy Bogard's: **minimize coupling
between slices, and maximize coupling inside a slice**, along the axis of change.

## Leading words

Each word below has one meaning across every file in this skill. The full definitions are in
[references/core-principles.md](references/core-principles.md).

- **Slice**: one use case, end to end: request, handler, response, edge mapping, and tests.
- **Area**: a group of related slices (`Orders`, `Billing`), one folder level above the slice.
- **Edge**: where a request enters the app (HTTP endpoint, message consumer, job, page, gRPC method,
  CLI command). The edge maps transport to the slice and the result back to transport.
- **Shared infrastructure**: technical code every slice may use (persistence setup, pipeline steps,
  clients). It knows no slice.
- **Domain**: entities, value objects, invariants, and domain events shared by slices. It depends on
  nothing in Features or infrastructure.
- **Shared kernel**: Evans' meaning only: a small, explicitly bounded part of the domain model that two
  modules or teams agree to share. It is not a utilities folder.
- **Promotion**: moving code out of a slice into area-shared code, the domain, or shared infrastructure
  after it has proven it is shared.

## Standing rules

Apply these in every workflow and every answer.

1. **Follow the repo, else ask.** Before you design or write code, detect the target repo's existing
   conventions: layout, dispatch style, data access, result/error style, slice isolation level, test
   style, and libraries. Follow what you find. Ask the user only for what the repo does not answer, and
   present the options from the relevant reference file when you ask. When the user cannot answer, choose
   the option closest to the existing code (or the reference file's suggested default), and list every
   such decision in your final report.
2. **Keep existing dependencies.** When the project already has a suitable library (validator,
   mediator, ORM, test framework, architecture-test library), use it. Add, replace, or remove a
   dependency only when the user instructs it.
3. **Unions need C# 15.** Use `union` types and `closed` classes only when the project's language
   version is C# 15 or later (the default for `net11.0`). Below C# 15, use an abstract record with a
   private constructor and nested sealed cases. See
   [references/results-and-errors.md](references/results-and-errors.md).
4. **Exceptions are for exceptional cases.** Model expected outcomes (validation failure, not found,
   conflict, forbidden) as results, or as the repo's established convention. Write no example that
   throws for an expected outcome.
5. **Scripts are .NET file-based apps.** Any script you bundle or write for the user is a single
   `.cs` file run with `dotnet run <file>.cs`, unless the user directs otherwise.
6. **Externally observable behavior is a contract.** Restructuring (moving code into slices, renaming
   folders, promoting shared code) keeps routes, request and response shapes, status codes, and side
   effects unchanged unless the user asks for a behavior change.
7. **Build only what was asked.** Add no required input, field, endpoint, or behavior that the request
   does not name. A loan with no borrower is what the user asked for if they did not mention a borrower.
   When you think something is missing, make it optional or leave it out, and list it in your final report
   as a suggestion.
8. **Async all the way, with a token.** I/O is async from the edge to the database. Every async method
   that other code calls (handlers, area `Shared/`, domain services, infrastructure, repositories) takes
   `CancellationToken ct = default` as its last parameter, and every caller passes on the token it
   received instead of relying on the default. Every `await` in `src/` uses `.ConfigureAwait(false)`,
   except in UI code that needs its context after the await; test code omits it. This rule overrides
   rule 1 for code you write or change: apply it even when the existing slices do not, leave untouched code
   alone unless the user asks, and name the gap in your report. Details and analyzer settings are in
   [references/slice-anatomy.md](references/slice-anatomy.md#async-and-cancellation).

## Dependency rules

The suggested isolation level for a repo with no convention is **area-level sharing**. The full
matrix, the strict and relaxed alternatives, and cross-slice communication options are in
[references/dependency-rules.md](references/dependency-rules.md).

| From | May depend on | Must not depend on |
| --- | --- | --- |
| Slice | its own types, its area's `Shared/`, domain, shared infrastructure | another slice, another area, the host |
| Area `Shared/` | domain, shared infrastructure | any slice, another area |
| Domain | the shared kernel, the BCL | Features, infrastructure, ASP.NET Core |
| Shared infrastructure | domain | Features |
| Host / composition root | everything (registration only) | — |

## Workflows

Each workflow is a numbered sequence with a completion criterion per step. Read the whole workflow
file before you start step 1.

| You are asked to... | Workflow |
| --- | --- |
| Create a new project organized by slices | [workflows/workflow-bootstrap-project.md](workflows/workflow-bootstrap-project.md) |
| Add a feature, endpoint, command, query, consumer, or job | [workflows/workflow-add-slice.md](workflows/workflow-add-slice.md) |
| Move a layered project (Controllers/Services/Repositories) to slices | [workflows/workflow-migrate.md](workflows/workflow-migrate.md) |
| Review or audit a VSA codebase, or check a slice for smells | [workflows/workflow-review.md](workflows/workflow-review.md) |

## Reference files

| You need... | Reference file |
| --- | --- |
| Definitions, the coupling rule, when VSA fits | [references/core-principles.md](references/core-principles.md) |
| Folder and project layout options with trees | [references/project-layouts.md](references/project-layouts.md) |
| Slices per edge: HTTP, consumers, jobs, UI, gRPC, CLI | [references/entry-points.md](references/entry-points.md) |
| The parts of one slice and the request flow | [references/slice-anatomy.md](references/slice-anatomy.md) |
| What may reference what; cross-slice communication | [references/dependency-rules.md](references/dependency-rules.md) |
| Dispatch (direct, mediator), CQRS, pipelines, filters, REPR | [references/patterns.md](references/patterns.md) |
| Result types, unions, closed hierarchies, exceptions, ProblemDetails | [references/results-and-errors.md](references/results-and-errors.md) |
| Input validation and invariants | [references/validation.md](references/validation.md) |
| Where domain, services, clients, and shared code live | [references/shared-logic.md](references/shared-logic.md) |
| DbContext, NHibernate, unit of work, repository, specifications | [references/data-access.md](references/data-access.md) |
| Logging, authorization, transactions, caching, observability | [references/cross-cutting-concerns.md](references/cross-cutting-concerns.md) |
| Domain and integration events, outbox, idempotency, concurrency | [references/messaging-and-consistency.md](references/messaging-and-consistency.md) |
| Native AOT, trimming, registration, migrations ownership | [references/platform-and-build.md](references/platform-and-build.md) |
| Pagination, filtering, OpenAPI, versioning, multi-tenancy | [references/api-contracts.md](references/api-contracts.md) |
| VSA vs. N-tier, Clean, Onion, Hexagonal, monoliths | [references/comparisons.md](references/comparisons.md) |
| Smells, symptoms, and refactors | [references/code-smells.md](references/code-smells.md) |
| Testing slices: integration, unit, test layout | [references/testing.md](references/testing.md) |
| Architecture tests that enforce slice isolation | [references/architecture-tests.md](references/architecture-tests.md) |

## Templates

- [assets/templates/slice.cs.tmpl](assets/templates/slice.cs.tmpl): one HTTP slice in a single file.
- [assets/templates/ArchitectureTests.cs.tmpl](assets/templates/ArchitectureTests.cs.tmpl): slice
  isolation rules.
- [assets/templates/ARCHITECTURE.md.tmpl](assets/templates/ARCHITECTURE.md.tmpl): the architecture
  section you write into the target project.

## Quick start

```text
# Layered (by technical role)          # Sliced (by use case)
Controllers/OrdersController.cs         Features/
Services/OrderService.cs                  Orders/
Repositories/OrderRepository.cs             CreateOrder/CreateOrder.cs      # request, handler, response, endpoint
Models/OrderDto.cs                          GetOrderById/GetOrderById.cs
                                            Shared/OrderNumberFormatter.cs  # used by 2+ Orders slices
                                        Domain/Order.cs
                                        Infrastructure/Persistence/AppDbContext.cs
```

A change to "create order" touches four folders in the layered tree and one folder in the sliced
tree. That locality is the trade VSA makes, for human readers and for agents.
