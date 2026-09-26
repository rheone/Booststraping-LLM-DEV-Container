# Notifications and Publishing

`Publish` fans an `INotification` out to every registered `INotificationHandler<TNotification>` —
zero, one, or many. Unlike `Send`, `Publish` returns nothing (`Task`, not `Task<TResponse>`): the
publisher gets no result back from any handler, by design — notifications are a broadcast, not a
question.

```csharp
public sealed record OrderShipped(Guid OrderId, DateTimeOffset ShippedAtUtc) : INotification;

await publisher.Publish(new OrderShipped(orderId, DateTimeOffset.UtcNow), cancellationToken);
```

## The publish strategy: how handlers actually get invoked

MediatR delegates the actual fan-out mechanics to an `INotificationPublisher`. Two implementations
ship with the library:

### `ForeachAwaitPublisher` — the default

Loops over the registered handlers and `await`s each one in turn, one at a time, in registration
order. This means:

- Execution is **sequential** — handler 2 doesn't start until handler 1 has finished.
- If a handler throws, **the loop stops** — later handlers in the list do not run, and the
  exception propagates out of `Publish`.
- Behavior is predictable and easy to reason about/debug, at the cost of total latency being the
  sum of every handler's latency.

This is the default because it's the safer, more conservative choice: nothing runs concurrently
unless you opt in, and a failing handler doesn't leave you guessing about which of the others
ran versus didn't.

### `TaskWhenAllPublisher` — built-in parallel option

Starts every handler's task without awaiting each individually, then `Task.WhenAll`s the whole
batch. Consequences:

- Handlers run **concurrently** with each other.
- **All handlers run regardless of whether one of them throws** — a failing handler doesn't
  prevent the others from running (though the aggregate exception still surfaces after all tasks
  complete).
- It does *not* use `Task.Run` — handlers still run on whatever synchronization context/thread
  pool the async machinery naturally uses; this isn't "fire off background threads," it's
  "don't serialize the awaits."
- Total latency is closer to the slowest single handler rather than the sum of all of them, at
  the cost of losing the "stop on first failure" and strict-ordering guarantees.

### Choosing and registering a publisher

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher); // respects configured ServiceLifetime
    // — or —
    cfg.NotificationPublisher = new TaskWhenAllPublisher(); // registered as a singleton instance
});
```

Passing a `NotificationPublisherType` registers the publisher through DI (so it respects whatever
`Lifetime` is configured); passing a `NotificationPublisher` instance directly registers that
exact singleton instance instead. A fully custom `INotificationPublisher` can also be implemented
and supplied the same way (e.g. to log per-handler exceptions individually rather than losing
them in an aggregate, or to bound concurrency with a semaphore) — the interface is a single method
(`Publish(IEnumerable<NotificationHandlerExecutor>, TNotification, CancellationToken)`) that owns
the entire fan-out loop, so a custom implementation has full control over ordering, concurrency,
and failure handling.

## Version note

Custom `INotificationPublisher` strategies (and the built-in `TaskWhenAllPublisher` alternative to
the default sequential loop) were introduced in MediatR 12.0. Before that, publish behavior was
sequential-only and customized by overriding a `Mediator.PublishCore` method instead — that
override point no longer exists on current versions; use `INotificationPublisher` for any custom
fan-out behavior on the current API.

## Practical guidance

- Default (`ForeachAwaitPublisher`) is the right choice when notification handlers have
  side-effect ordering dependencies, or when "stop on first failure and know exactly which
  handlers ran" matters more than latency — e.g. a chain where handler 2 assumes handler 1's
  side effect already happened.
- `TaskWhenAllPublisher` (or a custom concurrent publisher) fits independent handlers with no
  ordering dependency between them — e.g. "send a confirmation email" and "update a search index"
  reacting to the same event, where neither needs to wait on the other and a failure in one
  shouldn't block the other from attempting to run.
- Whichever strategy you pick applies to **every** `Publish` call process-wide (it's a single
  registration, not something chosen per-call) — if some notifications genuinely need sequential
  semantics and others need parallel, that has to be handled inside a custom publisher's logic
  (e.g. based on a marker interface on the notification) rather than by switching publishers
  per-call.
