---
name: dotnet-rabbitmq
description: Guidance on RabbitMQ.Client, the official third-party .NET client for RabbitMQ (current stable release 7.2.2, fully async-first API surface as of the 7.x major version). Covers IConnection/IChannel lifecycle (IModel was renamed to IChannel and every operation became Task-returning in 7.x), exchange/queue/binding declaration, publishing with publisher confirms via CreateChannelOptions.PublisherConfirmationsEnabled, consuming with AsyncEventingBasicConsumer and manual vs. automatic acknowledgment, dead-letter exchange and TTL-based retry patterns, and connection resiliency via ConnectionFactory.AutomaticRecoveryEnabled/TopologyRecoveryEnabled. Use when writing, reviewing, or debugging code that connects to, publishes to, or consumes from RabbitMQ through the RabbitMQ.Client package's IConnection/IChannel API.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# RabbitMQ.Client

Guidance on RabbitMQ.Client, the official third-party .NET client library for RabbitMQ. Current
stable release as of this writing: **7.2.2**. The 7.x major version is a
full async-first rewrite of the client's API surface — `IModel` was renamed to `IChannel`, every
channel operation now returns a `Task`/`ValueTask`, and `DispatchConsumersAsync` no longer exists
because every consumer is async by default. This skill documents the 7.x API shape throughout;
code written against the pre-7.0 synchronous `IModel` API does not compile against it unchanged.
Organized by concern/topic — each reference file notes a version-introduced fact inline rather than
splitting files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| First time connecting, or reviewing basic connection/channel setup | `ConnectionFactory`, `CreateConnectionAsync`, `CreateChannelAsync`, disposal order | [references/connection-and-channel-lifecycle.md](references/connection-and-channel-lifecycle.md) |
| Declaring the topology a producer or consumer depends on | `ExchangeDeclareAsync`, `QueueDeclareAsync`, `QueueBindAsync`, exchange types | [references/topology.md](references/topology.md) |
| Publishing a message and knowing whether the broker actually received it | `BasicPublishAsync`, `CreateChannelOptions.PublisherConfirmationsEnabled`, mandatory/returned messages | [references/publishing-with-confirms.md](references/publishing-with-confirms.md) |
| Consuming messages and deciding when to acknowledge them | `AsyncEventingBasicConsumer`, `BasicConsumeAsync`, `BasicAckAsync`/`BasicNackAsync`, manual vs. automatic ack | [references/consuming.md](references/consuming.md) |
| A message keeps failing and needs to be retried or shelved instead of lost | `x-dead-letter-exchange`, `x-dead-letter-routing-key`, TTL-based retry queues, quorum queue dead-lettering | [references/dead-lettering-and-retry.md](references/dead-lettering-and-retry.md) |
| The connection to the broker needs to survive a network blip or broker restart | `AutomaticRecoveryEnabled`, `TopologyRecoveryEnabled`, `NetworkRecoveryInterval`, recovery event handlers | [references/connection-resiliency.md](references/connection-resiliency.md) |
| Unit/integration testing code that publishes or consumes | Faking the channel interface vs. a real broker in a container, asserting on published messages, testing consumer handlers in isolation | [references/testing-with-rabbitmq.md](references/testing-with-rabbitmq.md) |

## Quick start

```csharp
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };

await using IConnection connection = await factory.CreateConnectionAsync();
await using IChannel channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "orders", durable: true, exclusive: false, autoDelete: false);

await channel.BasicPublishAsync(
    exchange: string.Empty,
    routingKey: "orders",
    body: System.Text.Encoding.UTF8.GetBytes("order-123"));
```

`IConnection` and `IChannel` both implement `IAsyncDisposable` (and `IDisposable`) — use
`await using` so a channel or connection closes cleanly even when an exception unwinds the method.

## Out of scope

- Broker administration (management plugin/HTTP API, `rabbitmqctl`, cluster formation, policy
  configuration on the server side) — a server-operations concern distinct from client library
  usage.
- AMQP 1.0 support (a separate protocol and client surface in RabbitMQ) — this skill covers the
  classic AMQP 0-9-1 `IConnection`/`IChannel` API that `RabbitMQ.Client` exposes by default.
- Message serialization format choices (JSON, protobuf, etc.) beyond the raw `byte[]`/
  `ReadOnlyMemory<byte>` body the client itself moves — a concern layered on top of, not part of,
  the client library's own API.
