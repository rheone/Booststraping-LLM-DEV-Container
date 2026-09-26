# ASP.NET Core OpenAPI Document Generation

Guidance on `Microsoft.AspNetCore.OpenApi`, the built-in library that generates an OpenAPI
document from an ASP.NET Core app's minimal API and controller endpoints: setup, document and
operation transformers, and pulling XML doc comments into the generated descriptions.

## When to reach for it

- You're adding OpenAPI document generation to a new or existing ASP.NET Core project.
- You need to customize a generated schema or operation (renaming a field, adding an example,
  excluding an endpoint) through a transformer.
- You're serving more than one OpenAPI document from the same app, such as one per API version.

## Using it

This skill fires automatically when your request involves adding OpenAPI generation, customizing
a generated document, or wiring XML comments into it. You can also invoke it directly with
`/dotnet-aspnetcore-openapi`.

## What it covers

| Topic | Reference |
| --- | --- |
| `AddOpenApi`/`MapOpenApi` on a minimal API or controller-based project | [references/core-setup.md](references/core-setup.md) |
| `IOpenApiDocumentTransformer`, `IOpenApiOperationTransformer`, schema transformers | [references/transformers.md](references/transformers.md) |
| Enabling `GenerateDocumentationFile` and mapping `<summary>`/`<remarks>`/`<param>` | [references/xml-doc-comments.md](references/xml-doc-comments.md) |
| Named documents and per-version document groups | [references/versioning-multiple-documents.md](references/versioning-multiple-documents.md) |
| Asserting the generated document's shape in tests | [references/testing.md](references/testing.md) |

## Example prompts

- "Add OpenAPI document generation to this minimal API project."
- "Pull my XML doc comments into the generated OpenAPI descriptions."
- "I need two OpenAPI documents: one for v1 and one for v2 of this API."
