# Vertical Slice Architecture

Organize a C#/.NET application by use case instead of by technical layer. This skill helps you lay out a
sliced project, add slices that stay independent, move a layered codebase over one use case at a time,
and review a sliced codebase for the smells that pull it back toward layers. It targets .NET 10 and
.NET 11, including C# 15 unions and closed hierarchies for handler outcomes.

## When to reach for it

- You are adding an endpoint, command, query, message consumer, or job to a project with a `Features/` or
  `Pages/` folder, and want it to match the existing slices.
- You are starting a new API or app and want a feature-first layout with tests and enforced boundaries
  from day one.
- You have a `Controllers/` → `Services/` → `Repositories/` codebase that is painful to change and want to
  move it to slices without changing its behavior.
- You want a review of a sliced codebase: cross-slice coupling, a `Shared/` folder that grew into a
  service layer, handlers calling handlers, missing tests.
- You need to explain or decide between VSA, Clean Architecture, Onion, N-tier, or a modular monolith.

## Using it

The skill is model-invoked: it loads on its own when your request involves slices, feature folders, or
project layout. You can also call it with `/csharp-vertical-slice-architecture`.

It follows your repository before it suggests anything. It reads your existing layout, libraries, error
style, and tests, and asks you only about what the code does not answer. It does not add or swap
packages unless you ask.

## What it covers

| Topic | Where |
| --- | --- |
| Bootstrap a new sliced project | [workflows/workflow-bootstrap-project.md](workflows/workflow-bootstrap-project.md) |
| Add one slice | [workflows/workflow-add-slice.md](workflows/workflow-add-slice.md) |
| Migrate a layered project | [workflows/workflow-migrate.md](workflows/workflow-migrate.md) |
| Review a sliced codebase | [workflows/workflow-review.md](workflows/workflow-review.md) |
| Principles, vocabulary, when VSA fits | [references/core-principles.md](references/core-principles.md) |
| Layout options with folder trees | [references/project-layouts.md](references/project-layouts.md) |
| HTTP, Razor Pages, Blazor, consumers, jobs, gRPC, CLI as slices | [references/entry-points.md](references/entry-points.md) |
| The parts of a slice and the request flow | [references/slice-anatomy.md](references/slice-anatomy.md) |
| What may depend on what | [references/dependency-rules.md](references/dependency-rules.md) |
| Mediator, CQRS, pipelines, REPR | [references/patterns.md](references/patterns.md) |
| Unions, closed hierarchies, results vs. exceptions | [references/results-and-errors.md](references/results-and-errors.md) |
| Validation | [references/validation.md](references/validation.md) |
| Where domain, services, and shared code live | [references/shared-logic.md](references/shared-logic.md) |
| EF Core, NHibernate, unit of work, repositories | [references/data-access.md](references/data-access.md) |
| Auth, logging, observability, caching, feature flags | [references/cross-cutting-concerns.md](references/cross-cutting-concerns.md) |
| Events, outbox, idempotency, concurrency | [references/messaging-and-consistency.md](references/messaging-and-consistency.md) |
| AOT, registration, migrations | [references/platform-and-build.md](references/platform-and-build.md) |
| Paging, OpenAPI, versioning, multi-tenancy | [references/api-contracts.md](references/api-contracts.md) |
| VSA vs. other architectures | [references/comparisons.md](references/comparisons.md) |
| Code smells | [references/code-smells.md](references/code-smells.md) |
| Testing and architecture tests | [references/testing.md](references/testing.md), [references/architecture-tests.md](references/architecture-tests.md) |

The `research/` folder holds the sourced research notes behind the skill. The skill itself does not load
them.

## Example prompts

- "Add a cancel-order endpoint to the Orders feature. It should return 409 if the order already shipped."
- "Start a new .NET 11 API for a library-loans system, organized by vertical slices, with Postgres and
  architecture tests."
- "Our OrdersController calls OrderService which calls OrderRepository. Move the order endpoints to
  vertical slices without breaking the API."
- "Review src/Features for coupling between slices and anything that looks like a service layer coming
  back."
- "Should a slice handler use the DbContext directly or go through a repository?"
