# Connection resiliency

## Automatic connection recovery

`ConnectionFactory.AutomaticRecoveryEnabled` is `true` by default in the 7.x client — the client
detects a dropped connection and transparently reconnects, without the application needing to
implement its own reconnect loop for the common case.

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    AutomaticRecoveryEnabled = true, // the 7.x default; set explicitly if a reader shouldn't assume it
    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
};
```

`NetworkRecoveryInterval` controls the delay between reconnection attempts after a connection drops
— tune it down for a service that needs to notice recovery quickly, or up to avoid hammering a
broker that's mid-restart with reconnect attempts.

## Topology recovery

`TopologyRecoveryEnabled` (also `true` by default) re-declares exchanges, queues, bindings, and
consumers that existed on the connection before it dropped, once the connection is automatically
recovered — so a consumer that was running before a network blip resumes consuming from the same
queue afterward without the application re-running its declaration/binding/consume code manually.

Topology recovery only restores what the client itself declared through this connection's channels;
it does not restore server-side state a separate process or tool created directly against the
broker. Declare topology through the same connection whose recovery you depend on, not through a
one-off administrative connection.

## Reacting to connection and channel events

```csharp
connection.ConnectionShutdownAsync += (sender, args) =>
{
    _logger.LogWarning("Connection shut down: {ReplyText}", args.ReplyText);
    return Task.CompletedTask;
};

connection.RecoverySucceededAsync += (sender, args) =>
{
    _logger.LogInformation("Connection recovered");
    return Task.CompletedTask;
};

connection.ConnectionRecoveryErrorAsync += (sender, args) =>
{
    _logger.LogError(args.Exception, "Automatic recovery failed");
    return Task.CompletedTask;
};
```

Subscribe to these for observability (metrics, alerting) even when automatic recovery handles the
mechanics — a service that silently reconnects with no logged signal makes a real outage
indistinguishable from normal operation in hindsight.

## What automatic recovery does not cover

- **In-flight, unconfirmed publishes at the moment of disconnection** are not automatically
  retried — the application still needs to track which publishes it has confirmation for (see
  [references/publishing-with-confirms.md](publishing-with-confirms.md)) and decide whether to
  republish anything left unconfirmed after reconnection.
- **Messages already delivered to a consumer but not yet acknowledged** when the connection drops
  are returned to the queue by the broker for redelivery once the consumer (or a different one)
  reconnects — the original consumer does not resume processing the exact same in-flight message
  instance across the reconnect.
- **A broker that never comes back** (permanently down, wrong credentials after a rotation) — the
  reconnection loop keeps retrying on the configured interval indefinitely; pair automatic recovery
  with your own health check/circuit-breaking at the application level if an unrecoverable failure
  needs to surface as a service-level outage rather than silent, endless retrying.
