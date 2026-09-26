# Error Handling

## `ApiException` on non-success responses

By default, every method returning `Task<T>` (not `Task<ApiResponse<T>>`) throws `ApiException`
when the response status code isn't successful. Catch it at the call site, not deep inside
business logic that shouldn't know it's talking to Refit:

```csharp
try
{
    var user = await usersApi.GetUser(id);
}
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    return null;
}
```

`ApiException` carries:

- `StatusCode` — the response's `HttpStatusCode`.
- `Content` — the raw response body as a string, already read and buffered (safe to inspect after
  the exception is caught; the underlying response stream is not still open).
- `Headers` — the response headers.
- `GetContentAsAsync<T>()` — deserializes `Content` into `T` using the client's configured
  serializer, for APIs that return a structured error payload (a problem-details body, a
  validation-error list) rather than plain text.

```csharp
catch (ApiException ex)
{
    var problem = await ex.GetContentAsAsync<ProblemDetails>();
    logger.LogWarning("Request failed: {Title} ({Status})", problem?.Title, ex.StatusCode);
    throw;
}
```

## Avoiding the exception entirely: `ApiResponse<T>`

For a code path that treats a non-2xx response as an expected outcome rather than an exceptional
one (a 404 that just means "not found yet," a 409 that means "already exists"), return
`ApiResponse<T>` instead of `T` — see [interface-definition.md](interface-definition.md). This
avoids using exceptions for expected control flow and is materially cheaper when the non-success
path is common:

```csharp
[Get("/users/{id}")]
Task<ApiResponse<User>> TryGetUser(int id);
```

```csharp
var response = await usersApi.TryGetUser(id);
if (!response.IsSuccessStatusCode)
{
    return response.StatusCode == HttpStatusCode.NotFound ? null : throw response.Error!;
}
return response.Content;
```

`response.Error` holds the same `ApiException` Refit would otherwise have thrown, so both styles
share the same exception type and shape.

## Where a resilience handler belongs

Retry, circuit-breaker, and timeout policies attach as an ordinary `DelegatingHandler` in the same
`AddHttpMessageHandler` chain described in
[headers-and-authorization.md](headers-and-authorization.md) — they operate below Refit, on the
raw `HttpResponseMessage`, before Refit ever inspects the status code to decide whether to throw
`ApiException`. Placing a resilience handler here means a retried-and-eventually-successful request
never surfaces `ApiException` to calling code at all; only the final, still-unsuccessful response
reaches Refit's own status-code check.

## Deserialization failures are not `ApiException`

A response with a success status code but a body that fails to deserialize into the interface's
declared return type throws the underlying serializer's own exception (e.g.
`JsonException`/`NotSupportedException`), not `ApiException` — `ApiException` is strictly about the
HTTP status code, never about a malformed-but-2xx body.
