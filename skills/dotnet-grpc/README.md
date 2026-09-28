# gRPC for .NET

gRPC for .NET defines a service contract in a `.proto` file and generates the server base class and
client stub from it, using `Grpc.AspNetCore` to host the service and `Grpc.Net.Client` to call it.
This skill covers the four RPC patterns, the .NET client, interceptors, deadlines/cancellation, and
error handling.

## When to reach for it

- You're writing a `.proto` file and need the generated C# code to come out right.
- You're implementing a server method and deciding between unary, server-streaming, client-streaming, or bidirectional-streaming.
- You're calling a gRPC service from .NET and setting up the channel and client stub.
- You need a client or server interceptor for logging, auth headers, or retries.
- A call needs a deadline so a hung or slow server doesn't block the caller indefinitely.
- You're raising or handling an `RpcException` and choosing the right `StatusCode`.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches defining a gRPC
service contract, implementing or calling a gRPC method, interceptors, deadlines, or error status
handling. You can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| `.proto` syntax basics, the `<Protobuf>` MSBuild item, generated base classes/client stubs | [references/proto-and-codegen.md](references/proto-and-codegen.md) |
| Unary, server-streaming, client-streaming, and bidirectional-streaming service methods | [references/server-implementation.md](references/server-implementation.md) |
| `GrpcChannel`, `GrpcChannelOptions`, the generated client stub, channel reuse | [references/dotnet-client.md](references/dotnet-client.md) |
| The `Interceptor` base class, client and server interceptors | [references/interceptors.md](references/interceptors.md) |
| `CallOptions.Deadline`, `CancellationToken` propagation | [references/deadlines-and-cancellation.md](references/deadlines-and-cancellation.md) |
| `RpcException`, `Status`/`StatusCode`, mapping exceptions to statuses | [references/error-handling.md](references/error-handling.md) |
| Testing a gRPC service and client code | [references/testing.md](references/testing.md) |

## Example prompts

- "Define a proto service for a greeter and generate the C# server and client code."
- "Add a client interceptor that attaches an auth token to every outgoing call."
- "This unary call hangs when the server is slow: add a deadline so it fails fast instead."
