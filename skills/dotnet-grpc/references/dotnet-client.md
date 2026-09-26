# The .NET Client

## Creating a channel and client

`GrpcChannel` represents a connection to a gRPC endpoint; the generated `<Service>Client` wraps a
channel with strongly-typed methods for each RPC:

```csharp
using var channel = GrpcChannel.ForAddress("https://localhost:5001");
var client = new Greeter.GreeterClient(channel);

var reply = await client.SayHelloAsync(new HelloRequest { Name = "World" });
```

`GrpcChannel` is meant to be long-lived and reused across many calls, not created per call — creating
a channel involves setting up the underlying HTTP/2 connection, and channels are safe to share
concurrently across many simultaneous RPCs. In an ASP.NET Core client application, register the
generated client via `AddGrpcClient<T>()` (from `Grpc.Net.ClientFactory`) instead of constructing a
`GrpcChannel` manually — it integrates with `IHttpClientFactory` for connection pooling and lifecycle
management the same way a typed `HttpClient` registration does:

```csharp
builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri("https://localhost:5001");
});
```

## Calling each RPC shape

**Unary:** `await client.SayHelloAsync(request)` returns the response directly (or use the non-`Async`
overload for a synchronous-looking blocking call, rarely appropriate outside a script/test).

**Server streaming:**

```csharp
using var call = client.SayHelloStream(new HelloRequest { Name = "World" });
await foreach (var reply in call.ResponseStream.ReadAllAsync())
{
    Console.WriteLine(reply.Message);
}
```

**Client streaming:**

```csharp
using var call = client.SumHellos();
foreach (var name in names)
{
    await call.RequestStream.WriteAsync(new HelloRequest { Name = name });
}
await call.RequestStream.CompleteAsync();

var summary = await call.ResponseAsync;
```

Calling `CompleteAsync()` on the request stream is what tells the server no more messages are
coming — a client-streaming or bidirectional-streaming call whose request stream is never completed
leaves the server-side read loop waiting indefinitely (bounded only by whatever deadline is in
effect).

**Bidirectional streaming:** combine both — write to `call.RequestStream`, read from
`call.ResponseStream`, and call `CompleteAsync()` on the request stream once done sending, typically
from a separate task than the one reading responses so neither side blocks the other.

## Channel options

`GrpcChannelOptions` (the second argument to `GrpcChannel.ForAddress`) configures per-channel
concerns: a custom `HttpClient`/`HttpMessageHandler` (for client certificates, a proxy, or a custom
`SocketsHttpHandler` with connection-pooling tuning), the maximum receive/send message size, and a
default set of interceptors applied to every call made through the channel (see
[interceptors.md](interceptors.md)).

```csharp
var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
{
    MaxReceiveMessageSize = 16 * 1024 * 1024,
});
```
