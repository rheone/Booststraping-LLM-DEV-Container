# Model Binding

Model binding populates an action method's parameters from the incoming request — route values,
query string, request body, headers, and form data — following a set of inference rules that
`[ApiController]` sharpens compared to plain MVC binding.

## Binding source attributes

```csharp
[HttpPost("{id:guid}/items")]
public IActionResult AddItem(
    [FromRoute] Guid id,
    [FromBody] AddItemRequest request,
    [FromServices] IOrderRepository repository,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
{
    return Ok(repository.AddItem(id, request, idempotencyKey));
}
```

- **`[FromRoute]`** — from a matched route template segment.
- **`[FromQuery]`** — from the query string.
- **`[FromBody]`** — deserialized (JSON, by default) from the request body; at most one parameter per
  action can bind from the body.
- **`[FromServices]`** — resolved from the DI container, an alternative to constructor injection for
  a dependency needed by only one action.
- **`[FromHeader]`** — from a named request header.
- **`[FromForm]`** — from form-encoded (`multipart/form-data` or `application/x-www-form-urlencoded`)
  request data, including uploaded files via `IFormFile`.

## Inference under [ApiController]

With `[ApiController]` applied, explicit attributes are frequently unnecessary — the framework infers
a binding source automatically:

- A single complex-type parameter with no attribute infers `[FromBody]`.
- A simple-type parameter matching a route template segment infers `[FromRoute]`.
- A simple-type parameter with no route match infers `[FromQuery]`.
- A parameter type registered in DI infers `[FromServices]`.

```csharp
[HttpPost("{id:guid}/items")]
public IActionResult AddItem(Guid id, AddItemRequest request) // id: route, request: body — both inferred
{
    return Ok(repository.AddItem(id, request));
}
```

Explicit attributes remain the right call whenever the implicit rule would guess wrong, or purely for
readability at the point of a code review — inference doesn't forbid being explicit, it just makes it
optional in the common cases.

## Binding complex types from the query string

A complex type with no `[FromBody]` and no full route match binds from the query string by matching
its public properties to individual query parameters:

```csharp
public sealed record OrderQuery(int Page, int PageSize, string? Sort);

[HttpGet]
public IActionResult List([FromQuery] OrderQuery query) => Ok(GetOrders(query));
```

`GET /api/orders?page=1&pageSize=20&sort=CreatedAt` populates `OrderQuery` by matching each query key
to the matching property name, case-insensitively.

## Custom model binders

For a parameter type the built-in binders don't handle correctly, implement `IModelBinder` and
register it via a `ModelBinderAttribute` or a custom `IModelBinderProvider`:

```csharp
public sealed class SortOptionsModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var field = bindingContext.ValueProvider.GetValue("sortField").FirstValue ?? "CreatedAt";
        var descending = bindingContext.ValueProvider.GetValue("sortDesc").FirstValue == "true";
        bindingContext.Result = ModelBindingResult.Success(new SortOptions(field, descending));
        return Task.CompletedTask;
    }
}

[HttpGet]
public IActionResult List([ModelBinder(typeof(SortOptionsModelBinder))] SortOptions sort) => Ok(GetOrders(sort));
```

Reserve a custom `IModelBinder` for a type genuinely needing bespoke construction logic — most
scenarios are covered by the built-in binders plus, at most, a type implementing `TryParse` for
simple-type conversion from a single string value.
