# Client Generation and DI

Refit builds a concrete implementation of your interface at build time via a source generator. Two
ways to obtain an instance of that generated implementation:

## `RestService.For<T>()` — no DI container

```csharp
var usersApi = RestService.For<IUsersApi>("https://api.example.com");
```

This creates its own internal `HttpClient` with no lifetime management from a container — reach
for it in a console app, a script, a quick prototype, or any code path that genuinely has no DI
container available. It does not participate in `IHttpClientFactory` pooling, so avoid constructing
a new one per call in a long-running service; cache the instance instead.

An overload accepts an existing `HttpClient`:

```csharp
var usersApi = RestService.For<IUsersApi>(httpClient);
```

## `AddRefitClient<T>()` — `IHttpClientFactory` integration

Requires the `Refit.HttpClientFactory` package in addition to `Refit`. This is the default choice
for ASP.NET Core, worker services, or anything already using `IHttpClientFactory` — it gets pooled
connections, `IHttpClientFactory`-managed handler lifetime, and standard `AddHttpMessageHandler`
composition for free:

```csharp
builder.Services
    .AddRefitClient<IUsersApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://api.example.com");
        c.Timeout = TimeSpan.FromSeconds(30);
    });
```

Inject `IUsersApi` directly into any class the container constructs — no need to inject
`IHttpClientFactory` and call `CreateClient` yourself:

```csharp
public sealed class UserService(IUsersApi api)
{
    public Task<User> GetUser(int id) => api.GetUser(id);
}
```

## Per-client `RefitSettings`

Both registration paths accept a `RefitSettings` to control serialization, exception factories, and
URL parameter formatting:

```csharp
builder.Services
    .AddRefitClient<IUsersApi>(new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)),
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
```

See [serialization.md](serialization.md) for the serializer options and
[headers-and-authorization.md](headers-and-authorization.md) for attaching a `DelegatingHandler` via
`AddHttpMessageHandler` in this same chain.

## Multiple named clients for the same interface

`AddRefitClient<T>()` registers a typed client keyed by the interface type; if two backends
implement the same interface shape, define two distinct interfaces (even if identical in
signature) rather than trying to register the same interface type twice — `IHttpClientFactory`
typed-client registration is one-per-type.
