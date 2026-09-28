---
name: csharp-vertical-slice-architecture
description: 'Guidance on Vertical Slice Architecture (VSA) as a C#/.NET code-organization pattern — organizing by feature/use-case ("slice") instead of by technical layer (controllers/services/repositories). Covers slice anatomy (request/handler/response), folder and naming conventions (Features/CreateOrder/), the VSA-vs-CQRS relationship, cross-cutting concerns without re-layering, data-access tradeoffs (query-per-slice vs shared repository), when VSA fits vs. doesn''t, incremental adoption into an existing layered codebase, common pitfalls (premature abstraction, inconsistent granularity, shared-kernel creep), and testing slices in isolation. Use when deciding how to structure a new feature, evaluating whether to adopt VSA, reviewing a "Features/" folder layout, or explaining how VSA relates to CQRS/Clean/Onion architecture. Tool-agnostic: describes mechanisms generically and does not name or require any specific mediator, DI, validation, or ORM library.'
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Vertical Slice Architecture

Vertical Slice Architecture (VSA) is an **architectural style**, not a package — there is nothing
to install and no version to pin. This skill documents the pattern itself: how to organize code by
feature instead of by technical layer, what a slice contains, how VSA relates to (and differs
from) CQRS, and where the pattern's tradeoffs actually bite. Guidance is cross-checked against
Jimmy Bogard's original formulation of the pattern and current community practice, not a single
opinionated source.

Everything below is described **tool-agnostically**: "a handler dispatch mechanism," "a validation
pipeline," "a pattern often paired with X" — never a specific mediator, DI container, validation,
or ORM library — because the pattern itself doesn't require any of them, and this skill needs to
stay correct regardless of which libraries a given project uses.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Deciding how to organize a new feature's code, or explaining "screaming architecture" / slice-vs-layer tradeoffs | [references/philosophy-and-organization.md](references/philosophy-and-organization.md) |
| Figuring out how VSA and CQRS relate — is one required for the other? | [references/cqrs-relationship.md](references/cqrs-relationship.md) |
| Writing or reviewing the shape of an individual slice (request/handler/response) | [references/slice-anatomy.md](references/slice-anatomy.md) |
| Sharing validation, logging, or persistence setup across slices without re-layering | [references/cross-cutting-concerns.md](references/cross-cutting-concerns.md) |
| Deciding query-object-per-slice vs. a shared repository for data access | [references/data-access-patterns.md](references/data-access-patterns.md) |
| Deciding whether VSA fits a project, or introducing it feature-by-feature into an existing layered codebase | [references/fit-and-adoption.md](references/fit-and-adoption.md) |
| Reviewing a VSA codebase for code smells (duplication, granularity, shared-kernel creep) | [references/pitfalls.md](references/pitfalls.md) |
| Deciding how to test a slice — unit vs. integration, test folder structure | [references/testing.md](references/testing.md) |

## Quick start

The one-line version of VSA: **stop grouping files by what they technically are
(`Controllers/`, `Services/`, `Repositories/`) and start grouping them by what they do for a user
(`Features/CreateOrder/`, `Features/GetOrderById/`)**. Each feature folder holds everything that
feature needs — its input shape, its logic, its output shape, colocated — rather than scattering
one feature's code across five layer-named folders.

```text
# Layered (by technical role)               # Vertical slice (by feature)
Controllers/                                 Features/
  OrdersController.cs                          CreateOrder/
Services/                                        CreateOrderRequest.cs
  OrderService.cs                                CreateOrderHandler.cs
Repositories/                                    CreateOrderResponse.cs
  OrderRepository.cs                           GetOrderById/
Models/                                          GetOrderByIdRequest.cs
  Order.cs                                       GetOrderByIdHandler.cs
  OrderDto.cs                                    GetOrderByIdResponse.cs
```

Changing "create order" behavior in the layered version touches four folders. In the sliced
version it touches one. That's the whole trade VSA is making — start with
[references/philosophy-and-organization.md](references/philosophy-and-organization.md) for why
that trade is usually worth it, and when it isn't.

## Out of scope

- Naming or depending on any specific mediator, DI, validation, or ORM library. Every mechanism
  here (handler dispatch, validation pipelines, persistence) is described generically; apply it
  with whatever libraries a given project already uses.
- Domain-Driven Design tactical patterns (aggregates, value objects, domain events) — VSA is a
  code-*organization* pattern and is compatible with DDD but doesn't require it; this skill covers
  the slicing axis only.
- Microservice/service-boundary decomposition — VSA is about organizing code *within* one
  deployable unit, not about where to draw network boundaries between services.
