# C# Vertical Slice Architecture

Guidance on Vertical Slice Architecture (VSA) as a C#/.NET code-organization pattern — organize by
feature/use-case, not by technical layer. The routing table (by situation) is in
[SKILL.md](SKILL.md).

```text
references/                          one file per concern/topic, not per package version —
                                      VSA is an architectural style with nothing to version-pin
  philosophy-and-organization.md       core philosophy, "screaming architecture", slice contents,
                                        folder/namespace conventions, contrast with layered/onion/
                                        clean architecture
  cqrs-relationship.md                 VSA (organization axis) vs. CQRS (read/write axis) —
                                        precise, non-conflating treatment of how they compose
  slice-anatomy.md                     generic request/handler/response shape, sizing a slice
  cross-cutting-concerns.md            sharing validation/logging/persistence without re-layering;
                                        minimal base abstractions and pipeline-style composition
  data-access-patterns.md              query-object-per-slice vs. shared repository, tradeoffs
  fit-and-adoption.md                  when VSA fits vs. doesn't; incremental adoption into an
                                        existing layered codebase
  pitfalls.md                          duplication vs. the wrong abstraction, inconsistent slice
                                        granularity, shared-kernel creep
  testing.md                           unit-testing handler logic vs. integration-testing a slice
                                        end to end; test organization mirroring Features/
```

## Scope

An architectural pattern, not a package — there is no version or license to pin, and no NuGet
package this skill tracks. Guidance is cross-checked against Jimmy Bogard's original formulation of
the pattern and current (2026) community practice rather than a single source.

This skill is **tool-agnostic by design**: it never names or depends on a specific mediator, DI,
validation, or ORM library, since VSA as a pattern requires none of them. Every mechanism described
(handler dispatch, validation pipelines, persistence context) is generic — apply it with whatever
libraries a given project already uses.

Out of scope: Domain-Driven Design tactical patterns (compatible with VSA but not required by it)
and microservice/service-boundary decomposition (VSA organizes code within one deployable unit, not
across network boundaries). See [SKILL.md](SKILL.md) for the full out-of-scope list.
