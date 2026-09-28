# ASP.NET Core SignalR

SignalR pushes real-time messages between an ASP.NET Core server and connected clients over
WebSockets (falling back to Server-Sent Events or long polling), in either direction. This skill
covers writing hubs, managing groups and connections, securing hubs, connecting from a .NET
client, streaming, and scaling out across multiple server instances.

## When to reach for it

- You're writing a `Hub` or `Hub<T>` and deciding how to send a message to all clients, the caller, a group, or a specific user.
- Clients need to join or leave a group, or you need to react to a connection or disconnection.
- A hub needs to be restricted to authenticated or authorized users.
- You're connecting from a .NET client and want automatic reconnect handled cleanly.
- You're streaming data from server to client (or client to server) instead of sending one message at a time.
- More than one server instance is running behind a load balancer and clients need messages regardless of which instance they're connected to.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches writing or reviewing
a SignalR hub, the .NET client, streaming, hub authorization, or backplane scale-out. You can also
invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| `Hub`, `Hub<T>` strongly-typed hubs, `Clients.All`/`Caller`/`Group`/`User`/`Others` | [references/hubs-and-messaging.md](references/hubs-and-messaging.md) |
| Adding/removing connections from groups, connect/disconnect lifecycle | [references/groups-and-connections.md](references/groups-and-connections.md) |
| `[Authorize]` on a hub or hub method, access-token authentication, policy-based authorization | [references/authentication-and-authorization.md](references/authentication-and-authorization.md) |
| `HubConnectionBuilder`, `WithAutomaticReconnect`, connection lifecycle events | [references/dotnet-client.md](references/dotnet-client.md) |
| Server-to-client and client-to-server streaming | [references/streaming.md](references/streaming.md) |
| A Redis backplane for multi-instance deployments | [references/scaling-out-backplane.md](references/scaling-out-backplane.md) |
| Testing hub logic | [references/testing.md](references/testing.md) |

## Example prompts

- "Write a chat hub that broadcasts a message to everyone in a group."
- "Secure this hub so only authenticated users can connect."
- "Set up a Redis backplane so SignalR works across two load-balanced server instances."
