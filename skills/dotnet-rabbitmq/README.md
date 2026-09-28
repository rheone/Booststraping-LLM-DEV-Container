# RabbitMQ.Client

RabbitMQ.Client is the official .NET client for talking to a RabbitMQ broker directly: opening
connections and channels, declaring exchanges and queues, publishing with delivery confirmation,
and consuming with explicit acknowledgment. This skill covers the fully async `IConnection`/
`IChannel` API surface, dead-lettering and retry patterns, and connection resiliency.

## When to reach for it

- You're opening your first connection and channel and want the disposal order right.
- You need to declare the exchange/queue/binding topology a producer or consumer depends on.
- You're publishing a message and need to know it actually reached the broker, not just left the client.
- You're deciding between manual and automatic acknowledgment while consuming.
- A message keeps failing and needs to retry or land in a dead-letter queue instead of disappearing.
- The connection needs to survive a network blip or broker restart without manual reconnect code.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches connecting to,
publishing to, or consuming from RabbitMQ through the `RabbitMQ.Client` package. You can also
invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| `ConnectionFactory`, `CreateConnectionAsync`, `CreateChannelAsync`, disposal order | [references/connection-and-channel-lifecycle.md](references/connection-and-channel-lifecycle.md) |
| `ExchangeDeclareAsync`, `QueueDeclareAsync`, `QueueBindAsync`, exchange types | [references/topology.md](references/topology.md) |
| `BasicPublishAsync`, `PublisherConfirmationsEnabled`, mandatory/returned messages | [references/publishing-with-confirms.md](references/publishing-with-confirms.md) |
| `AsyncEventingBasicConsumer`, `BasicConsumeAsync`, manual vs. automatic ack | [references/consuming.md](references/consuming.md) |
| Dead-letter exchanges, TTL-based retry queues, quorum queue dead-lettering | [references/dead-lettering-and-retry.md](references/dead-lettering-and-retry.md) |
| `AutomaticRecoveryEnabled`, `TopologyRecoveryEnabled`, recovery event handlers | [references/connection-resiliency.md](references/connection-resiliency.md) |
| Faking the channel interface vs. testing against a real broker in a container | [references/testing-with-rabbitmq.md](references/testing-with-rabbitmq.md) |

## Example prompts

- "Set up a durable queue and publish a message to it with confirms enabled."
- "My consumer isn't acking messages correctly: help me switch it to manual ack."
- "Add a dead-letter exchange so failed messages retry three times before landing in a parking-lot queue."
