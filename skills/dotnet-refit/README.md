# Refit

Guidance on Refit, the attribute-driven library that turns a plain C# interface into a working
`HttpClient`-backed REST API client, covering interface definitions, DI registration, custom
serialization, authentication, file uploads, error handling, and testing.

## When to reach for it

- Defining a typed HTTP client interface with `[Get]`/`[Post]`/`[Put]`/`[Delete]`/`[Patch]`
  attributes instead of hand-rolling `HttpClient` calls.
- Wiring `AddRefitClient<T>()` into dependency injection, or choosing it over
  `RestService.For<T>()`.
- Debugging a Refit serialization mismatch, a routing attribute, or a failed request.
- Attaching headers or a bearer-token/refresh handler to outgoing Refit requests.
- Uploading a file through a `[Multipart]` interface method, or writing tests for code that depends
  on a Refit client.

## Using it

This skill is model-invoked: it fires automatically when you're defining, wiring up, debugging, or
testing a Refit-based REST client. You can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| [Get]/[Post]/[Put]/[Delete]/[Patch], route templates, [Body]/[Query]/[Header] parameters | [references/interface-definition.md](references/interface-definition.md) |
| RestService.For\<T>() vs. AddRefitClient\<T>(), ConfigureHttpClient | [references/client-generation-and-di.md](references/client-generation-and-di.md) |
| System.Text.Json, Newtonsoft.Json, and XML content serializers | [references/serialization.md](references/serialization.md) |
| Static/dynamic headers and a bearer-token DelegatingHandler | [references/headers-and-authorization.md](references/headers-and-authorization.md) |
| [Multipart], StreamPart, ByteArrayPart, FileInfoPart | [references/multipart-uploads.md](references/multipart-uploads.md) |
| ApiException, EnsureSuccessStatusCodeAttribute, resilience-handler placement | [references/error-handling.md](references/error-handling.md) |
| Faking/mocking a Refit interface, testing against a real or stubbed server | [references/testing.md](references/testing.md) |

## Example prompts

- "Define a Refit interface for this REST API's user endpoints."
- "Register this Refit client in DI with a bearer token handler."
- "Why is my Refit call throwing an ApiException on a 404 instead of returning null?"
