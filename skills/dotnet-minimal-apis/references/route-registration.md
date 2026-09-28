# Route Registration

Minimal APIs register an HTTP endpoint as a direct mapping from a route pattern and HTTP verb to a
handler delegate — no controller class, no attribute routing, no action selection step.

## The Map* methods

```csharp
var app = builder.Build();

app.MapGet("/orders/{id:guid}", (Guid id) => GetOrder(id));
app.MapPost("/orders", (CreateOrderRequest request) => CreateOrder(request));
app.MapPut("/orders/{id:guid}", (Guid id, UpdateOrderRequest request) => UpdateOrder(id, request));
app.MapDelete("/orders/{id:guid}", (Guid id) => DeleteOrder(id));
app.MapPatch("/orders/{id:guid}", (Guid id, JsonPatchDocument patch) => PatchOrder(id, patch));
```

Each `Map*` call returns an `IEndpointConventionBuilder` (specifically a `RouteHandlerBuilder`), the
handle you chain further configuration off of — metadata (`.WithName(...)`, `.WithTags(...)`),
filters (`.AddEndpointFilter(...)`), and authorization (`.RequireAuthorization(...)`) all attach this
way, covered in their own reference files.

## Route constraints and patterns

Route parameters support the same constraint syntax as the rest of ASP.NET Core's routing system —
`{id:guid}`, `{page:int:min(1)}`, `{slug:alpha}` — evaluated during route matching, before the
handler ever runs:

```csharp
app.MapGet("/orders/{id:guid}", (Guid id) => GetOrder(id));
app.MapGet("/reports/{year:int:range(2000,2100)}/{month:int:range(1,12)}", (int year, int month) => GetReport(year, month));
```

A request whose route segment fails the constraint (a non-GUID string against `{id:guid}`) doesn't
match this route at all — it falls through to the next matching route, or a 404 if none matches,
rather than reaching the handler with an unparseable value.

## Naming and describing endpoints

```csharp
app.MapGet("/orders/{id:guid}", (Guid id) => GetOrder(id))
   .WithName("GetOrderById")
   .WithSummary("Retrieves a single order by its identifier.")
   .WithTags("Orders");
```

`.WithName(...)` gives the endpoint a name usable with `LinkGenerator`/`Results.CreatedAtRoute` for
generating URLs back to it — most commonly for a `Location` header on a 201 Created response from a
`POST` handler:

```csharp
app.MapPost("/orders", (CreateOrderRequest request) =>
{
    var order = CreateOrder(request);
    return Results.CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
});
```

## Handler delegate shape

A handler can be a lambda, a local function, or a reference to a static or instance method — nothing
about minimal API registration requires the handler to be an inline lambda:

```csharp
app.MapGet("/orders/{id:guid}", OrderEndpoints.GetById);

public static class OrderEndpoints
{
    public static IResult GetById(Guid id, IOrderRepository repository)
    {
        var order = repository.Find(id);
        return order is not null ? Results.Ok(order) : Results.NotFound();
    }
}
```

Extracting handlers to static methods on a dedicated class keeps `Program.cs` from growing into a
long, undifferentiated list of inline lambdas as the number of endpoints grows — a purely
organizational choice, not a different registration mechanism.
