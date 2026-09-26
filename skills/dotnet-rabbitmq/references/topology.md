# Exchange, queue, and binding declaration

Declaring topology is idempotent: declaring an exchange or queue that already exists with matching
arguments succeeds silently, which is why services typically declare their topology defensively on
every startup rather than assuming a separate provisioning step ran first.

## Declaring an exchange

```csharp
await channel.ExchangeDeclareAsync(
    exchange: "orders.events",
    type: ExchangeType.Topic,
    durable: true,
    autoDelete: false);
```

`ExchangeType` provides the standard AMQP exchange types:

- `Direct` — routes a message to queues bound with a routing key that exactly matches.
- `Fanout` — routes a message to every bound queue, ignoring the routing key entirely.
- `Topic` — routes by wildcard pattern match (`*` for one word, `#` for zero or more) against the
  routing key, the most flexible general-purpose choice for anything beyond exact/broadcast routing.
- `Headers` — routes by matching message header values instead of the routing key; rarely the
  right default over `Topic` unless the routing decision genuinely isn't expressible as a key.

## Declaring a queue

```csharp
QueueDeclareOk result = await channel.QueueDeclareAsync(
    queue: "orders.notifications",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: new Dictionary<string, object?>
    {
        ["x-dead-letter-exchange"] = "orders.dlx",
    });
```

- `durable: true` survives a broker restart — the default for anything a production consumer
  depends on existing after the broker comes back up.
- `exclusive: true` scopes the queue to the declaring connection and deletes it when that connection
  closes — appropriate for a temporary, per-connection reply queue, not a shared work queue.
- `autoDelete: true` deletes the queue once its last consumer disconnects — combine carefully with
  `durable`, since a durable, auto-delete queue still disappears the moment nobody is consuming it.
- `arguments` carries broker-specific queue arguments (dead-lettering, message TTL, queue type — see
  [references/dead-lettering-and-retry.md](dead-lettering-and-retry.md) for the dead-letter
  arguments specifically) as a plain dictionary of AMQP argument names to values.

Passing `queue: string.Empty` asks the broker to generate a unique name, returned on
`QueueDeclareOk.QueueName` — the standard pattern for a temporary, per-consumer queue (e.g. an
RPC-style reply queue).

## Binding a queue to an exchange

```csharp
await channel.QueueBindAsync(
    queue: "orders.notifications",
    exchange: "orders.events",
    routingKey: "order.created.#");
```

A queue with no binding to any exchange other than the unnamed default exchange only receives
messages published directly to it by queue name — publishing to a custom exchange with no matching
binding silently drops the message unless the publish is marked `mandatory` (see
[references/publishing-with-confirms.md](publishing-with-confirms.md)).

## The default (unnamed) exchange

Every RabbitMQ virtual host has a pre-declared direct exchange with the empty-string name, to which
every queue is implicitly bound under a routing key equal to its own queue name. Publishing with
`exchange: string.Empty` and `routingKey: "orders"` delivers directly to a queue named `"orders"`
without any explicit `ExchangeDeclareAsync`/`QueueBindAsync` call — the shape used in this skill's
quick-start example, and a reasonable default for simple point-to-point delivery to a single queue.
