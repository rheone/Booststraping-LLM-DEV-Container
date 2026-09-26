# Refit

Guidance on Refit, the attribute-driven declarative REST client library for .NET — the routing
table (by task, not Refit version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Refit version

| File | Covers |
| --- | --- |
| `interface-definition.md` | `[Get]`/`[Post]`/`[Put]`/`[Delete]`/`[Patch]`, route templates, `[Body]`/`[Query]`/`[Header]` parameters |
| `client-generation-and-di.md` | `RestService.For<T>()`, `AddRefitClient<T>()`, `ConfigureHttpClient`, base address/timeout setup |
| `serialization.md` | System.Text.Json (default), Newtonsoft.Json, XML content serializers |
| `headers-and-authorization.md` | Static/dynamic headers, `[Authorize]`, bearer-token `DelegatingHandler` |
| `multipart-uploads.md` | `[Multipart]`, `StreamPart`, `ByteArrayPart`, `FileInfoPart` |
| `error-handling.md` | `ApiException`, `EnsureSuccessStatusCodeAttribute`, resilience-handler placement |
| `testing.md` | Faking/mocking a Refit interface, integration-testing against a real or stubbed server |

## Scope

Refit only — declaring and consuming attribute-based REST API interfaces. Out of scope: general
`HttpClient`/`IHttpClientFactory` configuration beyond what Refit's DI extension touches, authoring
general-purpose resilience policies, and OpenAPI-to-interface code generation tooling.

Each reference file notes a version-introduced fact inline (e.g. which package version added a
given attribute); version is not the file-splitting axis for this skill (see [SKILL.md](SKILL.md)
for why).
