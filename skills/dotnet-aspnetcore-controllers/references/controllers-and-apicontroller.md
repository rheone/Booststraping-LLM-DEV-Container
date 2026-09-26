# ControllerBase and [ApiController]

A controller-based Web API groups related action methods into a class deriving from
`ControllerBase`, decorated with `[ApiController]` to opt into a set of API-specific conventions that
change default behavior across the whole class.

## Defining a controller

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController(IOrderRepository repository) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var order = repository.Find(id);
        return order is not null ? Ok(order) : NotFound();
    }
}
```

`ControllerBase` (rather than `Controller`, which adds view-rendering support for MVC pages) is the
base type for an API controller — it supplies `Ok()`, `NotFound()`, `BadRequest()`, `CreatedAtAction()`,
and the rest of the `IActionResult`-producing helper methods, plus access to `HttpContext`,
`User`, `ModelState`, and `Request`/`Response`, without pulling in Razor view support the API surface
never uses.

## What [ApiController] changes

Applying `[ApiController]` to a controller class turns on several conventions at once, all of which
apply automatically without further configuration:

- **Automatic 400 on invalid model state** — an action whose bound parameters fail validation returns
  a 400 with a structured problem-details body before the action method body ever runs. See
  [model-validation.md](model-validation.md).
- **Binding source inference** — a complex type parameter with no explicit binding attribute infers
  `[FromBody]` automatically (for actions taking a single complex-type parameter), and simple types
  infer `[FromRoute]` when the name matches a route parameter, otherwise `[FromQuery]`. See
  [model-binding.md](model-binding.md).
- **Attribute routing requirement** — every action must be reachable through attribute routing
  (`[Route]`/`[HttpGet]`/etc.); convention-based routing (defining routes centrally in
  `Program.cs` for a set of controllers) is not supported alongside `[ApiController]`.
- **Problem-details responses for error status codes** — client and server error responses
  automatically use the `application/problem+json` (RFC 9457) shape rather than an empty body,
  unless you've overridden the behavior explicitly.

## Applying [ApiController] at the assembly level

Rather than repeating `[ApiController]` on every controller class, apply it once via an assembly
attribute alongside `AddControllers()`:

```csharp
// AssemblyInfo.cs or any file in the project
[assembly: ApiController]
```

This is uncommon in practice — most codebases apply `[ApiController]` per-class for visibility at the
point where the conventions actually take effect, but the assembly-level attribute is a legitimate
way to enforce it uniformly across every controller in a project.

## Registering controller services

```csharp
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
```

`AddControllers()` registers the controller-discovery and action-invocation infrastructure;
`MapControllers()` (called after `UseRouting`/`UseAuthorization` in the middleware pipeline, or via
minimal hosting's implicit ordering) wires the discovered controllers' attribute routes into the
endpoint route table.
