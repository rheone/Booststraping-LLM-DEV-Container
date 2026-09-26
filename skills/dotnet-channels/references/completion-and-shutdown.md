# Completion and Graceful Shutdown

## Signaling completion

`ChannelWriter<T>.Complete(Exception? error = null)` tells the channel that no more items will ever
be written. Call it exactly once, from the code that knows all producers are finished:

```csharp
channel.Writer.Complete(); // no more writes are coming; existing buffered items still drain normally
```

Completion doesn't discard anything already in the buffer — consumers still read every item that was
written before `Complete()` was called. It only means `WriteAsync`/`TryWrite` calls made *after*
`Complete()` throw `ChannelClosedException`, and that readers eventually see the channel as fully
drained once the buffer empties.

## Observing completion from the consumer side

`ReadAllAsync()` is the simplest way to observe completion — the `await foreach` loop ends on its own
once the writer has completed and every buffered item has been read:

```csharp
await foreach (Item item in channel.Reader.ReadAllAsync())
{
    Process(item);
}
// Execution reaches here only after Complete() was called and the buffer fully drained.
```

`ChannelReader<T>.Completion` is a `Task` you can `await` directly when you need to know the channel
is done without necessarily being the one draining it (e.g. a supervisor task waiting alongside
several independent consumer tasks):

```csharp
await channel.Reader.Completion; // completes once Complete() was called and the buffer is empty
```

## Propagating an exception through the channel

Passing an exception to `Complete(error)` makes `Completion` fault with that exception, and makes any
in-progress or subsequent `ReadAsync`/`ReadAllAsync` calls rethrow it once the buffer drains:

```csharp
try
{
    await ProduceAllAsync(channel.Writer);
    channel.Writer.Complete();
}
catch (Exception ex)
{
    channel.Writer.Complete(ex); // consumers observe this exception once they finish draining
}
```

This is the correct way to tell consumers "the producer failed" without silently truncating the
data already queued — buffered items are still delivered first, and the fault surfaces only once
they're exhausted.

## Graceful shutdown checklist

1. Stop accepting new work into the producer side (e.g. stop a hosted service's background loop).
2. Let any producer already mid-write finish its current `WriteAsync` call.
3. Call `Complete()` (or `Complete(exception)` on a failure path) exactly once, after step 2, from a
   place that has certainty all producers are done — not from inside a single producer when there
   might be others still running (see the multi-producer completion pattern in
   [producer-consumer-patterns.md](producer-consumer-patterns.md)).
4. Await consumer tasks (or `Reader.Completion`) so shutdown doesn't proceed until the buffer is fully
   drained and every in-flight item has actually been processed.
5. Pass a `CancellationToken` into `WriteAsync`/`ReadAsync`/`ReadAllAsync` when shutdown needs a hard
   deadline in addition to graceful draining — on cancellation these throw `OperationCanceledException`
   instead of waiting indefinitely, which matters if a consumer might otherwise never finish draining
   (e.g. it's stuck on a hung downstream call).
