# Minimal APIs

Guidance on ASP.NET Core Minimal APIs — the routing table (by task) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic

| File | Covers |
| --- | --- |
| `route-registration.md` | `MapGet`/`MapPost`/etc., route constraints, `.WithName`/`.WithSummary`/`.WithTags` |
| `parameter-binding.md` | Implicit binding rules, `[FromRoute]`/`[FromQuery]`/`[FromServices]`/`[FromBody]`, `BindAsync`, `[AsParameters]` |
| `endpoint-filters.md` | `IEndpointFilter`, filter registration, ordering, group-level filters |
| `route-groups.md` | `MapGroup`, shared prefixes/filters/metadata, nesting |
| `typed-results.md` | `TypedResults`, `Results<T1,T2>` union return types, OpenAPI accuracy |
| `request-validation.md` | Manual validation, data annotations, filter-based validation |
| `testing.md` | Unit testing handlers/filters, integration testing with `WebApplicationFactory` |

## Scope

ASP.NET Core Minimal APIs as their own convention: routing, binding, filters, grouping, typed
results, and validation. Out of scope: content negotiation/formatting beyond the default JSON
shape, and OpenAPI document generation configuration itself.

Each reference file notes a version-specific fact inline where one applies; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).
