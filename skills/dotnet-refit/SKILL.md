---
name: dotnet-refit
description: Guidance on the Refit NuGet package (verified current release 16.1.0, targets .NET Standard 2.0+, .NET 8/9/10/11, and .NET Framework 4.6.2+) for declarative REST API clients in C#/.NET — defining interfaces with [Get]/[Post]/[Put]/[Delete]/[Patch] attributes, generating an implementation via RestService.For<T>() or the Refit.HttpClientFactory AddRefitClient<T>() DI registration, customizing request/response serialization (System.Text.Json, Newtonsoft.Json, XML), attaching headers and per-request or global authorization via DelegatingHandler, uploading files with [Multipart]/StreamPart/ByteArrayPart/FileInfoPart, handling failures via ApiException, and testing code that consumes a Refit interface. Use when defining a typed HTTP client interface, wiring up AddRefitClient in DI, debugging a Refit serialization or routing attribute, adding auth headers to outgoing Refit requests, uploading a file through a Refit interface, or writing tests against code that depends on a Refit-generated client.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Refit

Guidance on Refit, the attribute-driven library that turns a C# interface into a working
`HttpClient`-backed REST API client. Organized by task, not by Refit version — the attribute-based
interface shape has been stable across recent major versions; each reference file notes a
version-introduced fact inline where it matters.

## Quick start

```csharp
public interface IGitHubApi
{
    [Get("/repos/{owner}/{repo}")]
    Task<Repository> GetRepository(string owner, string repo);
}

// Program.cs — DI registration (needs the Refit.HttpClientFactory package)
builder.Services
    .AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"));

// Usage
public sealed class RepoService(IGitHubApi api)
{
    public Task<Repository> GetRepo() => api.GetRepository("dotnet", "runtime");
}
```

Outside DI (a script, a console app, a quick prototype), `RestService.For<IGitHubApi>("https://api.github.com")`
builds the same generated implementation without a container.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Defining interface methods with `[Get]`/`[Post]`/`[Put]`/`[Delete]`/`[Patch]`, route templates, query/body parameters | [references/interface-definition.md](references/interface-definition.md) |
| Choosing `RestService.For<T>()` vs. `AddRefitClient<T>()`, configuring the underlying `HttpClient` | [references/client-generation-and-di.md](references/client-generation-and-di.md) |
| Switching serializers (System.Text.Json, Newtonsoft.Json, XML), controlling property naming/casing | [references/serialization.md](references/serialization.md) |
| Adding static or per-request headers, wiring a bearer-token/refresh `DelegatingHandler` | [references/headers-and-authorization.md](references/headers-and-authorization.md) |
| Uploading a file or form fields with `[Multipart]` | [references/multipart-uploads.md](references/multipart-uploads.md) |
| Handling a non-success response, reading `ApiException` details, retry/resilience placement | [references/error-handling.md](references/error-handling.md) |
| Unit- or integration-testing code that depends on a Refit interface | [references/testing.md](references/testing.md) |

## Out of scope

- General `HttpClient`/`IHttpClientFactory` configuration not specific to Refit (named/typed
  clients, `SocketsHttpHandler` tuning) — only the parts Refit's DI extension touches are covered
  in [references/client-generation-and-di.md](references/client-generation-and-di.md).
- General-purpose resilience policies (retry, circuit breaker) — [references/error-handling.md](references/error-handling.md)
  notes only where a resilience handler slots into a Refit client's `DelegatingHandler` pipeline,
  not how to author one.
- OpenAPI/Swagger-to-Refit-interface code generation tooling — out of scope; this skill covers
  hand-written Refit interfaces only.
