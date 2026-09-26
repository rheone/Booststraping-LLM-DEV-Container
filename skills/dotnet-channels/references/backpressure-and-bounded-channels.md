# Backpressure and Bounded Channels

An unbounded channel never applies backpressure — a producer that outpaces its consumer just keeps
growing the internal buffer, which can exhaust memory under sustained load. A bounded channel caps
that buffer at a fixed `capacity` and gives you an explicit policy for what happens once it's full.

## Creating a bounded channel

```csharp
Channel<Event> channel = Channel.CreateBounded<Event>(new BoundedChannelOptions(capacity: 1_000)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = false,
    SingleWriter = false,
});
```

`Channel.CreateBounded<T>(int capacity)` (without an options object) defaults `FullMode` to
`BoundedChannelFullMode.Wait`.

## `BoundedChannelFullMode` — the four policies

| Value | Behavior when the channel is full |
| --- | --- |
| `Wait` (default) | `WriteAsync` asynchronously waits until space frees up (a consumer reads an item) before completing. This is the only mode that applies real backpressure — a fast producer is slowed down to match the consumer. |
| `DropOldest` | Removes and discards the oldest queued item to make room, then writes the new item. The write always completes synchronously; the channel silently loses data instead of applying backpressure. |
| `DropNewest` | Removes and discards the newest *queued* item (not the one currently being written) to make room, then writes the new item. Also always completes synchronously with silent data loss. |
| `DropWrite` | Drops the item currently being written and keeps the existing queue untouched. The write still reports as completed even though the item was discarded. |

Only `Wait` provides backpressure in the sense of "slow the producer down." The three drop modes
exist for scenarios where staying current matters more than completeness — a live telemetry feed
where the newest reading is what matters, not a backlog of every reading ever produced.

## Choosing a mode

- **`Wait`** — the default choice for work queues, job pipelines, and anything where every item must
  eventually be processed (an order, a file to persist, a message to deliver). Losing an item here is
  a correctness bug, so let the producer block instead.
- **`DropOldest`/`DropNewest`** — appropriate for a bounded buffer of "current state" updates (a
  sensor reading, a UI progress percentage) where only the latest value has to survive and older,
  superseded values are safe to discard.
- **`DropWrite`** — least common; useful when the *producer's* newest attempt is the one that's safe
  to lose (e.g. a best-effort metrics counter increment) and the existing queue should be left intact
  rather than evicting something already queued.

## Sizing capacity

There's no universal right capacity — it's a tradeoff between memory (a larger buffer smooths out
short bursts without blocking the producer) and staleness/latency (a larger buffer under `Wait` means
items can sit queued longer before a slow consumer gets to them). Start from the actual burst size
you expect between producer and consumer speed, and adjust based on observed backpressure behavior
under real load rather than guessing a round number up front.
