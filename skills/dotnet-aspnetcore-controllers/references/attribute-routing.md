# Attribute Routing

Attribute routing declares each controller's and action's route directly on the class/method via
`[Route]` and the HTTP-verb attributes (`[HttpGet]`, `[HttpPost]`, and so on), rather than matching
requests against a centrally defined route table.

## Class-level and action-level routes

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(GetAllOrders());

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id) => Ok(GetOrder(id));

    [HttpPost]
    public IActionResult Create(CreateOrderRequest request) => Ok(CreateOrder(request));

    [HttpGet("{id:guid}/items")]
    public IActionResult GetItems(Guid id) => Ok(GetOrderItems(id));
}
```

`[controller]` in the class-level `[Route]` template is a token replaced with the controller's name
minus its `Controller` suffix (`OrdersController` → `orders`) — a convention that keeps routes in
sync with the class name automatically, though nothing requires using it over a literal string.

An action-level route template is appended to the class-level template when it doesn't start with
`/`; a template starting with `/` replaces the class-level prefix entirely for that one action:

```csharp
[HttpGet("/health")] // ignores the class-level "api/[controller]" prefix entirely
public IActionResult Health() => Ok();
```

## HTTP verb attributes

`[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]` each combine a route template
argument with an implicit HTTP method constraint — an action decorated `[HttpGet("{id:guid}")]`
matches only GET requests to that route, not any other verb.

## Route constraints and parameters

Route templates support the same constraint syntax used anywhere else in ASP.NET Core's routing
system:

```csharp
[HttpGet("{id:guid}")]
public IActionResult GetById(Guid id) => Ok(GetOrder(id));

[HttpGet("by-date/{year:int:range(2000,2100)}/{month:int:range(1,12)}")]
public IActionResult ByDate(int year, int month) => Ok(GetOrdersByDate(year, month));
```

A request whose route segment fails a constraint doesn't match that action — it falls through to
another matching route or a 404, rather than reaching the action with an invalid value.

## Route naming for URL generation

```csharp
[HttpGet("{id:guid}", Name = "GetOrderById")]
public IActionResult GetById(Guid id) => Ok(GetOrder(id));

[HttpPost]
public IActionResult Create(CreateOrderRequest request)
{
    var order = CreateOrder(request);
    return CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
}
```

`Name` on a route attribute is what `CreatedAtRoute`/`Url.RouteUrl`/`LinkGenerator` reference when
generating a URL back to that specific action — the standard way to populate a `Location` header on a
201 Created response without hand-building the URL string.

## Multiple route templates on one action

An action can be reachable from more than one route by stacking multiple route attributes:

```csharp
[HttpGet("{id:guid}")]
[HttpGet("legacy/{id:int}")]
public IActionResult GetById(Guid id) => Ok(GetOrder(id));
```

Each attribute is evaluated independently during route matching — the action runs identically
regardless of which route matched, since routing has already resolved parameter values into the
method's arguments by the time the action body executes.
