# Consumers

A consumer is where you handle a message. MassTransit discovers and wires up consumers through DI
registration — you rarely construct one yourself.

## Defining a message contract

```csharp
public record OrderSubmitted(Guid OrderId, decimal Total);
```

Keep contracts in a shared project (or package) referenced by both the publisher and every
consumer, so both sides compile against the same type. Favor records or interfaces with only
primitive/serializable members; avoid inheritance hierarchies for messages, since polymorphic
message contracts complicate the topology MassTransit builds for subscriptions.

## Implementing IConsumer<T>

```csharp
public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    private readonly IOrderRepository _orders;

    public OrderSubmittedConsumer(IOrderRepository orders) => _orders = orders;

    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        await _orders.MarkSubmittedAsync(context.Message.OrderId, context.CancellationToken);
    }
}
```

A consumer is resolved from DI per message — constructor-inject whatever dependencies it needs
exactly as you would for a controller or a handler. Throwing from `Consume` triggers retry/fault
handling rather than crashing the process; do not swallow an exception you want retried.

## Registering a consumer

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSubmittedConsumer>();
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});
```

`AddConsumer<T>` registers the consumer with DI and makes `ConfigureEndpoints` aware of it so a
receive endpoint gets created for it automatically. Consumers default to a fresh instance per
message (transient); do not register a consumer with a singleton lifetime that holds
message-specific state across invocations.

## One consumer, one message type

Keep a consumer narrowly scoped to a single message type per `IConsumer<T>` implementation. A class
implementing `IConsumer<A>, IConsumer<B>` is legal but couples two unrelated handling paths to one
constructor and one set of dependencies — prefer two separate consumer classes unless the two
message types are genuinely part of the same operation.

## Consuming multiple message types with a shared concern

When several message types need the same cross-cutting behavior (logging, a shared validation
step), express that as middleware on the receive endpoint or bus configuration rather than as
shared base-class logic duplicated across consumers — see
[transport-configuration.md](transport-configuration.md) for where endpoint-level configuration
like this goes.
