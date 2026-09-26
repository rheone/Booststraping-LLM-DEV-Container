# Minimal APIs

Guidance on ASP.NET Core Minimal APIs: route registration, parameter binding, endpoint filters,
route groups, typed results, and request validation.

## When to reach for it

- You're writing or reviewing a minimal API endpoint.
- You're debugging a parameter-binding ambiguity, such as two complex-type parameters on one
  handler with no explicit `[FromBody]`.
- You're grouping related endpoints under a shared prefix or filter, or deciding how to shape and
  validate a response.

## Using it

This skill fires automatically when your request involves writing an endpoint, debugging binding,
or adding a filter or route group. You can also invoke it directly with `/dotnet-minimal-apis`.

## What it covers

| Topic | Reference |
| --- | --- |
| `MapGet`/`MapPost`/etc., route constraints, `.WithName`/`.WithSummary`/`.WithTags` | [references/route-registration.md](references/route-registration.md) |
| Implicit binding rules, `[FromRoute]`/`[FromQuery]`/`[FromServices]`/`[FromBody]`, `BindAsync`, `[AsParameters]` | [references/parameter-binding.md](references/parameter-binding.md) |
| `IEndpointFilter`, filter registration and ordering | [references/endpoint-filters.md](references/endpoint-filters.md) |
| `MapGroup`, shared prefixes/filters/metadata, nesting | [references/route-groups.md](references/route-groups.md) |
| `TypedResults`, `Results<T1,T2>` union return types | [references/typed-results.md](references/typed-results.md) |
| Manual validation, data annotations, filter-based validation | [references/request-validation.md](references/request-validation.md) |
| Unit and integration testing endpoints and filters | [references/testing.md](references/testing.md) |

## Example prompts

- "Write a minimal API endpoint that creates an order and returns a typed result."
- "Why is this handler failing to bind two complex parameters at once?"
- "Group these order-related endpoints under /orders with a shared validation filter."
