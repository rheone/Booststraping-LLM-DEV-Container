# ASP.NET Core OpenAPI Document Generation

Guidance on `Microsoft.AspNetCore.OpenApi` — the routing table (by task, not package version) is
in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per package version

| File | Covers |
| --- | --- |
| `core-setup.md` | `AddOpenApi`, `MapOpenApi`, minimal API vs. controller support |
| `transformers.md` | `IOpenApiDocumentTransformer`, `IOpenApiOperationTransformer`, schema transformers |
| `xml-doc-comments.md` | enabling `GenerateDocumentationFile`, `<summary>`/`<remarks>`/`<param>` mapping |
| `versioning-multiple-documents.md` | named documents, per-version document groups |
| `testing.md` | asserting on the generated document's shape in tests |

## Scope

Document generation only — producing an OpenAPI JSON document from ASP.NET Core's minimal API and
controller endpoints. Out of scope: any interactive UI for browsing the generated document,
non-ASP.NET-Core OpenAPI generation sources, and OpenAPI-to-client-code generation (see
[SKILL.md](SKILL.md) for why).

Each reference file notes a version-introduced fact inline (e.g. the library's .NET 9 introduction,
.NET 10's default OpenAPI 3.1 output); version is not the file-splitting axis for this skill (see
[SKILL.md](SKILL.md)).
