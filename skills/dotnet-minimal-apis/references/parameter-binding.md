# Parameter Binding

A minimal API handler's parameters bind from the request automatically, based on parameter type and
name, following a fixed set of implicit rules — with explicit attributes available to override the
default source when the implicit rule doesn't do what you want.

## Implicit binding rules

- A parameter whose name matches a **route** template segment binds from the route.
- A **complex type** (a class or record with no route match) binds from the **request body** as
  JSON — at most one such parameter per handler.
- A simple type (`string`, `int`, `Guid`, `bool`, `DateTime`, and similar) with no route match binds
  from the **query string**.
- A parameter type registered in the DI container binds as a **service**, resolved from
  `HttpContext.RequestServices`.
- `HttpContext`, `HttpRequest`, `HttpResponse`, `ClaimsPrincipal`, and `CancellationToken` bind
  specially, from the current request context.

```csharp
app.MapGet("/orders/{id:guid}", (
    Guid id,                    // route
    string? sort,               // query string ("?sort=...")
    IOrderRepository repository, // DI service
    CancellationToken cancellationToken) =>
{
    return repository.FindAsync(id, sort, cancellationToken);
});
```

## Explicit binding attributes

Use `[FromRoute]`, `[FromQuery]`, `[FromBody]`, `[FromServices]`, `[FromHeader]`, or `[FromForm]`
when the implicit rule would guess wrong, or to make the source unambiguous in code review:

```csharp
app.MapGet("/search", ([FromQuery] string q, [FromServices] ISearchIndex index) => index.Search(q));

app.MapPost("/orders", ([FromBody] CreateOrderRequest request, [FromServices] IOrderService orders) =>
    orders.CreateAsync(request));
```

`[FromBody]` is the most commonly *necessary* explicit attribute: a handler taking more than one
complex-type parameter needs at most one of them to actually read the body, and the implicit rule
alone can't disambiguate which — mark the one that should be the body explicitly, and bind the rest
from elsewhere (route, query, or a service).

## Binding via a type implementing BindAsync

For a parameter type the built-in binder doesn't know how to construct from primitive route/query
values, implement a static `BindAsync` method on the type itself — minimal API model binding calls it
automatically when present:

```csharp
public sealed record SortOptions(string Field, bool Descending)
{
    public static ValueTask<SortOptions?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var field = context.Request.Query["sortField"].FirstOrDefault() ?? "CreatedAt";
        var descending = context.Request.Query["sortDesc"] == "true";
        return ValueTask.FromResult<SortOptions?>(new SortOptions(field, descending));
    }
}

app.MapGet("/orders", (SortOptions sort, IOrderRepository repository) => repository.List(sort));
```

## Binding [AsParameters] for grouping many simple parameters

`[AsParameters]` collects several route/query-bound values into a single parameter object, without
making that object a JSON request body:

```csharp
public sealed record OrderQuery(int Page, int PageSize, string? Sort);

app.MapGet("/orders", ([AsParameters] OrderQuery query, IOrderRepository repository) =>
    repository.List(query.Page, query.PageSize, query.Sort));
```

Each property on an `[AsParameters]` type binds using the same implicit/explicit rules as an ordinary
handler parameter — it's purely a way to group related bound values into one type, not a different
binding source.

## Common pitfall: two complex-type parameters with no explicit [FromBody]

```csharp
// Ambiguous: which of these two reads the request body?
app.MapPost("/orders", (CreateOrderRequest request, ShippingDetails shipping) => ...);
```

This throws at startup (or, depending on the exact shapes involved, silently binds only one and
leaves the other at its default) — mark exactly one parameter `[FromBody]` and bind the other from
route/query/services instead.
