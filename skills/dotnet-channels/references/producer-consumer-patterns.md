# Producer-Consumer Patterns

## Single producer, single consumer

The simplest and fastest shape — set `SingleReader = true, SingleWriter = true` on the channel
options so the channel can use its least-synchronized internal path:

```csharp
var channel = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(capacity: 256)
{
    SingleReader = true,
    SingleWriter = true,
});

Task producer = Task.Run(async () =>
{
    await foreach (WorkItem item in SourceAsync())
    {
        await channel.Writer.WriteAsync(item);
    }
    channel.Writer.Complete();
});

Task consumer = Task.Run(async () =>
{
    await foreach (WorkItem item in channel.Reader.ReadAllAsync())
    {
        await HandleAsync(item);
    }
});

await Task.WhenAll(producer, consumer);
```

## Multiple producers, single consumer

Set `SingleWriter = false` (or omit it — `false` is the default) and have every producer call
`WriteAsync` on the same `ChannelWriter<T>`. Only the *last* producer to finish should call
`Complete()` — calling it from every producer independently completes the channel as soon as the
first one finishes, cutting off the others. Track completion with a counter or `Task.WhenAll`:

```csharp
var channel = Channel.CreateUnbounded<LogEntry>();

Task[] producers = sources.Select(source => Task.Run(async () =>
{
    await foreach (LogEntry entry in source.ReadAsync())
    {
        await channel.Writer.WriteAsync(entry);
    }
})).ToArray();

// Complete only after every producer has finished writing.
_ = Task.WhenAll(producers).ContinueWith(
    _ => channel.Writer.Complete(),
    TaskScheduler.Default);

await foreach (LogEntry entry in channel.Reader.ReadAllAsync())
{
    Persist(entry);
}
```

## Single producer, multiple consumers

Start several consumer tasks reading from the same `ChannelReader<T>`. Each item is delivered to
exactly one consumer — the channel does not broadcast; it distributes. Use this to fan out
CPU-bound or I/O-bound work across a fixed pool of workers:

```csharp
var channel = Channel.CreateBounded<Job>(capacity: 500);

Task[] workers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
{
    await foreach (Job job in channel.Reader.ReadAllAsync())
    {
        await ExecuteAsync(job);
    }
})).ToArray();

Task producer = Task.Run(async () =>
{
    foreach (Job job in LoadJobs())
    {
        await channel.Writer.WriteAsync(job);
    }
    channel.Writer.Complete();
});

await Task.WhenAll(producer, Task.WhenAll(workers));
```

Because every worker enumerates the same `ReadAllAsync()` sequence, the channel's internal
synchronization — not an external lock — handles handing each item to exactly one waiting consumer.

## Multiple producers, multiple consumers

Combine the two patterns above: any number of tasks may call `WriteAsync` on the writer, and any
number of tasks may enumerate the reader concurrently. Track producer completion the same way as the
multiple-producer case (only complete the writer once every producer is done), and let consumers run
until `ReadAllAsync()` ends naturally.

## Common mistake: completing too early or too late

- **Too early**: calling `Complete()` before all producers finish drops in-flight writes — any
  `WriteAsync` call still queued when `Complete()` runs throws `ChannelClosedException`. Gate
  `Complete()` behind an accurate signal that every producer is actually done (a `Task.WhenAll` over
  producer tasks, not a fixed delay or an assumption about ordering).
- **Never**: forgetting to call `Complete()` at all leaves every consumer awaiting `ReadAllAsync()`
  (or `WaitToReadAsync`) forever, since nothing tells the channel there's nothing left to read — the
  consumer task never completes and the process can hang on shutdown.
