# Testing Channel-Based Code

## What to assert on

Test the observable contract of the pipeline — what comes out of the reader given what went into the
writer — rather than internal channel state, which isn't exposed for inspection anyway:

```csharp
[Fact]
public async Task Pipeline_ForwardsEveryItemToTheConsumer()
{
    var channel = Channel.CreateUnbounded<int>();

    await channel.Writer.WriteAsync(1);
    await channel.Writer.WriteAsync(2);
    channel.Writer.Complete();

    var results = new List<int>();
    await foreach (int item in channel.Reader.ReadAllAsync())
    {
        results.Add(item);
    }

    Assert.Equal(new[] { 1, 2 }, results);
}
```

## Testing completion and exception propagation

Assert that `Completion` (or the `await foreach` loop) actually ends, and that a faulted completion
surfaces the right exception — both are easy to get wrong (see
[completion-and-shutdown.md](completion-and-shutdown.md)) and both are cheap to verify directly:

```csharp
[Fact]
public async Task Complete_WithException_FaultsCompletionTask()
{
    var channel = Channel.CreateUnbounded<int>();
    var expected = new InvalidOperationException("producer failed");

    channel.Writer.Complete(expected);

    var actual = await Assert.ThrowsAsync<InvalidOperationException>(
        () => channel.Reader.Completion);
    Assert.Same(expected, actual);
}
```

## Testing backpressure (`BoundedChannelFullMode.Wait`)

Assert that a bounded channel actually blocks a producer once full, rather than trusting the
configuration alone — this is the kind of behavior a refactor can silently break by switching
`FullMode` or capacity:

```csharp
[Fact]
public async Task BoundedChannel_BlocksWriterWhenFull()
{
    var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.Wait,
    });

    await channel.Writer.WriteAsync(1); // fills the single slot

    Task write2 = channel.Writer.WriteAsync(2).AsTask();
    await Task.Delay(50); // give the second write a chance to (not) complete
    Assert.False(write2.IsCompleted);

    await channel.Reader.ReadAsync(); // frees a slot
    await write2; // now completes
}
```

A short `Task.Delay` to assert something *hasn't* happened yet is inherently a little racy (it
proves "didn't complete within 50ms," not "can never complete without the read") but is standard
practice for this kind of test — pair it with the definitive assertion (`await write2` completing
only after the read) rather than relying on the delay alone.

## Gotchas specific to testing channel code

- **Don't assert on timing precision.** A test that measures exact wall-clock delay between a write
  and a downstream effect is inherently flaky under CI load. Assert on ordering and completion
  instead (as above), not elapsed time.
- **Always give a test a way to end.** A test that awaits `ReadAllAsync()` or `Completion` on a
  channel that's never completed hangs until the test runner's timeout — always ensure the test
  itself calls `Complete()` (directly or via the code under test) on every path, including
  early-return/exception paths in setup.
- **Prefer testing through the public reader/writer, not a wrapped abstraction's internals.** If
  production code wraps a channel inside a class (a queue service, a pipeline stage), test that
  class's public methods and let the channel itself remain an implementation detail — this keeps the
  test valid if the class later switches its internal buffering strategy.

## Most likely scenarios

1. **Verifying a producer emits the right sequence of items** for a given input — the straightforward
   write-then-drain-and-assert pattern above.
2. **Verifying a consumer correctly reacts to a failure signaled via `Complete(exception)`** — e.g.
   that a hosted service logs and exits cleanly rather than crashing the process.
3. **Verifying backpressure actually engages** under a bounded channel with `FullMode = Wait`, for
   pipelines where a producer must not be allowed to run unbounded ahead of its consumer.
