# RabbitMQ.Client

Guidance on RabbitMQ.Client, the official third-party .NET client for RabbitMQ — the routing table
(by situation, not by RabbitMQ.Client/C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per RabbitMQ.Client/C# version

| File | Covers |
| --- | --- |
| `connection-and-channel-lifecycle.md` | ConnectionFactory, CreateConnectionAsync, CreateChannelAsync, disposal order |
| `topology.md` | ExchangeDeclareAsync, QueueDeclareAsync, QueueBindAsync, exchange types |
| `publishing-with-confirms.md` | BasicPublishAsync, PublisherConfirmationsEnabled, mandatory/returned messages |
| `consuming.md` | AsyncEventingBasicConsumer, BasicConsumeAsync, BasicAckAsync/BasicNackAsync, ack strategy |
| `dead-lettering-and-retry.md` | x-dead-letter-exchange/routing-key, TTL-based retry queues, quorum queue dead-lettering |
| `connection-resiliency.md` | AutomaticRecoveryEnabled, TopologyRecoveryEnabled, NetworkRecoveryInterval, recovery events |
| `testing-with-rabbitmq.md` | Faking the channel interface, a real broker in a container, testing consumer handlers |

## Scope

The `RabbitMQ.Client` package's async `IConnection`/`IChannel` API surface (7.x) for connecting,
declaring topology, publishing, and consuming over classic AMQP 0-9-1. Out of scope: broker-side
administration, AMQP 1.0 support, and message serialization format choices layered on top of the
raw message body.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why the 7.x API shape is the one documented
throughout). Current stable release as of this writing: 7.2.2.
