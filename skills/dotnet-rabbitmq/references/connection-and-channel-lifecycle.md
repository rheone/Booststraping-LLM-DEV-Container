# Connection and channel lifecycle

## `ConnectionFactory` and `IConnection`

`ConnectionFactory` holds connection parameters (host, port, credentials, virtual host) and creates
`IConnection` instances. A connection represents one TCP socket to the broker and is comparatively
expensive to establish — create one per logical application, not one per operation.

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "app",
    Password = "app-password",
    VirtualHost = "/orders",
};

await using IConnection connection = await factory.CreateConnectionAsync();
```

`CreateConnectionAsync` also accepts an `IEnumerable<string>` of hostnames (or
`IEnumerable<AmqpTcpEndpoint>`) for connecting against a cluster, trying each endpoint in turn until
one succeeds.

## `IChannel`

A channel is a lightweight, multiplexed virtual connection over the single underlying TCP
connection — the unit publishing, consuming, and topology declaration actually happen on. Open one
channel per logical unit of work or per thread of execution that publishes/consumes independently;
sharing a single channel across concurrent operations from multiple threads without external
synchronization is not supported by the client.

```csharp
await using IChannel channel = await connection.CreateChannelAsync();
```

`IChannel` is the 7.x name for what earlier major versions of this client called `IModel` — every
member on it that talks to the broker (`QueueDeclareAsync`, `BasicPublishAsync`,
`BasicConsumeAsync`, and so on) is `Task`-/`ValueTask`-returning, so channel operations are awaited
rather than called synchronously.

## Enabling publisher confirms at channel-creation time

Publisher confirmations are configured when the channel is created via `CreateChannelOptions`,
not through a separate `ConfirmSelect()`-style call made after the fact:

```csharp
var channelOptions = new CreateChannelOptions(
    publisherConfirmationsEnabled: true,
    publisherConfirmationTrackingEnabled: true);

await using IChannel channel = await connection.CreateChannelAsync(channelOptions);
```

See [references/publishing-with-confirms.md](publishing-with-confirms.md) for how a channel created
this way then confirms individual publishes.

## Disposal order

Dispose channels before the connection that created them, and prefer `await using` for both so
disposal happens deterministically even when an exception unwinds the surrounding method:

```csharp
await using IConnection connection = await factory.CreateConnectionAsync();
await using IChannel channel = await connection.CreateChannelAsync();

// ... publish/consume ...

// channel disposes first (end of its using scope), then connection.
```

Closing a connection implicitly closes every channel opened on it, but relying on that instead of
explicit per-channel disposal makes cleanup order (and therefore any exception surfaced during
cleanup) harder to reason about — dispose channels explicitly rather than only the connection.

## One connection, many channels, per application

The common shape for a long-running service: one `IConnection` for the process's lifetime (recreated
only on unrecoverable failure — see
[references/connection-resiliency.md](connection-resiliency.md)), with channels opened and disposed
around each unit of work or held for the lifetime of a single consumer.
