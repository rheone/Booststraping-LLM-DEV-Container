# Testing

MassTransit ships an in-memory test harness so you can assert publish, consume, and fault behavior
without a real broker and without sleeping to wait for asynchronous delivery.

## Setting up the test harness

```csharp
[Fact]
public async Task Consumer_HandlesOrderSubmitted()
{
    await using var provider = new ServiceCollection()
        .AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderSubmittedConsumer>();
        })
        .BuildServiceProvider(true);

    var harness = provider.GetRequiredService<ITestHarness>();
    await harness.Start();

    await harness.Bus.Publish(new OrderSubmitted(Guid.NewGuid(), 42m));

    Assert.True(await harness.Consumed.Any<OrderSubmitted>());
}
```

`AddMassTransitTestHarness` registers the in-memory transport plus the harness itself, and starting
the harness starts the bus. `harness.Consumed.Any<T>()` and `harness.Published.Any<T>()` poll
(without a fixed sleep) for a matching message within the harness's default timeout, which is
enough for the test to be deterministic without racing real bus delivery.

## Asserting a specific consumer received a message

```csharp
var consumerHarness = harness.GetConsumerHarness<OrderSubmittedConsumer>();

await harness.Bus.Publish(new OrderSubmitted(Guid.NewGuid(), 42m));

Assert.True(await consumerHarness.Consumed.Any<OrderSubmitted>());
```

Use the consumer-specific harness when more than one consumer could plausibly react to the same
message type and the test needs to confirm *this* consumer ran, not merely that the message was
consumed somewhere.

## Asserting a fault was published

```csharp
await harness.Bus.Publish(new OrderSubmitted(Guid.Empty, -1m)); // triggers consumer to throw

Assert.True(await harness.Published.Any<Fault<OrderSubmitted>>());
```

Configure the consumer under test with the same retry policy it would use in production (or an
explicit `Immediate(0)`/no-retry policy) when the test's intent is to reach the fault path quickly —
otherwise the harness has to wait out the full retry policy before the fault publishes.

## Testing a request client

```csharp
var client = harness.GetRequestClient<CheckInventory>();
var response = await client.GetResponse<InventoryChecked>(new CheckInventory("SKU-1", 5));

Assert.True(response.Message.Available);
```

## Testing a saga

```csharp
await using var provider = new ServiceCollection()
    .AddMassTransitTestHarness(x =>
    {
        x.AddSagaStateMachine<OrderStateMachine, OrderState>().InMemoryRepository();
    })
    .BuildServiceProvider(true);

var harness = provider.GetRequiredService<ITestHarness>();
await harness.Start();

var orderId = Guid.NewGuid();
await harness.Bus.Publish(new OrderSubmitted(orderId, 42m));

var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
Assert.True(await sagaHarness.Created.Any(x => x.CorrelationId == orderId));
```

Assert on the saga instance's persisted state (`sagaHarness.Created`, or querying the repository
directly) rather than on internal implementation details of the state machine — a saga test should
verify the process reaches the right state given a sequence of events, not that a specific internal
method ran.

## What still deserves a real integration test

Reserve a real-broker integration test for verifying transport-specific behavior the in-memory
harness doesn't exercise — actual message durability across a broker restart, broker-specific retry
or dead-lettering semantics, or throughput under realistic concurrency. The in-memory harness is the
right tool for the large majority of consumer, publisher, and saga logic tests; keep broker-backed
tests few and targeted at what only a real broker can prove.
