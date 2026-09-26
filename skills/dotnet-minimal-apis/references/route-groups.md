# Route Groups

`MapGroup` collects a set of related endpoints under a shared route prefix and lets you apply
configuration — filters, authorization, tags, OpenAPI metadata — once to the whole group instead of
repeating it on every individual endpoint.

## Creating a group

```csharp
var orders = app.MapGroup("/orders")
    .WithTags("Orders")
    .RequireAuthorization();

orders.MapGet("/", () => ListOrders());
orders.MapGet("/{id:guid}", (Guid id) => GetOrder(id));
orders.MapPost("/", (CreateOrderRequest request) => CreateOrder(request));
```

Every endpoint mapped on `orders` above is prefixed with `/orders` and inherits `WithTags("Orders")`
and `RequireAuthorization()` without repeating either call. `MapGroup` returns a
`RouteGroupBuilder`, itself an `IEndpointRouteBuilder` — `MapGet`/`MapPost`/etc. work on it exactly as
they do on the top-level `app`, which is what lets group and endpoint registration nest naturally.

## Nesting groups

Groups nest to build up a route prefix and configuration incrementally:

```csharp
var api = app.MapGroup("/api").RequireAuthorization();
var v1 = api.MapGroup("/v1").WithTags("v1");

v1.MapGet("/orders", () => ListOrders()); // final route: /api/v1/orders, authorized, tagged "v1"
```

Configuration applied at each level composes — an inner group's endpoints get both its own
configuration and everything applied at every enclosing group level.

## Overriding a group-level policy per endpoint

Endpoint-level configuration composes with, and can extend, whatever the group already applied — for
example, adding a second, more specific authorization requirement on top of the group's:

```csharp
var orders = app.MapGroup("/orders").RequireAuthorization();

orders.MapDelete("/{id:guid}", (Guid id) => DeleteOrder(id))
      .RequireAuthorization("RequireAdministrator"); // both the group's and this policy must pass
```

`AllowAnonymous()` on a specific endpoint within an authorized group overrides the group's
`RequireAuthorization()` for that one endpoint, the same way it overrides `[Authorize]` in
attribute-based authorization.

## Grouping purely for shared filters or metadata, without a route prefix

`MapGroup("")` (an empty prefix) is a legitimate way to apply shared filters or metadata to a set of
endpoints that don't share a common URL segment — the grouping mechanism (shared configuration) is
independent of whether the group also contributes a path prefix:

```csharp
var instrumented = app.MapGroup("").AddEndpointFilter<TimingFilter>();

instrumented.MapGet("/health", () => Results.Ok());
instrumented.MapGet("/version", () => Results.Ok(AppVersion));
```
