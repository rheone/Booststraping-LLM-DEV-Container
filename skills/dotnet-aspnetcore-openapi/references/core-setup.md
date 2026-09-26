# Core Setup

`Microsoft.AspNetCore.OpenApi` ships as part of the .NET 10 shared framework/SDK (current version
10.0.12) and generates an OpenAPI document by reflecting over your app's registered endpoints —
minimal API route handlers and MVC controller actions alike.

## Minimal API setup

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();

app.MapGet("/orders/{id}", (string id) => Results.Ok(new Order(id, "pending")))
    .WithName("GetOrder")
    .WithTags("Orders");

app.Run();
```

`AddOpenApi()` registers the document-generation services; `MapOpenApi()` adds the endpoint that
serves the generated document, defaulting to `/openapi/{documentName}.json` where `documentName`
defaults to `v1`.

## Controller-based (MVC) setup

The same two calls apply identically to a controller-based app — the library reflects over
`[ApiController]` actions the same way it reflects over minimal API route handlers:

```csharp
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapControllers();
app.MapOpenApi();
```

## What gets included automatically

Without any further configuration, the generated document already includes:

- Every mapped route's HTTP method, path (including route parameters), and a stable
  `operationId` derived from `WithName(...)` (minimal APIs) or the action/controller name (MVC).
- Request and response schemas inferred from parameter types, `[FromBody]`/binding-source
  attributes, and the action's declared return type (`Results<T1, T2>`, `ActionResult<T>`, or a
  plain return type).
- `[Tags]`/`WithTags(...)` groupings, used to organize operations in the generated document.

## Restricting which endpoints are included

Exclude a specific endpoint from the generated document with `ExcludeFromDescription()`:

```csharp
app.MapGet("/internal/health", () => Results.Ok())
    .ExcludeFromDescription();
```

## OpenAPI version: 3.1 by default in .NET 10

.NET 10 generates OpenAPI **3.1** documents by default (earlier .NET 9 releases of this library
defaulted to 3.0). Override the emitted version explicitly through the document's options when a
consuming tool requires a specific version:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});
```

Set this only when a downstream consumer of the document (a code generator, a validation tool)
specifically requires 3.0 — otherwise the current default (3.1) is the right choice for a new
project.

## Serving the document only in specific environments

A common pattern restricts the OpenAPI endpoint to non-production environments, since the document
can reveal internal route/schema shape that a production deployment may not want publicly exposed:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

Apply this per your own project's exposure requirements — nothing in the library itself restricts
the endpoint by environment; it's an ordinary conditional route registration.
