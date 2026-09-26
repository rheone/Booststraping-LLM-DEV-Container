# ASP.NET Core Controllers

Guidance on controller-based ASP.NET Core Web APIs — the routing table (by task) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per topic

| File | Covers |
| --- | --- |
| `controllers-and-apicontroller.md` | `ControllerBase`, `[ApiController]`'s conventions, registering controller services |
| `attribute-routing.md` | `[Route]`/`[HttpGet]`/etc., route constraints, naming routes for URL generation |
| `model-binding.md` | `[FromRoute]`/`[FromQuery]`/`[FromBody]`/`[FromServices]`/`[FromHeader]`/`[FromForm]`, inference under `[ApiController]`, custom model binders |
| `action-filters.md` | `IActionFilter`/`IAsyncActionFilter`, registration, the MVC filter pipeline's stage ordering |
| `model-validation.md` | `ModelState`, automatic 400 under `[ApiController]`, `IValidatableObject` |
| `content-negotiation.md` | Output/input formatters, `[Produces]`, `[ProducesResponseType]` |
| `testing.md` | Unit testing controllers/filters, integration testing with `WebApplicationFactory` |

## Scope

Controller-based ASP.NET Core Web APIs as their own convention: `ControllerBase`/`[ApiController]`,
attribute routing, model binding, action filters, model validation, and content negotiation. Out of
scope: Razor views/Pages and other MVC-page-rendering concerns, and OpenAPI document generation
configuration itself.

Each reference file notes a version-specific fact inline where one applies; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).
