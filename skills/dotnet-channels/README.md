# Channels (System.Threading.Channels)

This skill covers `System.Threading.Channels`, the BCL's async-first, thread-safe producer-consumer
data structure: creating channels, writing and reading with backpressure, and shutting a pipeline
down cleanly.

## When to reach for it

- Building an async producer-consumer pipeline or a work queue between async tasks.
- Throttling a fast producer against a slow consumer with a bounded channel.
- Deciding what should happen when a bounded channel fills up: wait, drop oldest, drop newest, or
  drop the write.
- Debugging a channel that never completes, or a consumer that stops reading before all items are
  processed.
- Signaling completion correctly when more than one producer writes to the same channel.

## Using it

This skill fires automatically when your request involves an async producer-consumer pipeline or
`System.Threading.Channels` specifically. You can also invoke it directly with
`/dotnet-channels`.

## What it covers

| Topic | Reference |
| --- | --- |
| `Channel.CreateUnbounded`/`CreateBounded`, `ChannelWriter<T>`/`ChannelReader<T>` | [references/core-api.md](references/core-api.md) |
| Single/multiple producers and consumers, fan-out, multi-producer completion signaling | [references/producer-consumer-patterns.md](references/producer-consumer-patterns.md) |
| Bounded channels, `BoundedChannelFullMode`, sizing capacity | [references/backpressure-and-bounded-channels.md](references/backpressure-and-bounded-channels.md) |
| `Complete()`/`Completion`, propagating an exception, graceful shutdown | [references/completion-and-shutdown.md](references/completion-and-shutdown.md) |
| Factual API-design contrast with `BlockingCollection<T>` | [references/blockingcollection-contrast.md](references/blockingcollection-contrast.md) |
| Testing pipeline output, completion, and backpressure | [references/testing.md](references/testing.md) |

## Example prompts

- "Build a producer-consumer pipeline where one task reads files and another processes them."
- "My bounded channel deadlocks under load. How do I stop the producer from blocking forever?"
- "How do I make sure every producer finishes before the channel calls itself complete?"
