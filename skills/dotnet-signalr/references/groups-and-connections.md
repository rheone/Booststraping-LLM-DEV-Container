# Groups and Connection Management

## Groups

A group is a named set of connections you manage explicitly — SignalR has no built-in notion of
"groups a user should automatically belong to"; you add and remove connections yourself, typically
in response to some application event:

```csharp
public sealed class ChatHub : Hub
{
    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("ReceiveMessage", "system", $"{Context.ConnectionId} joined");
    }

    public async Task LeaveRoom(string roomName) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
}
```

Group membership is per-connection, not per-user — a user with two open connections (two browser
tabs) that both joined a group receives the group's messages on both connections independently.
Group membership is also not persisted anywhere by default: it lives only in the in-memory (or
backplane-shared, see [scaling-out-backplane.md](scaling-out-backplane.md)) connection state for as
long as the connection stays open. A reconnect after a dropped connection starts with no group
memberships — your application code is responsible for re-joining the relevant groups once
`OnConnectedAsync` (or an equivalent client-triggered call) runs again.

## Connection lifecycle

Override `OnConnectedAsync` and `OnDisconnectedAsync` to react to a connection starting or ending:

```csharp
public sealed class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Group membership is cleaned up automatically on disconnect; add
        // any additional per-connection cleanup here (e.g. presence tracking).
        await base.OnDisconnectedAsync(exception);
    }
}
```

`OnDisconnectedAsync` receives the exception that caused the disconnect, if any — `null` indicates a
clean, client-initiated disconnect. SignalR removes a disconnected connection from every group it was
in automatically; you don't need to call `RemoveFromGroupAsync` yourself during `OnDisconnectedAsync`
purely for cleanup, only for application-level bookkeeping (updating a presence list, notifying
other group members someone left).

## Connection IDs are not stable identity

`Context.ConnectionId` identifies one specific connection and changes on every reconnect — it is not
a stable way to identify a user across reconnects or across multiple tabs/devices. For anything that
needs to survive a reconnect or reach a user regardless of which connection they're currently on,
use `Context.UserIdentifier` and `Clients.User(...)` instead (see
[hubs-and-messaging.md](hubs-and-messaging.md)), or persist your own mapping keyed by an application
user ID rather than the connection ID.
