---
name: dotnet-channels
description: Guidance on System.Threading.Channels — Channel.CreateUnbounded/CreateBounded, ChannelWriter<T>/ChannelReader<T>, WriteAsync/TryWrite/ReadAsync/TryRead/ReadAllAsync, single- and multi-producer/consumer pipelines, backpressure via bounded channels and BoundedChannelFullMode (Wait/DropOldest/DropNewest/DropWrite), completing a channel with Complete()/Completion for graceful shutdown, and how a channel's API design contrasts with BlockingCollection<T>. Use when building an async producer-consumer pipeline, a work queue between async tasks, throttling a fast producer against a slow consumer, or debugging a channel that never completes or drops items unexpectedly.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Channels (System.Threading.Channels)

Guidance on `System.Threading.Channels` (current stable release: **10.0.12**, shipping alongside
**.NET 10**; ships in-box in the shared framework since .NET Core 3.0, and as a standalone package
targeting .NET Standard 2.0+ for earlier targets). Channels are an async-first, thread-safe
producer-consumer data structure: one or more producers write items on a `ChannelWriter<T>`, one or
more consumers read them on a `ChannelReader<T>`, and both ends compose naturally with `async`/`await`
and cancellation.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Learning `Channel.CreateUnbounded<T>`/`Channel.CreateBounded<T>`, `ChannelWriter<T>`, `ChannelReader<T>` | [references/core-api.md](references/core-api.md) |
| Wiring up a single producer/single consumer, multiple producers, or multiple consumers | [references/producer-consumer-patterns.md](references/producer-consumer-patterns.md) |
| Bounding a channel to apply backpressure, or choosing a `BoundedChannelFullMode` | [references/backpressure-and-bounded-channels.md](references/backpressure-and-bounded-channels.md) |
| Shutting a pipeline down cleanly — calling `Complete()`, awaiting `Completion`, propagating an exception through a channel | [references/completion-and-shutdown.md](references/completion-and-shutdown.md) |
| Deciding between a channel and `BlockingCollection<T>` for a given producer-consumer scenario | [references/blockingcollection-contrast.md](references/blockingcollection-contrast.md) |
| Testing code built around channels | [references/testing.md](references/testing.md) |

## Quick start

The most common shape — a bounded channel with one producer task and one consumer loop, backpressure
applied automatically once the buffer fills:

```csharp
Channel<int> channel = Channel.CreateBounded<int>(new BoundedChannelOptions(capacity: 100)
{
    FullMode = BoundedChannelFullMode.Wait, // producer awaits when the channel is full
    SingleReader = true,
    SingleWriter = true,
});

Task producer = Task.Run(async () =>
{
    for (int i = 0; i < 1000; i++)
    {
        await channel.Writer.WriteAsync(i);
    }

    channel.Writer.Complete(); // signals no more items; lets the consumer's loop end
});

Task consumer = Task.Run(async () =>
{
    await foreach (int item in channel.Reader.ReadAllAsync())
    {
        Process(item);
    }
});

await Task.WhenAll(producer, consumer);
```

`ReadAllAsync()` ends its enumeration automatically once the writer calls `Complete()` and the buffer
drains — no separate "are we done" flag is needed.

## Out of scope

- General `async`/`await` and `Task` fundamentals not specific to channels.
- `System.IO.Pipelines`, a separate, byte-buffer-oriented API for high-throughput stream parsing —
  a different problem domain (streaming bytes, not discrete items) with its own allocation and
  backpressure model.
