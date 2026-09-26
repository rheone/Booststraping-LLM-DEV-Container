# The .NET Client

## Building a connection

`HubConnectionBuilder` configures and creates a `HubConnection` — the client-side counterpart to a
server hub:

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://example.com/hubs/chat", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(currentToken);
    })
    .WithAutomaticReconnect()
    .Build();

connection.On<string, string>("ReceiveMessage", (user, message) =>
{
    Console.WriteLine($"{user}: {message}");
});

await connection.StartAsync();
await connection.InvokeAsync("SendMessage", "Alice", "Hello");
```

`connection.On<T1, T2, ...>(methodName, handler)` registers a handler for a server-pushed method call
— its generic parameters and the argument types the server actually sends must match, since this
side isn't checked against a shared interface the way `Hub<T>` checks the server side; a mismatch
deserializes incorrectly or throws at the point a message arrives, not at registration time.
`InvokeAsync` calls a hub method and awaits its completion (including its return value, via the
generic `InvokeAsync<TResult>` overload); `SendAsync` fires a hub method call without waiting for
completion, when you don't need to know when the server-side call finishes.

## Automatic reconnect

`WithAutomaticReconnect()` (with no arguments) retries a dropped connection using a built-in backoff
sequence; `WithAutomaticReconnect(TimeSpan[] retryDelays)` lets you supply your own sequence of
delays between attempts, with the array's length setting how many attempts are made before giving up
entirely:

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)])
    .Build();
```

Without `WithAutomaticReconnect()` at all, a dropped connection transitions straight to
`Disconnected` and stays there — the client does not retry on its own; you'd need to call
`StartAsync()` again yourself. `HubConnection` exposes `Reconnecting`, `Reconnected`, and `Closed`
events to react to each phase:

```csharp
connection.Reconnecting += error =>
{
    // Connection lost, an automatic reconnect attempt is starting.
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    // Reconnected — connectionId is a new value; re-join any groups the
    // server-side OnConnectedAsync doesn't already restore for you.
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    // All reconnect attempts exhausted (or automatic reconnect wasn't
    // configured) — the connection is not coming back on its own.
    return Task.CompletedTask;
};
```

`Reconnected` hands you a new connection ID — any server-side state keyed by the previous connection
ID (group membership not restored by `OnConnectedAsync`, in particular) needs to be re-established
from the client or server side after this event, since the underlying transport connection is not
resumed, it's replaced.

## Disposal

`HubConnection` implements `IAsyncDisposable`; dispose it (`await connection.DisposeAsync()`) when
the client no longer needs the connection, to stop the underlying transport and release its
resources — letting it go out of scope without disposing leaves the connection (and any reconnect
loop) running.
