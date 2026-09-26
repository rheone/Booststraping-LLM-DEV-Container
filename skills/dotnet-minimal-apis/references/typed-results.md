# TypedResults

`TypedResults` is a static class mirroring `Results`, whose members return concrete result types
(`Ok<T>`, `NotFound`, `Created<T>`, and so on) instead of the shared `IResult` interface `Results`
returns. Preferring `TypedResults` over `Results` costs nothing at the call site and buys compiler-
checked return shapes plus more accurate generated OpenAPI documentation.

## Basic usage

```csharp
app.MapGet("/orders/{id:guid}", (Guid id, IOrderRepository repository) =>
{
    var order = repository.Find(id);
    return order is not null ? TypedResults.Ok(order) : Results.NotFound();
});
```

## Declaring a multi-result return type explicitly

A handler that can return more than one distinct result shape should declare that in its return
type, using `Results<TResult1, TResult2, ...>` — a union type the framework and OpenAPI generation
both understand:

```csharp
app.MapGet("/orders/{id:guid}", Results<Ok<Order>, NotFound> (Guid id, IOrderRepository repository) =>
{
    var order = repository.Find(id);
    return order is not null ? TypedResults.Ok(order) : TypedResults.NotFound();
});
```

With the return type declared as `Results<Ok<Order>, NotFound>`, the compiler rejects a branch that
tries to return any other result shape — a `TypedResults.BadRequest()` slipped into one branch by
mistake fails to compile instead of silently changing the endpoint's actual possible responses.

## Why this matters for OpenAPI generation

An endpoint returning the bare `Results` type (or a loosely-typed `IResult`) gives OpenAPI generation
nothing to infer response shapes from beyond what you declare separately via
`.Produces<T>(statusCode)`. An endpoint whose return type is `Results<Ok<Order>, NotFound>` (or a
single concrete type like `Ok<Order>`) is self-describing — the generator reads the return type
directly and produces an accurate schema without a separate, easy-to-forget `.Produces<T>(...)`
call for every possible response.

## Common result types

| `TypedResults` member | HTTP status | Typical use |
| --- | --- | --- |
| `Ok(value)` / `Ok()` | 200 | Successful GET/PUT with a body, or with no body |
| `Created(uri, value)` / `CreatedAtRoute(...)` | 201 | Successful POST that created a resource |
| `NoContent()` | 204 | Successful action with nothing to return (e.g. DELETE) |
| `NotFound()` | 404 | Requested resource doesn't exist |
| `BadRequest(error)` | 400 | Malformed or semantically invalid request |
| `ValidationProblem(errors)` | 400 | Structured field-level validation failure |
| `Conflict()` | 409 | Request conflicts with current resource state |
| `Unauthorized()` / `Forbid()` | 401 / 403 | Authentication/authorization failure (rare to return manually — usually the authentication/authorization pipeline produces these) |

## When plain Results is still fine

A handler with exactly one possible outcome, or one whose response shape genuinely doesn't matter for
OpenAPI/documentation purposes (an internal diagnostic endpoint, a quick prototype), loses little by
using `Results.Ok(...)` directly — `TypedResults` is a strict improvement with no runtime cost, but
it's a code-quality choice, not a correctness requirement for every single endpoint.
