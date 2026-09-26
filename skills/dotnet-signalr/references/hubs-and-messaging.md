# Hubs and Messaging

## The Hub class

A `Hub` is the server-side endpoint clients connect to; each public method on it is callable from a
connected client, and the hub's `Clients` property lets you push messages back out:

```csharp
public sealed class ChatHub : Hub
{
    public async Task SendMessage(string user, string message) =>
        await Clients.All.SendAsync("ReceiveMessage", user, message);
}
```

Register it with `builder.Services.AddSignalR()` and expose it at an endpoint with
`app.MapHub<ChatHub>("/hubs/chat")`. A new `Hub` instance is created per method invocation (hubs are
not meant to hold long-lived state across calls in instance fields) — anything that needs to persist
across the connection's lifetime belongs in a separate, DI-injected service the hub calls into, or
in `Context.Items` for per-connection state scoped to that connection.

## Hub\<T> for strongly-typed hubs

`Hub<T>` replaces the stringly-typed `SendAsync("MethodName", args)` call with a compile-time-checked
interface describing what the client can be told to do:

```csharp
public interface IChatClient
{
    Task ReceiveMessage(string user, string message);
}

public sealed class ChatHub : Hub<IChatClient>
{
    public async Task SendMessage(string user, string message) =>
        await Clients.All.ReceiveMessage(user, message);
}
```

`Clients.All.ReceiveMessage(user, message)` is checked against `IChatClient` at compile time — a
renamed or retyped client method is caught by the compiler instead of failing silently at runtime
the way a typo'd string method name in the untyped `SendAsync` form would. Prefer `Hub<T>` for any
hub with more than one or two client-callable message shapes.

## Targeting recipients

`Clients` exposes several ways to choose who receives a message:

- **`Clients.All`** — every currently connected client.
- **`Clients.Caller`** — only the connection that invoked the current hub method.
- **`Clients.Others`** — every connection except the caller.
- **`Clients.Client(connectionId)`** — one specific connection by its `ConnectionId`.
- **`Clients.Group(groupName)`** / **`Clients.Groups(groupNames)`** — every connection currently in a
  named group (see [groups-and-connections.md](groups-and-connections.md)).
- **`Clients.User(userId)`** / **`Clients.Users(userIds)`** — every connection associated with a
  given user identity (a user can have more than one active connection — multiple browser tabs,
  multiple devices — and `Clients.User` reaches all of them). This requires the hub's
  `Context.UserIdentifier` to resolve to a stable value per user, which by default comes from the
  `ClaimTypes.NameIdentifier` claim on the authenticated connection.

```csharp
public async Task NotifyOrderShipped(string userId, string orderId) =>
    await Clients.User(userId).ReceiveMessage("system", $"Order {orderId} shipped");
```

## Exceptions from a hub method

An unhandled exception thrown from a hub method is caught by SignalR and converted into an error sent
back to the calling client as a failed invocation — the client-side call awaiting the invocation
throws a `HubException`-derived error on the caller. By default, the actual exception details (type,
message, stack trace) are not sent to the client, to avoid leaking server internals; throwing
`HubException` explicitly with a message you intend the client to see is the supported way to
surface a specific error message across the wire.
