---
name: dotnet-signalr
description: Guidance on ASP.NET Core SignalR (verified current release 10.0.12, MIT license) — the Hub class and Hub<T> for strongly-typed hubs, sending messages via Clients.All/Caller/Group/User, groups and connection management, authentication/authorization on hubs, the .NET client (HubConnectionBuilder, WithAutomaticReconnect), streaming with IAsyncEnumerable/ChannelReader hub methods, and scaling out across multiple server instances via a backplane (Redis). Use when writing or reviewing a SignalR hub, wiring up the .NET SignalR client, implementing a streaming hub method, securing a hub with authorization, or configuring a Redis backplane for horizontal scale-out.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# ASP.NET Core SignalR

Guidance on ASP.NET Core SignalR, the real-time messaging library for pushing server-to-client (and
client-to-server) messages over WebSockets/Server-Sent Events/long polling. Organized by task, not
by SignalR version — the core `Hub`/`HubConnection` API surface has been stable across recent
releases; each reference file notes a version fact inline where it matters.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Writing a `Hub` or `Hub<T>`, sending messages via `Clients.All`/`Caller`/`Group`/`User` | [references/hubs-and-messaging.md](references/hubs-and-messaging.md) |
| Adding connections to/from a group, or handling connect/disconnect lifecycle | [references/groups-and-connections.md](references/groups-and-connections.md) |
| Securing a hub or a hub method with authentication/authorization | [references/authentication-and-authorization.md](references/authentication-and-authorization.md) |
| Connecting from a .NET client, configuring automatic reconnect | [references/dotnet-client.md](references/dotnet-client.md) |
| Streaming data from server to client, or from client to server | [references/streaming.md](references/streaming.md) |
| Running more than one server instance behind a load balancer | [references/scaling-out-backplane.md](references/scaling-out-backplane.md) |
| Testing hub logic | [references/testing.md](references/testing.md) |

## Quick start

```csharp
public sealed class ChatHub : Hub<IChatClient>
{
    public async Task SendMessage(string user, string message) =>
        await Clients.All.ReceiveMessage(user, message);
}

public interface IChatClient
{
    Task ReceiveMessage(string user, string message);
}

// Program.cs
builder.Services.AddSignalR();
app.MapHub<ChatHub>("/hubs/chat");
```

The single most common miss: mapping a hub with `MapHub` but never enabling authorization on it when
the hub is meant to be restricted — an unauthenticated client can connect to any hub endpoint that
doesn't carry an `[Authorize]` attribute (or equivalent policy) of its own. See
[references/authentication-and-authorization.md](references/authentication-and-authorization.md).

## Out of scope

- Transport-level protocol details (WebSocket framing, Server-Sent Events, long-polling internals)
  beyond what you configure through SignalR's own options — this skill covers the `Hub`/
  `HubConnection` API, not the underlying transport negotiation implementation.
- Azure SignalR Service as a managed offering — this skill covers self-hosted ASP.NET Core SignalR
  and a Redis backplane for scale-out, not a specific cloud-hosted service's configuration surface.
