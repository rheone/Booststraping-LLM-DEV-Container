# gRPC for .NET

Task-organized guidance on gRPC for .NET — the routing table (by task, not version) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per version

| File | Covers |
| --- | --- |
| `proto-and-codegen.md` | .proto syntax basics, `<Protobuf>` MSBuild item, generated base classes/client stubs |
| `server-implementation.md` | unary, server-streaming, client-streaming, and bidirectional-streaming service methods |
| `dotnet-client.md` | GrpcChannel, GrpcChannelOptions, the generated client stub, channel reuse |
| `interceptors.md` | Interceptor base class, client and server interceptors |
| `deadlines-and-cancellation.md` | CallOptions.Deadline, CancellationToken propagation |
| `error-handling.md` | RpcException, Status and StatusCode, mapping exceptions to statuses |
| `testing.md` | Testing a gRPC service and client code |

## Scope

gRPC for .NET (`Grpc.AspNetCore` for the server, `Grpc.Net.Client` for the client, plus the shared
`Grpc.Core.Api`/`Google.Protobuf` code-generation surface). Out of scope: Protocol Buffers wire-format
internals and the standalone `protoc` CLI beyond MSBuild integration, and HTTP/2 transport
configuration beyond gRPC's own channel/Kestrel options.

Each reference file notes a version fact inline where relevant; version is not the file-splitting
axis for this skill (see SKILL.md for why).

## Verified facts (as of 2026-09-26)

- **Current latest release: Grpc.AspNetCore 2.83.0 and Grpc.Net.Client 2.83.0** (a prerelease
  2.84.0-pre1 exists ahead of it). Apache-2.0-licensed (the `grpc/grpc-dotnet` repository's license).
  Source: the NuGet Gallery package pages (nuget.org/packages/grpc.aspnetcore,
  nuget.org/packages/grpc.net.client) and the `grpc/grpc-dotnet` GitHub repository's `LICENSE` file.

These facts were verified via live web search against nuget.org and github.com at the time this
skill was written; re-verify before relying on the exact version number.
