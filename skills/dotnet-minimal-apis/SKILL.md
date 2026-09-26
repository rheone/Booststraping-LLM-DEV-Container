---
name: dotnet-minimal-apis
description: Guidance on ASP.NET Core Minimal APIs (verified current against ASP.NET Core 10.0) — MapGet/MapPost/MapPut/MapDelete route registration, parameter binding from route/query/body/services via implicit rules and [FromRoute]/[FromQuery]/[FromServices]/[FromBody], BindAsync and [AsParameters] for custom/grouped binding, IEndpointFilter for cross-cutting endpoint logic and filter ordering, MapGroup route groups for shared prefixes/filters/metadata, TypedResults and Results<T1,T2> union return types for compiler-checked and OpenAPI-accurate responses, and request validation approaches (manual, data annotations, filter-based). Use when writing or reviewing a minimal API endpoint, debugging a parameter binding ambiguity, adding an endpoint filter, grouping related routes, or choosing how to shape and validate a minimal API response.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Minimal APIs

Guidance on ASP.NET Core Minimal APIs — routing, parameter binding, endpoint filters, route groups,
typed results, and request validation, as their own convention with its own scope and tradeoffs.
Organized by task, not by ASP.NET Core version — the `Map*`/`MapGroup`/`IEndpointFilter`/
`TypedResults` surface has been stable across recent releases; each reference file notes a
version-specific fact inline where one applies.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Registering `MapGet`/`MapPost`/etc., route constraints, naming an endpoint | [references/route-registration.md](references/route-registration.md) |
| Binding route/query/body/service parameters, `BindAsync`, `[AsParameters]` | [references/parameter-binding.md](references/parameter-binding.md) |
| Writing cross-cutting logic (validation, logging, short-circuiting) around a handler | [references/endpoint-filters.md](references/endpoint-filters.md) |
| Grouping related endpoints under a shared prefix/filter/policy with `MapGroup` | [references/route-groups.md](references/route-groups.md) |
| Returning strongly-typed, OpenAPI-accurate responses with `TypedResults`/`Results<T1,T2>` | [references/typed-results.md](references/typed-results.md) |
| Validating a request body or bound parameters | [references/request-validation.md](references/request-validation.md) |
| Unit or integration testing endpoints and filters | [references/testing.md](references/testing.md) |

## Quick start

```csharp
var orders = app.MapGroup("/orders").WithTags("Orders");

orders.MapGet("/{id:guid}", Results<Ok<Order>, NotFound> (Guid id, IOrderRepository repository) =>
{
    var order = repository.Find(id);
    return order is not null ? TypedResults.Ok(order) : TypedResults.NotFound();
});

orders.MapPost("/", (CreateOrderRequest request, IOrderRepository repository) =>
{
    var order = repository.Create(request);
    return TypedResults.Created($"/orders/{order.Id}", order);
})
.AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

The single most common miss: registering two complex-type parameters on one handler with no explicit
`[FromBody]` on either — the binder can't disambiguate which one should read the request body. See
[references/parameter-binding.md](references/parameter-binding.md).

## Out of scope

- Content negotiation and response formatting beyond `TypedResults`' JSON-first shape — minimal APIs
  return `IResult`-based responses serialized as JSON by default; format-negotiation concerns beyond
  that default are a narrow, separate surface this skill doesn't cover.
- OpenAPI document generation configuration itself (title, servers, security scheme definitions) —
  out of scope beyond noting, in
  [references/typed-results.md](references/typed-results.md), how a handler's return type shapes
  what a generator infers.
