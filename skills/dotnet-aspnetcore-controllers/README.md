# ASP.NET Core Controllers

Guidance on controller-based ASP.NET Core Web APIs: `ControllerBase` and `[ApiController]`'s
conventions, attribute routing, model binding, action filters, model validation, and content
negotiation.

## When to reach for it

- You're writing or reviewing a controller-based API endpoint.
- You're debugging an unexpected automatic `400` response, or a model-binding surprise on an
  action parameter.
- You're adding an action filter, or configuring how a controller negotiates its response format.

## Using it

This skill fires automatically when your request involves writing a controller, debugging model
binding or validation, or adding a filter. You can also invoke it directly with
`/dotnet-aspnetcore-controllers`.

## What it covers

| Topic | Reference |
| --- | --- |
| `ControllerBase`, `[ApiController]`'s conventions, registering controller services | [references/controllers-and-apicontroller.md](references/controllers-and-apicontroller.md) |
| `[Route]`/`[HttpGet]`/etc., route constraints, naming routes for URL generation | [references/attribute-routing.md](references/attribute-routing.md) |
| Binding parameters from route/query/body/services, custom model binders | [references/model-binding.md](references/model-binding.md) |
| `IActionFilter`/`IAsyncActionFilter` and the MVC filter pipeline's stage ordering | [references/action-filters.md](references/action-filters.md) |
| `ModelState`, the automatic 400 under `[ApiController]`, `IValidatableObject` | [references/model-validation.md](references/model-validation.md) |
| Output/input formatters, `[Produces]`, `[ProducesResponseType]` | [references/content-negotiation.md](references/content-negotiation.md) |
| Unit and integration testing controllers and filters | [references/testing.md](references/testing.md) |

## Example prompts

- "Why does this endpoint return 400 before my action code even runs?"
- "Add an action filter that logs every request handled by this controller."
- "Should this parameter be bound with [FromQuery] or [FromBody] here?"
