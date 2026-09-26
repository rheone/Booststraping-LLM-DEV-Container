# Publishing with confirms

## Basic publish

```csharp
await channel.BasicPublishAsync(
    exchange: "orders.events",
    routingKey: "order.created.123",
    body: System.Text.Encoding.UTF8.GetBytes(payloadJson));
```

`BasicPublishAsync` on its own, without publisher confirmations enabled on the channel, does not
wait for the broker to acknowledge receipt — it returns once the message has been written to the
client's outbound buffer. A publish can still be lost between the client and the broker (a dropped
connection, a broker crash) with no signal back to the caller unless confirms are enabled.

## Enabling publisher confirms

Confirms are turned on when the channel is created, via `CreateChannelOptions`:

```csharp
var options = new CreateChannelOptions(
    publisherConfirmationsEnabled: true,
    publisherConfirmationTrackingEnabled: true);

await using IChannel channel = await connection.CreateChannelAsync(options);
```

With confirms enabled, awaiting `BasicPublishAsync` waits for the broker's acknowledgment of that
specific message before the `Task` completes — a successful `await` means the broker has accepted
responsibility for the message (persisted it, for a durable queue/message combination), not merely
that the client sent it.

```csharp
await channel.BasicPublishAsync(
    exchange: "orders.events",
    routingKey: "order.created.123",
    mandatory: false,
    basicProperties: new BasicProperties { Persistent = true },
    body: System.Text.Encoding.UTF8.GetBytes(payloadJson));
```

## Tracking confirms across many publishes

For high-throughput publishing, awaiting each `BasicPublishAsync` individually serializes publishes
behind each confirmation round-trip. Publish a batch without awaiting each call individually, then
await completion, or subscribe to the channel's `BasicAcksAsync`/`BasicNacksAsync` events to track
outstanding confirmations asynchronously alongside a per-message correlation identifier (e.g. a
sequence number captured before publishing) rather than blocking on each one in turn.

```csharp
channel.BasicAcksAsync += (sender, args) =>
{
    // args.DeliveryTag / args.Multiple identify which publish(es) were confirmed.
    return Task.CompletedTask;
};

channel.BasicNacksAsync += (sender, args) =>
{
    // The broker could not persist/route the message; treat it as failed and retry or alert.
    return Task.CompletedTask;
};
```

## `mandatory` and returned messages

`mandatory: true` asks the broker to return the message to the publisher (via the channel's
`BasicReturnAsync` event) instead of silently dropping it when no queue is bound to receive it —
publisher confirms alone only tell you the broker accepted the message, not that it was
successfully routed anywhere. Combine `mandatory: true` with a `BasicReturnAsync` handler for any
publish whose routing depends on bindings that could plausibly be missing or misconfigured.

```csharp
channel.BasicReturnAsync += (sender, args) =>
{
    _logger.LogWarning("Message returned, unroutable: {ReplyText}", args.ReplyText);
    return Task.CompletedTask;
};
```

## Message persistence vs. queue durability

A message surviving a broker restart requires both a durable queue (declared with `durable: true`)
and a persistent message (`BasicProperties.Persistent = true`, or the equivalent
`DeliveryMode.Persistent`) — a persistent message published to a non-durable queue, or a
non-persistent message published to a durable queue, does not survive a restart either way. Set
both explicitly for anything that must not be lost.
