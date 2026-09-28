# Testing code that uses RabbitMQ.Client

## How to test it

- **Isolate message-handling logic from the channel API entirely for unit tests.** Extract the
  per-message processing into a method or class that takes the deserialized payload (and whatever
  context it needs, e.g. a delivery tag or retry count as plain values) rather than an
  `IChannel`/`BasicDeliverEventArgs` directly — then that logic is testable with plain inputs and
  assertions, with no broker or fake channel involved at all.

```csharp
public interface IOrderMessageHandler
{
    Task HandleAsync(OrderCreated message, CancellationToken cancellationToken);
}
```

```csharp
[Fact]
public async Task HandleAsync_marks_order_as_processed()
{
    var handler = new OrderMessageHandler(_repository);
    var message = new OrderCreated(orderId: 123);

    await handler.HandleAsync(message, CancellationToken.None);

    Assert.True(_repository.WasProcessed(123));
}
```

- **Use a real broker in a container for integration tests of the plumbing itself** — topology
  declaration, publish/consume wiring, ack/nack and dead-letter routing all depend on genuine broker
  behavior that a hand-written fake cannot faithfully reproduce (routing rules, TTL expiry timing,
  dead-letter republishing). Point the integration test's `ConnectionFactory` at a disposable
  RabbitMQ instance started for the test run and torn down afterward, rather than a shared or
  long-lived broker whose state could leak between test runs.
- **Never fake `IChannel` to test business logic.** A hand-rolled or mocked `IChannel` exercises
  none of the real client's serialization, delivery-tag bookkeeping, or broker interaction — a test
  built on it verifies that the test's own mock behaves as configured, not that the code works
  against a real broker.

## Common scenarios

### Testing that a consumer acknowledges only on success

Using a real broker (or a test harness driving the actual consumer against one), publish a message
that the handler will process successfully and one that will throw, and assert on each message's
final state: the success case's message gone from the queue, the failure case's message either
redelivered, dead-lettered, or held for retry per the consumer's configured `BasicNackAsync`
behavior (see [references/consuming.md](consuming.md) and
[references/dead-lettering-and-retry.md](dead-lettering-and-retry.md)).

### Testing publisher-confirm handling

Publish against a real broker with confirms enabled and assert the publish call completes (the
`await` returns) for a routable message, and — separately — that a `BasicReturnAsync` handler fires
for a `mandatory: true` publish to a routing key with no bound queue, rather than trying to simulate
broker-side routing decisions without a broker.

### Testing dead-letter/retry topology end-to-end

Declare the full work/retry/dead-letter queue chain against a real broker, publish a message,
force a `BasicNackAsync(requeue: false)`, and assert the message eventually arrives (with the
retry-count header incremented, for a retry-loop test) on the queue the chain is supposed to route
it to — this is the only reliable way to catch a misconfigured `x-dead-letter-routing-key` or a TTL
value that doesn't match the test's expected timing.
