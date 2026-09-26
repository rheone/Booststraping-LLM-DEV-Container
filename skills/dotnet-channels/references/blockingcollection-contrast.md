# Contrast with BlockingCollection<T>

`System.Collections.Concurrent.BlockingCollection<T>` is the BCL's older, synchronous
producer-consumer collection. The two types solve the same conceptual problem — hand items from one
or more producers to one or more consumers — with different concurrency models, and picking between
them is a factual API-design decision rather than a matter of one superseding the other.

## API design differences

| Aspect | `Channel<T>` | `BlockingCollection<T>` |
| --- | --- | --- |
| Core operation model | `async`/`await` (`WriteAsync`/`ReadAsync`) | Blocking calls (`Add`/`Take`) that occupy a thread while waiting |
| Non-blocking attempt | `TryWrite`/`TryRead`, both synchronous | `TryAdd`/`TryTake`, both synchronous |
| Enumeration | `ReadAllAsync()` returns `IAsyncEnumerable<T>` for `await foreach` | `GetConsumingEnumerable()` returns `IEnumerable<T>` for a blocking `foreach` |
| Backing store | Fixed to the channel's own internal queue implementation | Pluggable — wraps any `IProducerConsumerCollection<T>` (e.g. a stack instead of a queue) for custom ordering |
| Bounding policy | `BoundedChannelFullMode` (`Wait`/`DropOldest`/`DropNewest`/`DropWrite`) | Bounded capacity only supports blocking (`Add` blocks until space frees); no drop-oldest/newest equivalent |
| Completion signal | `Complete()`/`Completion`, observed via `await` | `CompleteAdding()`, observed via `IsCompleted`/the consuming enumerable ending |

## When each fits

- **`Channel<T>`** fits naturally into a codebase already built around `async`/`await` — an
  ASP.NET Core background service, an async pipeline stage, anything where blocking a thread pool
  thread to wait for an item would be wasteful. Its drop-based `BoundedChannelFullMode` options also
  give it a backpressure policy `BlockingCollection<T>` doesn't have.
- **`BlockingCollection<T>`** fits a purely synchronous, thread-based producer-consumer setup — a
  dedicated background `Thread` (not a `Task`) consuming work, or code that has no `async` call chain
  to integrate with and would gain nothing from one. Its pluggable backing collection is useful when
  the ordering semantics need to differ from FIFO (e.g. LIFO via a `ConcurrentStack<T>`-backed
  instance).

Neither type is a drop-in replacement for the other inside an existing call chain — swapping one for
the other means changing the surrounding code from blocking calls to `async`/`await` (or vice versa),
not just changing a type name.
