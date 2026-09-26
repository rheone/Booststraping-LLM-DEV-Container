# Consuming

Every consumer in the 7.x API is async — there is no separate synchronous consumer type, and no
`DispatchConsumersAsync` flag to opt into async dispatch, since it is the only dispatch mode.

## `AsyncEventingBasicConsumer`

```csharp
var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (sender, args) =>
{
    string body = System.Text.Encoding.UTF8.GetString(args.Body.ToArray());

    try
    {
        await ProcessAsync(body);
        await channel.BasicAckAsync(args.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Processing failed for delivery {Tag}", args.DeliveryTag);
        await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
    }
};

await channel.BasicConsumeAsync(
    queue: "orders.notifications",
    autoAck: false,
    consumer: consumer);
```

`args.Body` is a `ReadOnlyMemory<byte>` valid only for the duration of the `ReceivedAsync` handler
unless copied out (via `.ToArray()` or similar) — holding onto the original `ReadOnlyMemory<byte>`
past the handler's synchronous return (before an `await` inside it, in particular) risks reading a
buffer the client has since reused.

## Manual vs. automatic acknowledgment

- **`autoAck: false` (manual ack)** — the default and near-universal choice for anything where
  losing a message on a crash matters. The broker keeps the message unacknowledged (and undelivered
  to any other consumer) until `BasicAckAsync` is called, so a consumer that crashes mid-processing
  leaves the message for redelivery rather than silently dropping it.
- **`autoAck: true`** — the broker considers a message acknowledged the instant it's delivered,
  before the consumer has done anything with it. A consumer crash between delivery and completed
  processing loses that message permanently. Reserve this for genuinely disposable messages where
  redelivery-on-crash would cost more (e.g. duplicate side effects) than occasional loss.

## `BasicAckAsync` vs. `BasicNackAsync`

- `BasicAckAsync(deliveryTag, multiple)` — confirms successful processing; the broker discards the
  message. `multiple: true` acknowledges every unacknowledged message up to and including this
  delivery tag on the channel, useful for batch processing; `multiple: false` acknowledges only this
  one.
- `BasicNackAsync(deliveryTag, multiple, requeue)` — signals processing failure.
  `requeue: true` returns the message to the front of the queue for immediate redelivery (risking a
  tight retry loop against a consistently failing message); `requeue: false` drops the message, or
  routes it to a configured dead-letter exchange if the queue has one (see
  [references/dead-lettering-and-retry.md](dead-lettering-and-retry.md)) — the safer default for a
  failure that isn't a transient blip.

## Prefetch count

`await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: N, global: false)` caps how many
unacknowledged messages the broker delivers to a consumer at once, before it must ack/nack at least
one to receive more. Set this explicitly (a small number, often in the single or low double digits)
for any manual-ack consumer — leaving it at the unlimited default lets the broker push its entire
backlog to one consumer at once, which both defeats load balancing across multiple consumers on the
same queue and risks unbounded client-side memory growth if processing falls behind delivery.

```csharp
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false);
```

## Stopping a consumer cleanly

`await channel.BasicCancelAsync(consumerTag)` stops delivery to a specific consumer without closing
the channel — call it (or dispose the channel/connection) as part of a graceful shutdown so
in-flight messages get a chance to finish processing rather than being abandoned mid-handler.
