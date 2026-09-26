---
name: dotnet-aspnetcore-controllers
description: Guidance on controller-based ASP.NET Core Web APIs (verified current against ASP.NET Core 10.0) — ControllerBase and [ApiController]'s conventions (automatic 400, binding-source inference, attribute-routing requirement, problem-details error responses), attribute routing with [Route]/[HttpGet]/etc. and route naming for URL generation, model binding and [FromBody]/[FromQuery]/[FromRoute]/[FromServices], action filters (IActionFilter/IAsyncActionFilter) and MVC's fixed filter-pipeline ordering (authorization/resource/action/exception/result), model validation via ModelState and IValidatableObject, and content negotiation via output/input formatters. Use when writing or reviewing a controller-based API endpoint, debugging an [ApiController] automatic-400 or model-binding surprise, adding an action filter, or configuring response format negotiation.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# ASP.NET Core Controllers

Guidance on controller-based ASP.NET Core Web APIs — `ControllerBase`/`[ApiController]`, attribute
routing, model binding, action filters, model validation, and content negotiation, as their own
convention with its own scope and tradeoffs. Organized by task, not by ASP.NET Core version — the
`[ApiController]`/filter-pipeline surface has been stable across recent releases; each reference file
notes a version-specific fact inline where one applies.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Defining a controller, understanding what `[ApiController]` changes | [references/controllers-and-apicontroller.md](references/controllers-and-apicontroller.md) |
| Declaring routes with `[Route]`/`[HttpGet]`/etc., naming a route for URL generation | [references/attribute-routing.md](references/attribute-routing.md) |
| Binding action parameters from route/query/body/services, custom model binders | [references/model-binding.md](references/model-binding.md) |
| Writing cross-cutting logic around action execution with `IActionFilter`/`IAsyncActionFilter` | [references/action-filters.md](references/action-filters.md) |
| Validating a bound model, understanding the automatic 400 behavior, cross-property validation | [references/model-validation.md](references/model-validation.md) |
| Configuring response format negotiation (JSON/XML), declaring response shapes for OpenAPI | [references/content-negotiation.md](references/content-negotiation.md) |
| Unit or integration testing controllers and filters | [references/testing.md](references/testing.md) |

## Quick start

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController(IOrderRepository repository) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetOrderById")]
    public IActionResult GetById(Guid id)
    {
        var order = repository.Find(id);
        return order is not null ? Ok(order) : NotFound();
    }

    [HttpPost]
    public IActionResult Create(CreateOrderRequest request)
    {
        var order = repository.Create(request);
        return CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
    }
}
```

The single most common surprise: `[ApiController]` returns an automatic 400 for an invalid bound
model *before* the action body runs — a manual `if (!ModelState.IsValid)` check inside the action is
redundant (though harmless) once `[ApiController]` is applied, and its absence on a non-`[ApiController]`
controller means that check is required instead. See
[references/model-validation.md](references/model-validation.md).

## Out of scope

- Razor views, Razor Pages, and other MVC-page-rendering concerns — this skill covers the
  `ControllerBase`/API surface, not `Controller`'s view-rendering additions.
- OpenAPI document generation configuration itself (title, servers, security scheme definitions) —
  out of scope beyond noting, in
  [references/content-negotiation.md](references/content-negotiation.md), how `[ProducesResponseType]`
  shapes what a generator infers.
