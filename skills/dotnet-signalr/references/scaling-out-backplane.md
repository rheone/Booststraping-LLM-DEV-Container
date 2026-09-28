# Scaling Out via a Backplane

## The problem a backplane solves

A single ASP.NET Core process holds every connection's state (group membership, which connections
are open) in memory. The moment you run more than one server instance behind a load balancer, a
message sent from a hub method running on instance A needs to reach clients connected to instance B
— which instance A's in-memory state knows nothing about. Without a backplane, `Clients.All`,
`Clients.Group`, and `Clients.User` calls only reach connections on the same instance that issued the
call, silently dropping the rest.

## Redis backplane

The `Microsoft.AspNetCore.SignalR.StackExchangeRedis` package wires SignalR's `Clients.*` calls
through a Redis pub/sub channel so every server instance publishes outgoing messages to Redis and
subscribes to receive messages published by every other instance:

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")!, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("MyApp");
    });
```

Setting a distinct `ChannelPrefix` per application matters when more than one SignalR application
shares the same Redis instance — without it, messages from one application's hubs could be received
by another application's server instances subscribed to the same unprefixed channel names.

## What the backplane does and doesn't fix

The Redis backplane makes `Clients.*` calls reach every connection across every server instance. It
does **not** make hub method invocations themselves load-balanced beyond what the load balancer
already does at the connection level, and it does not turn group membership into durable, persisted
state — group membership still lives per-connection, just now synchronized across instances through
Redis pub/sub for the duration each connection stays open (see
[groups-and-connections.md](groups-and-connections.md) for why membership doesn't survive a
reconnect regardless of backplane).

## Sticky sessions still matter for non-WebSocket transports

For transports that require multiple HTTP requests per logical connection (Server-Sent Events, long
polling), the load balancer must route every request for a given connection back to the same server
instance — the backplane synchronizes messages *between* instances, but a single logical client
connection's own request sequence still needs to land on the instance that's actually holding that
connection open. Configure session affinity (sticky sessions) at the load balancer for these
transports; a pure WebSocket-only deployment doesn't have this requirement since the whole connection
is one long-lived socket to begin with.
