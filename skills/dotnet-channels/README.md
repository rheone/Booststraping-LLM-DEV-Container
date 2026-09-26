# Channels

Guidance on `System.Threading.Channels`, the BCL's async-first producer-consumer data structure —
the routing table (by situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern

| File | Covers |
| --- | --- |
| `core-api.md` | `Channel.CreateUnbounded`/`CreateBounded`, `ChannelWriter<T>`, `ChannelReader<T>`, `SingleReader`/`SingleWriter` options |
| `producer-consumer-patterns.md` | single/multiple producers and consumers, fan-out consumption, correctly signaling completion with more than one producer |
| `backpressure-and-bounded-channels.md` | bounded channels, `BoundedChannelFullMode` (`Wait`/`DropOldest`/`DropNewest`/`DropWrite`), sizing capacity |
| `completion-and-shutdown.md` | `Complete()`/`Completion`, propagating an exception through a channel, a graceful-shutdown checklist |
| `blockingcollection-contrast.md` | factual API-design contrast with `System.Collections.Concurrent.BlockingCollection<T>` |
| `testing.md` | asserting on pipeline output, testing completion/exception propagation, testing backpressure |

## Scope

`System.Threading.Channels` (current stable release **10.0.12**, shipping with **.NET 10**; in-box
since .NET Core 3.0, available as a standalone package targeting .NET Standard 2.0+ for earlier
targets). Covers channel creation, the writer/reader API surface, single- and multi-producer/consumer
patterns, backpressure via bounded channels, completion and graceful shutdown, the factual contrast
with `BlockingCollection<T>`, and testing channel-based code.

Out of scope: general `async`/`await`/`Task` fundamentals, and `System.IO.Pipelines` (a separate
byte-buffer-oriented API for stream parsing). See [SKILL.md](SKILL.md) for the full out-of-scope
list and rationale.

This skill is self-contained: it does not assume any other skill is installed.
