# Retry and Error Handling

MassTransit gives a failed consumer two layers of recovery before a message is considered
permanently undeliverable: in-process retry, and a separate error queue for messages that exhaust
retry.

## Configuring message retry

```csharp
x.UsingInMemory((context, cfg) =>
{
    cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    cfg.ConfigureEndpoints(context);
});
```

`UseMessageRetry` wraps the consume pipeline so a thrown exception re-invokes `Consume` according to
the configured policy before giving up. Common policies:

- **`Interval(retryLimit, interval)`** — a fixed delay between each of a fixed number of attempts.
- **`Intervals(TimeSpan[] delays)`** — an explicit sequence of delays, one per attempt.
- **`Exponential(retryLimit, minInterval, maxInterval, intervalDelta)`** — exponential backoff
  bounded by a minimum and maximum delay.
- **`Immediate(retryLimit)`** — retries back-to-back with no delay; use only for failures expected
  to be resolved by a near-instant retry (a transient lock contention), not for anything involving
  network or external-system latency.

Retry runs entirely within the same message delivery — the message is not requeued between
attempts, so retried attempts don't compete with other messages on the queue and don't reset the
message's position.

## Filtering which exceptions retry

```csharp
cfg.UseMessageRetry(r =>
{
    r.Interval(3, TimeSpan.FromSeconds(5));
    r.Handle<TimeoutException>();
    r.Ignore<ValidationException>();
});
```

Retry only exceptions that represent a transient condition likely to succeed on a later attempt.
Retrying a `ValidationException` or any other exception caused by the message's own content wastes
every retry attempt reproducing the same deterministic failure — `Ignore` such exceptions so they
fault immediately instead.

## What happens when retry is exhausted: faults

When every retry attempt fails, MassTransit publishes a `Fault<T>` message (wrapping the original
message and the exception detail) and moves the original message to that receive endpoint's error
queue — conventionally named `<queue-name>_error`. Consume `Fault<T>` like any other message to
react to permanent failures (alerting, compensating action):

```csharp
public class OrderSubmittedFaultConsumer : IConsumer<Fault<OrderSubmitted>>
{
    public Task Consume(ConsumeContext<Fault<OrderSubmitted>> context)
    {
        var originalMessage = context.Message.Message;
        var exceptions = context.Message.Exceptions;
        return Task.CompletedTask;
    }
}
```

## The error queue as the operational safety net

A message sitting in `<queue-name>_error` is not lost — it's held for inspection and, once the
underlying cause is fixed, can be moved back onto the original queue for reprocessing. Treat a
non-empty error queue as an operational signal worth alerting on, not a dead end; it is the record
of every message your system could not process even after retrying.

## Redelivery vs. retry

Retry (above) happens immediately, in-process, without ever leaving the receive endpoint.
*Redelivery* (`UseScheduledRedelivery`) instead requeues the message after a longer delay — useful
for a failure that needs more time to resolve than an in-process retry loop should reasonably wait
for (a downstream dependency that's down for minutes, not milliseconds). Configure redelivery
policies the same way as retry policies, at the bus or endpoint level, when a failure class calls
for a longer wait than blocking the consumer in-process would justify.
