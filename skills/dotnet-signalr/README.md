# ASP.NET Core SignalR

Task-organized guidance on ASP.NET Core SignalR — the routing table (by task, not SignalR version)
is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per version

| File | Covers |
| --- | --- |
| `hubs-and-messaging.md` | Hub, Hub\<T> strongly-typed hubs, Clients.All/Caller/Group/User/Others |
| `groups-and-connections.md` | Groups.AddToGroupAsync/RemoveFromGroupAsync, OnConnectedAsync/OnDisconnectedAsync, connection IDs |
| `authentication-and-authorization.md` | [Authorize] on a hub/hub method, hub authentication via the access token query string, policy-based hub authorization |
| `dotnet-client.md` | HubConnectionBuilder, WithAutomaticReconnect, connection lifecycle events |
| `streaming.md` | IAsyncEnumerable/ChannelReader server-to-client streaming, client-to-server streaming parameters |
| `scaling-out-backplane.md` | Redis backplane for multi-instance deployments |
| `testing.md` | Testing hub logic |

## Scope

ASP.NET Core SignalR (the `Microsoft.AspNetCore.SignalR` server APIs and
`Microsoft.AspNetCore.SignalR.Client` .NET client). Out of scope: transport-level protocol internals
beyond SignalR's own configuration surface, and Azure SignalR Service as a managed offering — this
skill covers self-hosting SignalR with a Redis backplane for scale-out.

Each reference file notes a SignalR version fact inline where relevant; version is not the
file-splitting axis for this skill (see SKILL.md for why).

## Verified facts (as of 2026-09-26)

- **Current latest release: Microsoft.AspNetCore.SignalR.Client 10.0.12** (part of the ASP.NET Core
  10.0 shared framework release train), MIT-licensed. Source: the NuGet Gallery package page
  (nuget.org/packages/microsoft.aspnetcore.signalr.client).

These facts were verified via live web search against nuget.org at the time this skill was written;
re-verify before relying on the exact version number.
