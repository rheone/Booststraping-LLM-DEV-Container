# Authentication and Authorization

## Securing a hub

`[Authorize]` on a hub class requires every connection to the hub to be authenticated; placed on an
individual hub method instead, it restricts just that method while leaving the rest of the hub open:

```csharp
[Authorize]
public sealed class ChatHub : Hub
{
    public async Task SendMessage(string message) =>
        await Clients.All.SendAsync("ReceiveMessage", Context.User!.Identity!.Name, message);

    [Authorize(Policy = "RequireAdmin")]
    public async Task DeleteMessage(string messageId) => /* ... */;
}
```

A hub with no `[Authorize]` attribute at all accepts connections from any client that can reach the
endpoint, authenticated or not — SignalR does not require authentication by default the way some
other ASP.NET Core middleware pipelines assume. Add `[Authorize]` explicitly on every hub that isn't
genuinely meant to be public.

## The access token for non-header transports

Ordinary ASP.NET Core authentication reads the bearer token from the `Authorization` header, which
works for the initial negotiate request but not for the WebSocket and Server-Sent Events transports
browsers use afterward — those transports can't set custom headers. SignalR's client sends the token
as an `access_token` query string value instead for those transports, and the server side needs to
be told to also accept the token from there:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

Scope the path check (`path.StartsWithSegments("/hubs")` above) to your actual hub route prefix —
accepting a query-string token on every request, not just hub negotiation/transport requests, widens
where a token could leak into logs (query strings end up in web server access logs far more often
than headers do).

## Context.User and Context.UserIdentifier

Inside a hub method, `Context.User` is the same `ClaimsPrincipal` ASP.NET Core authentication
populates elsewhere in the pipeline — read claims from it the same way you would in a controller.
`Context.UserIdentifier` is what `Clients.User(...)` matches against (see
[hubs-and-messaging.md](hubs-and-messaging.md)); it defaults to the `ClaimTypes.NameIdentifier`
claim and can be customized by implementing `IUserIdProvider` and registering it in DI if your
identity scheme uses a different claim as the stable per-user key.

## Policy-based authorization inside a hub method

For an authorization decision that depends on runtime state `[Authorize]`'s declarative form can't
express (checking a specific resource the caller is trying to act on), inject `IAuthorizationService`
and call it explicitly from within the method body, the same pattern used outside SignalR:

```csharp
public sealed class DocumentHub(IAuthorizationService authorizationService) : Hub
{
    public async Task EditDocument(string documentId)
    {
        var result = await authorizationService.AuthorizeAsync(Context.User!, documentId, "CanEdit");
        if (!result.Succeeded)
        {
            throw new HubException("Not authorized to edit this document.");
        }
        // ...
    }
}
```
