---
name: dotnet-grpc
description: Guidance on gRPC for .NET (Grpc.AspNetCore server, Grpc.Net.Client client; verified current release 2.83.0, Apache-2.0) — defining a service in a .proto file and its code generation, implementing a server-side service class, the four RPC patterns (unary, server-streaming, client-streaming, bidirectional-streaming), the .NET client (GrpcChannel, generated client stubs), interceptors for cross-cutting concerns, deadlines and cancellation, and error handling via RpcException/Status codes. Use when defining a gRPC service contract, implementing or calling a gRPC method, writing a client or server interceptor, setting a call deadline, or handling/mapping a gRPC error status.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# gRPC for .NET

Guidance on gRPC for .NET — `Grpc.AspNetCore` for hosting a gRPC service and `Grpc.Net.Client` for
calling one. Organized by task, not by version — the core service-definition and call-pattern API
has been stable across recent releases; each reference file notes a version fact inline where it
matters.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Writing a `.proto` file and generating C# code from it | [references/proto-and-codegen.md](references/proto-and-codegen.md) |
| Implementing a server-side service class, choosing unary/server-streaming/client-streaming/bidirectional-streaming | [references/server-implementation.md](references/server-implementation.md) |
| Calling a gRPC service from .NET: `GrpcChannel`, the generated client stub | [references/dotnet-client.md](references/dotnet-client.md) |
| Writing a client or server interceptor for logging, auth headers, or retries | [references/interceptors.md](references/interceptors.md) |
| Setting a call deadline, or propagating/handling cancellation | [references/deadlines-and-cancellation.md](references/deadlines-and-cancellation.md) |
| Raising or handling an `RpcException`, choosing/mapping a `StatusCode` | [references/error-handling.md](references/error-handling.md) |
| Testing a gRPC service or client code | [references/testing.md](references/testing.md) |

## Quick start

```proto
// greet.proto
syntax = "proto3";
option csharp_namespace = "MyApp.Grpc";

service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply);
}

message HelloRequest { string name = 1; }
message HelloReply { string message = 1; }
```

```csharp
// Server
public sealed class GreeterService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context) =>
        Task.FromResult(new HelloReply { Message = $"Hello, {request.Name}" });
}

// Program.cs
builder.Services.AddGrpc();
app.MapGrpcService<GreeterService>();

// Client
using var channel = GrpcChannel.ForAddress("https://localhost:5001");
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloAsync(new HelloRequest { Name = "World" });
```

The single most common miss: calling a unary RPC with no deadline and no cancellation token wired to
anything, so a hung or slow-responding server call blocks the caller indefinitely instead of failing
fast. See
[references/deadlines-and-cancellation.md](references/deadlines-and-cancellation.md).

## Out of scope

- Protocol Buffers wire-format internals and the `protoc` compiler's own CLI beyond the MSBuild
  integration this skill's code-generation guidance covers.
- HTTP/2 transport-level configuration (TLS termination, ALPN negotiation) beyond what you set
  through `GrpcChannelOptions`/Kestrel's own gRPC-relevant settings — this skill covers the gRPC API
  surface built on top of that transport, not the transport's own configuration in general.
