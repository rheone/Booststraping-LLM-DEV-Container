# Server Implementation

Implement a service by deriving from the generated `<Service>Base` class and overriding the methods
corresponding to each RPC, then registering it with `AddGrpc()`/`MapGrpcService<T>()`.

## Unary

One request, one response — the most common shape:

```csharp
public sealed class GreeterService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context) =>
        Task.FromResult(new HelloReply { Message = $"Hello, {request.Name}" });
}
```

## Server streaming

One request, a stream of responses — the method writes to an `IServerStreamWriter<T>` instead of
returning a single value:

```proto
rpc SayHelloStream (HelloRequest) returns (stream HelloReply);
```

```csharp
public override async Task SayHelloStream(
    HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
{
    for (var i = 0; i < 5; i++)
    {
        await responseStream.WriteAsync(new HelloReply { Message = $"Hello #{i}, {request.Name}" });
        await Task.Delay(500, context.CancellationToken);
    }
}
```

Check `context.CancellationToken` (or pass it into awaited calls, as above) so the loop actually
stops if the client disconnects or cancels partway through — without it, the server keeps producing
and attempting to write to a stream the client has already abandoned.

## Client streaming

A stream of requests, one response — the method reads from an `IAsyncStreamReader<T>`:

```proto
rpc SumHellos (stream HelloRequest) returns (HelloSummary);
```

```csharp
public override async Task<HelloSummary> SumHellos(
    IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
{
    var count = 0;
    await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
    {
        count++;
    }
    return new HelloSummary { Count = count };
}
```

The method doesn't return until the client has finished sending (closed its request stream) — this
shape is for a client that needs to send an unbounded or large sequence of items and get one
aggregated result back, not for a request/response pair that happens to repeat.

## Bidirectional streaming

Both sides stream independently and concurrently — reads and writes are not required to alternate in
lockstep:

```proto
rpc Chat (stream ChatMessage) returns (stream ChatMessage);
```

```csharp
public override async Task Chat(
    IAsyncStreamReader<ChatMessage> requestStream,
    IServerStreamWriter<ChatMessage> responseStream,
    ServerCallContext context)
{
    await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
    {
        await responseStream.WriteAsync(new ChatMessage { Text = $"Echo: {message.Text}" });
    }
}
```

Reading and writing on a bidirectional stream both run against the same `ServerCallContext`, but
writes to `responseStream` are not automatically synchronized with the read loop's iteration — a
service that needs to write independently of what it's currently reading (e.g. pushing server-
initiated messages while also processing client messages) runs the write side on its own task rather
than only inside the `await foreach` over incoming messages.

## Accessing call context

`ServerCallContext` exposes the request's metadata headers (`context.RequestHeaders`), lets you set
response headers/trailers (`context.WriteResponseHeadersAsync`, `context.ResponseTrailers.Add(...)`),
and exposes the caller's cancellation token and deadline — see
[deadlines-and-cancellation.md](deadlines-and-cancellation.md) for how the deadline propagates from
the client automatically.
