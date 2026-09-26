# Streaming

## Server-to-client streaming

A hub method that returns `IAsyncEnumerable<T>` (or `Task<ChannelReader<T>>`) streams results to the
calling client incrementally instead of waiting to send one complete response:

```csharp
public sealed class CounterHub : Hub
{
    public async IAsyncEnumerable<int> Counter(
        int count, int delayMs, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < count; i++)
        {
            yield return i;
            await Task.Delay(delayMs, cancellationToken);
        }
    }
}
```

The `[EnumeratorCancellation]` attribute on the `CancellationToken` parameter matters: SignalR
supplies a token that's canceled if the client stops the stream (or disconnects) before it completes,
and without the attribute the compiler-generated iterator doesn't wire that token through to the
`yield`-based loop's own cancellation checks, so the server keeps producing values into a stream
nothing is reading.

`ChannelReader<T>` is the alternative for a producer that isn't naturally expressed as an
`async IEnumerable` iterator (e.g. wrapping an existing event-based or callback-based source):

```csharp
public ChannelReader<int> Counter(int count, int delayMs, CancellationToken cancellationToken)
{
    var channel = Channel.CreateUnbounded<int>();
    _ = WriteItemsAsync(channel.Writer, count, delayMs, cancellationToken);
    return channel.Reader;
}

private static async Task WriteItemsAsync(
    ChannelWriter<int> writer, int count, int delayMs, CancellationToken cancellationToken)
{
    try
    {
        for (var i = 0; i < count; i++)
        {
            await writer.WriteAsync(i, cancellationToken);
            await Task.Delay(delayMs, cancellationToken);
        }
        writer.Complete();
    }
    catch (Exception ex)
    {
        writer.Complete(ex);
    }
}
```

## Consuming a stream from the .NET client

```csharp
var channel = await connection.StreamAsChannelAsync<int>("Counter", 10, 500, cancellationToken);

while (await channel.WaitToReadAsync(cancellationToken))
{
    while (channel.TryRead(out var item))
    {
        Console.WriteLine(item);
    }
}
```

or, using `IAsyncEnumerable` directly on the client:

```csharp
await foreach (var item in connection.StreamAsync<int>("Counter", 10, 500, cancellationToken))
{
    Console.WriteLine(item);
}
```

## Client-to-server streaming

A hub method can also accept a stream from the client, by taking an `IAsyncEnumerable<T>` (or
`ChannelReader<T>`) parameter — the client sends items incrementally rather than needing the entire
sequence up front:

```csharp
public sealed class UploadHub : Hub
{
    public async Task UploadStream(IAsyncEnumerable<string> lines)
    {
        await foreach (var line in lines)
        {
            // process each line as it arrives
        }
    }
}
```

On the client, pass an `IAsyncEnumerable<T>` (e.g. produced by another `async IEnumerable` method or
a `Channel<T>`'s reader) as the corresponding argument to `SendAsync`/`InvokeAsync` and SignalR
streams it to the server automatically — you don't call a separate streaming-specific client method
for this direction.
