---
name: dotnet-aspnetcore-openapi
description: Guidance on Microsoft.AspNetCore.OpenApi (verified current release 10.0.12, part of the .NET 10 shared framework/SDK) for generating OpenAPI 3.1 documents from ASP.NET Core minimal APIs and controllers — AddOpenApi/MapOpenApi setup, document transformers (IOpenApiDocumentTransformer) and operation transformers (IOpenApiOperationTransformer), including XML doc comments (<summary>/<remarks>/<param>) into the generated document, generating multiple named OpenAPI documents for API versioning, and this library's own scope as document generation only — it produces an OpenAPI JSON document and nothing else, with no bundled interactive UI. Use when adding OpenAPI generation to an ASP.NET Core app, customizing the generated document or a specific operation/schema, wiring XML doc comments into OpenAPI descriptions, or serving more than one OpenAPI document from the same app (e.g. per API version).
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# ASP.NET Core OpenAPI Document Generation

Guidance on `Microsoft.AspNetCore.OpenApi`, the built-in library that generates an OpenAPI
document describing an ASP.NET Core app's minimal API and controller endpoints. Organized by
task, not by version — the current `.NET 10`/v10.0.12 API (`AddOpenApi`, `MapOpenApi`,
transformers) is the API this skill documents throughout; each reference file notes a
version-introduced fact inline where it matters (the library shipped first in .NET 9, and .NET 10
changed its default output version).

## Scope: document generation only

This library's entire job is producing an OpenAPI **document** (a JSON file describing your API's
routes, request/response shapes, and schemas) at a `/openapi/{documentName}.json`-style endpoint.
It has no built-in interactive UI of its own — no Swagger-UI-style browsable page, no "try it out"
button. Rendering that document as a browsable page is a separate concern from generating it in the
first place, the same way generating an OpenAPI document is a separate concern from consuming one
in an API client generator. If your project wants an interactive documentation page, that's a
distinct piece of software layered on top of the JSON document this library produces — this skill
covers only the generation side.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Adding `AddOpenApi`/`MapOpenApi` to a minimal API or controller-based project | [references/core-setup.md](references/core-setup.md) |
| Modifying the generated document or a specific operation/schema in code | [references/transformers.md](references/transformers.md) |
| Getting XML `<summary>`/`<remarks>`/`<param>` comments into the generated descriptions | [references/xml-doc-comments.md](references/xml-doc-comments.md) |
| Serving more than one OpenAPI document (e.g. one per API version) from the same app | [references/versioning-multiple-documents.md](references/versioning-multiple-documents.md) |
| Asserting the generated document contains what you expect | [references/testing.md](references/testing.md) |

## Quick start

Minimal setup, current API (v10.0.12, .NET 10, OpenAPI 3.1 by default):

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi(); // serves the document at /openapi/v1.json by default

app.MapGet("/orders/{id}", (string id) => Results.Ok(new Order(id)))
    .WithName("GetOrder");

app.Run();
```

The single most common miss: expecting `MapOpenApi()` alone to give you a browsable documentation
page — it only serves the raw JSON document. See "Scope: document generation only" above.

## Out of scope

- Any interactive UI for browsing or exercising a generated OpenAPI document — this library
  produces the document; rendering it as a page is out of scope entirely.
- Generating an OpenAPI document from a non-ASP.NET-Core source (a hand-written YAML file, another
  language's web framework) — this skill covers only the ASP.NET Core reflection-based generation
  path.
- OpenAPI-to-client-code generation (producing a typed HTTP client from the document) — a
  downstream consumer of the generated document, not part of generating it.
