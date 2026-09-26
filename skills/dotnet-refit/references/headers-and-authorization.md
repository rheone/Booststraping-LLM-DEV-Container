# Headers and Authorization

## Static headers

`[Headers]` on the interface applies to every method; on a specific method it applies only there:

```csharp
[Headers("User-Agent: MyApp/1.0")]
public interface IUsersApi
{
    [Headers("Cache-Control: no-cache")]
    [Get("/users/{id}")]
    Task<User> GetUser(int id);
}
```

## Per-call dynamic headers

A `[Header("Name")]` parameter supplies a header value at call time:

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id, [Header("X-Correlation-Id")] string correlationId);
```

`[HeaderCollection]` accepts an `IDictionary<string, string>` parameter for a variable set of
headers not known until call time.

## Authorization: prefer a `DelegatingHandler`, not a header parameter

For a bearer token (or any credential fetched from somewhere other than the immediate caller —
a token cache, an `IHttpContextAccessor`, a refresh flow), attach a `DelegatingHandler` to the
`HttpClient` pipeline rather than threading a token through every interface method:

```csharp
public sealed class BearerTokenHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
```

Register it on the same `AddRefitClient<T>()` chain used in
[client-generation-and-di.md](client-generation-and-di.md):

```csharp
builder.Services.AddTransient<BearerTokenHandler>();

builder.Services
    .AddRefitClient<IUsersApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddHttpMessageHandler<BearerTokenHandler>();
```

This keeps every interface method free of an auth parameter, and the handler composes normally
with any other message handler in the same pipeline (logging, a resilience handler) — handler
order in `AddHttpMessageHandler` calls is outermost-first, matching standard
`IHttpClientFactory` handler chaining.

## `[Authorize]` attribute shortcut

Refit ships an `[Authorize("Bearer")]` method/interface attribute that accepts a token supplied as
a plain string parameter — useful only when the token is already in hand at the call site with no
refresh logic needed. For anything beyond a static, already-fetched token, the `DelegatingHandler`
approach above stays the one source of truth and avoids the token ending up duplicated at every
call site.
