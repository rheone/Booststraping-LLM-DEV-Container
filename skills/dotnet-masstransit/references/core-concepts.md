# Core Concepts

MassTransit sits between your application code and a message transport, giving you a small set of
interfaces for sending and publishing messages, and a pipeline that delivers them to consumers.

## IBus, IPublishEndpoint, ISendEndpoint

- **`IBus`** is the top-level handle to the configured bus. It implements both
  `IPublishEndpoint` and `ISendEndpoint`, so most code never needs to inject `IBus` directly —
  inject the narrower interface that matches what you're doing.
- **`IPublishEndpoint`** publishes a message to every consumer subscribed to that message type. Use
  it for events — facts that already happened, with zero or many interested subscribers:

  ```csharp
  public class OrderService(IPublishEndpoint publishEndpoint)
  {
      public Task SubmitAsync(Guid orderId, decimal total) =>
          publishEndpoint.Publish(new OrderSubmitted(orderId, total));
  }
  ```

- **`ISendEndpoint`** sends a message to one specific destination queue. Use it for commands — an
  instruction directed at exactly one receiver:

  ```csharp
  var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:process-payment"));
  await endpoint.Send(new ProcessPayment(orderId, total));
  ```

Inject `ISendEndpointProvider` rather than resolving `ISendEndpoint` once and holding onto it —
endpoint resolution is cheap and the provider handles connection/transport lifetime correctly.

## ConsumeContext<T>

Every consumer receives a `ConsumeContext<T>` wrapping the message, not the bare message type. It
carries the message (`context.Message`), correlation and conversation identifiers, headers, and the
ability to respond, publish, or send further messages as part of handling the current one:

```csharp
public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        await context.Publish(new OrderConfirmed(context.Message.OrderId));
    }
}
```

Publishing or sending through `context` (rather than through an injected `IPublishEndpoint`)
ensures the outgoing message enrolls in the same conversation and, where the transport supports it,
the same outbox/transaction as the message currently being consumed.

## How a message reaches a consumer

1. A publisher calls `Publish` (or a sender calls `Send` to a known queue address).
2. MassTransit serializes the message and, for `Publish`, routes it through the transport's
   publish/subscribe mechanism (a topic, an exchange, or equivalent) to every queue whose receive
   endpoint declared a subscription for that message type.
3. A receive endpoint pulls the message off its queue and runs it through the consume pipeline —
   retry middleware, then your `IConsumer<T>.Consume` method.
4. On success, the message is acknowledged. On unhandled failure, retry middleware and fault
   handling decide what happens next (see
   [retry-and-error-handling.md](retry-and-error-handling.md)).

## Message contracts are interfaces or records, not classes with behavior

Define messages as plain data — an interface or an immutable record — never as a class carrying
behavior or a reference to a live object (a `DbContext`, an open connection). Everything on a
message contract must serialize cleanly and mean the same thing on the receiving side, which may be
a different process, a different machine, or a different version of your application entirely.
