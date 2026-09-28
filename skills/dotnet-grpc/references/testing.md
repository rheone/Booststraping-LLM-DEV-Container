# Testing

## How to test it

A gRPC service class is, at its core, a plain C# class with methods taking a request message and a
`ServerCallContext` — testable directly without a running server for anything that's really a unit-
level concern, plus an integration layer for what genuinely needs the real transport.

**Unit-test a service method by calling it directly with a constructed `ServerCallContext`.** The
context itself is awkward to construct by hand (it's normally supplied by the gRPC server
infrastructure); either use a minimal test double that implements the members your method actually
reads (`CancellationToken`, `RequestHeaders`), or reach for a package-provided test-context helper if
one is part of your existing test dependencies. This exercises the method's actual logic —
validation, calls to injected dependencies, mapping to the response message — the same way you'd unit
test any other class.

```csharp
var service = new GreeterService();
var reply = await service.SayHello(new HelloRequest { Name = "World" }, TestServerCallContext.Create());

Assert.Equal("Hello, World", reply.Message);
```

**Use an in-process test server for anything that needs to exercise the real gRPC pipeline.**
`Grpc.Net.Client.Testing`'s in-process transport (via `GrpcChannel.ForAddress` against a `TestServer`
from `Microsoft.AspNetCore.TestHost`, wired to an in-process `SocketsHttpHandler` alternative) runs
your service through the actual ASP.NET Core gRPC middleware pipeline — interceptors, model binding,
authorization — without a real network socket. Reach for this to verify wiring concerns a direct unit
test can't see: that an interceptor is actually registered and runs, that `[Authorize]` actually
rejects an unauthenticated call, that the generated client and server actually agree on the wire
format for a message shape you're unsure about.

**Test streaming methods by driving the reader/writer interfaces directly for the unit-level case,**
and via the in-process test server for the end-to-end case. A server-streaming method's
`IServerStreamWriter<T>` and a client-streaming method's `IAsyncStreamReader<T>` are both interfaces
you can substitute or fake in a unit test — capture what was written to a fake writer, or feed a
fake reader a canned sequence of messages — without needing a live bidirectional connection for every
test.

**Test interceptors by asserting on their effect through the pipeline, not by calling their override
methods directly in isolation** unless the interceptor's logic is complex enough to warrant it — an
interceptor's entire job is to run as part of a chain, so a test that only calls
`UnaryServerHandler` directly with a stub continuation can miss an ordering bug that only shows up
with more than one interceptor registered.

## Most likely scenarios

**Testing a unary service method's validation and mapping logic.** The direct-call unit test pattern
above — the majority of gRPC service testing.

**Testing that a server-streaming method stops producing when the client cancels.** Drive the method
with a fake `IServerStreamWriter<T>` and a canceled token, and assert the loop actually exits rather
than continuing to attempt writes — this catches a missing `context.CancellationToken` check (see
[server-implementation.md](server-implementation.md)) without needing a real streaming client.

**Testing end-to-end error status mapping.** An in-process test server plus a real generated client:
call a method designed to trigger a specific `RpcException`, and assert the client actually receives
the expected `StatusCode` and detail — this is the layer where a status code chosen on the server
either does or doesn't survive the real serialization/deserialization round trip intact.
