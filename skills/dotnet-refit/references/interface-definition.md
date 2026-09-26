# Interface Definition

Refit generates an `HttpClient`-backed implementation of any interface you decorate with its
routing attributes. The interface is the entire contract — no partial classes, no base class to
inherit.

## HTTP method attributes

```csharp
public interface IUsersApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);

    [Post("/users")]
    Task<User> CreateUser([Body] CreateUserRequest request);

    [Put("/users/{id}")]
    Task UpdateUser(int id, [Body] UpdateUserRequest request);

    [Patch("/users/{id}")]
    Task PatchUser(int id, [Body] JsonPatchDocument<User> patch);

    [Delete("/users/{id}")]
    Task DeleteUser(int id);
}
```

Each attribute takes the relative route as its constructor argument. Route placeholders (`{id}`)
bind to a method parameter of the same name, matched by name, not position — parameter order in
the method signature doesn't need to match the route.

## Query parameters

A parameter that isn't part of the route template and isn't marked `[Body]` becomes a query string
parameter automatically:

```csharp
[Get("/users")]
Task<List<User>> SearchUsers(string? name, int page = 1);
// GET /users?name=...&page=1
```

Use `[Query]` to flatten an object's public properties into individual query parameters, and
`[Query(CollectionFormat.Multi)]` to control how a collection parameter serializes (`?tag=a&tag=b`
vs. a single comma-joined value):

```csharp
[Get("/users")]
Task<List<User>> SearchUsers([Query] UserSearchCriteria criteria);

[Get("/users")]
Task<List<User>> FilterByTags([Query(CollectionFormat.Multi)] IEnumerable<string> tags);
```

## Body parameters

Exactly one parameter per method may carry `[Body]`. By default the parameter serializes with the
client's configured content serializer (System.Text.Json unless you've swapped it — see
[serialization.md](serialization.md)). Pass `[Body(BodySerializationMethod.UrlEncoded)]` to send the
object as `application/x-www-form-urlencoded` instead, common for OAuth token endpoints:

```csharp
[Post("/oauth/token")]
Task<TokenResponse> GetToken([Body(BodySerializationMethod.UrlEncoded)] TokenRequest request);
```

## Headers on a single method

`[Headers("Cache-Control: no-cache")]` on a method adds a static header to every call; a `[Header]`
parameter attribute lets a caller supply a header value per call. See
[headers-and-authorization.md](headers-and-authorization.md) for both, plus authorization headers
applied client-wide.

## Returning the raw response

Return `Task<ApiResponse<T>>` instead of `Task<T>` when you need the status code, response
headers, or the raw `HttpResponseMessage` alongside the deserialized body — useful for paging via
`Link`/`X-Total-Count` headers, or for endpoints that return `204 No Content` on success and you
still need to distinguish that from a failure:

```csharp
[Get("/users/{id}")]
Task<ApiResponse<User>> GetUserWithHeaders(int id);
```

`ApiResponse<T>.IsSuccessStatusCode`, `.StatusCode`, `.Headers`, and `.Content` (the deserialized
`T`, or `default` on failure) are all available without Refit throwing — this bypasses the
`ApiException`-throwing behavior described in [error-handling.md](error-handling.md) entirely for
this one method.

## Cancellation

Add a `CancellationToken` parameter anywhere in the method signature; Refit recognizes it by type,
not position, and wires it into the underlying `HttpClient` call.

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id, CancellationToken cancellationToken);
```
