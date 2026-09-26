# Dead-lettering and retry patterns

## Dead-letter exchange basics

A queue declared with the `x-dead-letter-exchange` argument routes a message to that exchange
instead of discarding it when the message is rejected without requeue
(`BasicNackAsync(..., requeue: false)` or `BasicRejectAsync(..., requeue: false)`), expires via
per-message or queue TTL, or is dropped for exceeding a configured queue length limit.

```csharp
await channel.QueueDeclareAsync(
    queue: "orders.notifications",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: new Dictionary<string, object?>
    {
        ["x-dead-letter-exchange"] = "orders.dlx",
        ["x-dead-letter-routing-key"] = "orders.notifications.dead",
    });

await channel.ExchangeDeclareAsync("orders.dlx", ExchangeType.Direct, durable: true);
await channel.QueueDeclareAsync("orders.notifications.dead", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync("orders.notifications.dead", "orders.dlx", "orders.notifications.dead");
```

Omitting `x-dead-letter-routing-key` preserves the message's original routing key when it's
republished to the dead-letter exchange — set it explicitly only when the dead-letter exchange needs
a different key than the original queue's binding used.

## TTL-based retry via a parking queue

RabbitMQ has no built-in "retry after N seconds" primitive; the common pattern composes dead-lettering
with a per-message or per-queue TTL to build one: reject a failed message into a retry queue with a
TTL, and let that queue's own dead-letter configuration route expired messages back to the original
work queue once the delay elapses.

```csharp
// Work queue: failed messages go to the retry queue.
await channel.QueueDeclareAsync("orders.work", durable: true, exclusive: false, autoDelete: false,
    arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "orders.retry-exchange" });

// Retry queue: holds messages for 30s, then dead-letters them back to the work exchange.
await channel.QueueDeclareAsync("orders.retry", durable: true, exclusive: false, autoDelete: false,
    arguments: new Dictionary<string, object?>
    {
        ["x-message-ttl"] = 30000,
        ["x-dead-letter-exchange"] = "orders.work-exchange",
        ["x-dead-letter-routing-key"] = "orders.work",
    });
```

Track a retry count on the message (a custom header incremented by the consumer before rejecting it)
and route to a final, non-retrying dead-letter queue once the count exceeds a configured maximum —
without an explicit cap, a message that always fails cycles between the work and retry queues
indefinitely.

```csharp
int attempt = args.BasicProperties.Headers?.TryGetValue("x-retry-count", out var v) == true
    ? Convert.ToInt32(v) + 1
    : 1;

if (attempt > maxAttempts)
{
    await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false); // to final DLQ
}
else
{
    var props = new BasicProperties
    {
        Headers = new Dictionary<string, object?> { ["x-retry-count"] = attempt },
    };
    await channel.BasicPublishAsync("orders.retry-exchange", "orders.retry", basicProperties: props, body: args.Body);
    await channel.BasicAckAsync(args.DeliveryTag, multiple: false); // remove from the work queue
}
```

## Quorum queue dead-lettering

Quorum queues dead-letter at-least-once: the broker republishes a dead-lettered message with
publisher confirms enabled internally, retrying periodically (roughly every few minutes, broker
version-dependent) if the target exchange doesn't exist yet or the message can't currently be
routed. This means a quorum-queue-backed retry pipeline can occasionally deliver a duplicate to the
dead-letter target — design the downstream consumer to tolerate a duplicate delivery (idempotent
processing, a deduplication key) rather than assuming dead-lettering is exactly-once.

## When not to build a custom retry pipeline

For a small number of retries with a short, fixed delay, an in-process retry (catch the exception,
delay, retry, all before acknowledging the original delivery) is simpler than a multi-queue TTL
pipeline and avoids the quorum-queue duplicate-delivery caveat above entirely. Reach for the
queue-based retry pattern once retries need to survive a consumer restart, need a delay too long to
hold a message unacknowledged for, or need to be distributed across multiple consumer instances
rather than retried by whichever instance happened to receive the message first.
