# Deadlines and Cancellation

## Setting a deadline

A deadline is an absolute point in time by which the call must complete — set on `CallOptions` (or
the convenience overload most generated client methods accept) as a `DateTime`, not a duration:

```csharp
var deadline = DateTime.UtcNow.AddSeconds(5);
var reply = await client.SayHelloAsync(
    new HelloRequest { Name = "World" }, deadline: deadline);
```

If the call doesn't complete before the deadline, the client receives an `RpcException` with
`StatusCode.DeadlineExceeded` (see [error-handling.md](error-handling.md)), and the server side's
`ServerCallContext.CancellationToken` is canceled so a well-behaved server implementation stops
doing wasted work rather than continuing to compute a response nobody will receive.

A call with no deadline set at all runs with no time limit imposed by gRPC itself — it can hang as
long as the underlying connection stays open and the server never responds. Set a deadline for any
call to a dependency the caller can't fully trust to always respond promptly, which in practice means
most calls outside a script or a trusted, tightly-coupled internal call.

## CancellationToken

Passing a `CancellationToken` to a generated client method (most accept one via `CallOptions` or a
convenience parameter) lets the caller cancel the call independently of any deadline — a user
navigating away, a parent operation being canceled, a composite timeout built from
`CancellationTokenSource.CreateLinkedTokenSource`:

```csharp
using var cts = new CancellationTokenSource();
var reply = await client.SayHelloAsync(
    new HelloRequest { Name = "World" }, cancellationToken: cts.Token);
```

Canceling the token produces the same client-side effect as a deadline expiring — an `RpcException`
with a status the client catches — but the two are independent mechanisms: a deadline is a maximum
duration policy, cancellation is an external "stop this specific call now" signal. Using both
together (a deadline as a safety net, plus cancellation wired to something like a user action) is
common and not redundant.

## Server-side propagation

On the server, `ServerCallContext.CancellationToken` reflects both the client's deadline expiring and
the client disconnecting/canceling — pass it into every awaited operation inside the service method
(database calls, downstream HTTP calls, `Task.Delay`) so those operations actually stop rather than
running to completion for a caller that's already given up:

```csharp
public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
{
    var data = await _repository.GetDataAsync(request.Name, context.CancellationToken);
    return new HelloReply { Message = data };
}
```

Ignoring `context.CancellationToken` inside a long-running service method doesn't break correctness
immediately, but it means server resources keep working on a call the client has already stopped
waiting for — the same wasted-work problem cancellation propagation exists to prevent anywhere else
in .NET.

## Deadline propagation across service calls

When one gRPC service calls another gRPC service to fulfill an incoming request, the incoming call's
remaining deadline is not propagated to the outgoing call automatically — you compute the remaining
time from the incoming `ServerCallContext.Deadline` and set it explicitly on the outgoing call if you
want the downstream call bounded by however much time is left on the original request, rather than
getting its own independent deadline (or none at all).
