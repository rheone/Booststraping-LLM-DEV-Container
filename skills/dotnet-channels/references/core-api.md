# Core API

A channel splits into two halves you hand out separately: a `ChannelWriter<T>` for producers and a
`ChannelReader<T>` for consumers. You create both together from one of two factory methods on the
static `Channel` class, then distribute `channel.Writer`/`channel.Reader` to whichever code needs
each side — never the whole `Channel<T>` object to a component that should only write or only read.

## Creating a channel

```csharp
// Unbounded: no capacity limit. Writes always succeed immediately (memory permitting).
Channel<Order> unbounded = Channel.CreateUnbounded<Order>();

// Bounded: a fixed capacity. Writes beyond capacity behave per BoundedChannelFullMode
// (see references/backpressure-and-bounded-channels.md).
Channel<Order> bounded = Channel.CreateBounded<Order>(capacity: 100);
```

Both factories accept an options object (`UnboundedChannelOptions`/`BoundedChannelOptions`) with
`SingleReader`/`SingleWriter` hints and `AllowSynchronousContinuations`:

```csharp
var options = new UnboundedChannelOptions
{
    SingleReader = true,  // set true only when you guarantee exactly one consumer
    SingleWriter = false, // set true only when you guarantee exactly one producer
};
Channel<Order> channel = Channel.CreateUnbounded<Order>(options);
```

Set `SingleReader`/`SingleWriter` to `true` only when the corresponding side is genuinely
single-threaded in your usage — the channel uses this hint to pick faster, less-synchronized internal
data structures. Setting it to `true` while actually using multiple producers or consumers is
undefined-behavior territory for ordering and can corrupt internal state; when unsure, leave it
`false`.

## `ChannelWriter<T>`

- `WriteAsync(T item, CancellationToken)` — asynchronously writes one item, completing once the item
  is accepted (immediately for unbounded, or once space is free / per `BoundedChannelFullMode` for
  bounded).
- `TryWrite(T item)` — synchronous, non-blocking attempt; returns `false` immediately if the channel
  can't currently accept the item (a full bounded channel in `Wait` mode) rather than waiting.
- `WaitToWriteAsync(CancellationToken)` — returns a `ValueTask<bool>` that completes when the channel
  is (probably) ready to accept a write, or `false` once the channel is completed. Pair with
  `TryWrite` in a loop for advanced scenarios; most code just calls `WriteAsync` directly.
- `Complete(Exception? error = null)` — signals no more items are coming (see
  [completion-and-shutdown.md](completion-and-shutdown.md)).

## `ChannelReader<T>`

- `ReadAsync(CancellationToken)` — asynchronously reads one item, completing once an item is
  available or throwing `ChannelClosedException` once the channel is completed and drained.
- `TryRead(out T item)` — synchronous, non-blocking attempt; returns `false` immediately if nothing is
  currently available.
- `WaitToReadAsync(CancellationToken)` — returns a `ValueTask<bool>` that completes `true` when an
  item is available to read, or `false` once the channel is completed and fully drained.
- `ReadAllAsync(CancellationToken)` — returns an `IAsyncEnumerable<T>` for consumption with
  `await foreach`; this is the idiomatic way to drain a channel and ends automatically once the
  writer completes and the buffer empties.
- `Completion` — a `Task` that completes once the channel is done (see
  [completion-and-shutdown.md](completion-and-shutdown.md)).

## Choosing a read/write style

Prefer `ReadAllAsync()` with `await foreach` for straightforward drain loops — it handles the
wait/read/loop/exit sequence for you. Reach for the `WaitToReadAsync`/`TryRead` pair only when you
need to read without necessarily blocking (e.g. batching everything currently available before
yielding), since `TryRead` never awaits.
