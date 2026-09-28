# Vertical Slice Architecture

Guidance on organizing C#/.NET code by feature instead of by technical layer. It covers what a
slice contains, how the pattern relates to CQRS, and where its tradeoffs (duplication,
granularity, adoption cost) actually bite in a real codebase.

## When to reach for it

- You're deciding how to lay out a new feature's code and weighing a `Features/CreateOrder/`
  folder against splitting it across `Controllers/`, `Services/`, and `Repositories/`.
- You're reviewing an existing `Features/` folder and want to check its granularity or spot
  shared-kernel creep.
- You're deciding whether to introduce vertical slices into an existing layered codebase, one
  feature at a time, and want to know where that adoption tends to get hard.

## Using it

This skill fires automatically when your request involves describing a feature layout, asking
about slice-vs-layer tradeoffs, or asking how VSA relates to CQRS. You can also invoke it directly
with `/csharp-vertical-slice-architecture`.

## What it covers

| Topic | Reference |
| --- | --- |
| Core philosophy, "screaming architecture," folder and naming conventions | [references/philosophy-and-organization.md](references/philosophy-and-organization.md) |
| How VSA and CQRS relate, and where they don't require each other | [references/cqrs-relationship.md](references/cqrs-relationship.md) |
| The shape of an individual slice: request, handler, response, sizing | [references/slice-anatomy.md](references/slice-anatomy.md) |
| Sharing validation, logging, or persistence across slices without re-layering | [references/cross-cutting-concerns.md](references/cross-cutting-concerns.md) |
| Query-object-per-slice vs. a shared repository for data access | [references/data-access-patterns.md](references/data-access-patterns.md) |
| Whether VSA fits a project, and introducing it incrementally | [references/fit-and-adoption.md](references/fit-and-adoption.md) |
| Code smells: duplication vs. premature abstraction, inconsistent granularity, shared-kernel creep | [references/pitfalls.md](references/pitfalls.md) |
| Testing a slice: unit vs. integration, test folder structure | [references/testing.md](references/testing.md) |

## Example prompts

- "I'm starting a new 'cancel order' feature. Should this live in its own folder or spread
  across Controllers/Services/Repositories?"
- "How does Vertical Slice Architecture relate to CQRS? Do I need a mediator library for either?"
- "Review this Features/CreateOrder folder and tell me if it's grown too many responsibilities."
