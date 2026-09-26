# .proto Definition and Code Generation

## Defining a service

A `.proto` file declares message shapes and the RPC methods a service exposes, in Protocol Buffers'
own interface-definition language:

```proto
syntax = "proto3";

option csharp_namespace = "MyApp.Grpc";

package myapp;

service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply);
  rpc SayHelloStream (HelloRequest) returns (stream HelloReply);
}

message HelloRequest {
  string name = 1;
}

message HelloReply {
  string message = 1;
}
```

Every field on a message carries a numeric tag (`= 1`, `= 2`, ...) that identifies it on the wire —
these numbers, not the field names, are what the binary encoding actually uses, which is why renaming
a field is backward-compatible but reusing or renumbering an existing tag for a different field is
not: an old client/server pair still decodes the new field using the old tag's original meaning.
`stream` before a request or response type marks that side of the RPC as streaming rather than a
single message (see [server-implementation.md](server-implementation.md) for what each combination
means).

## Wiring the file into the build

The `Grpc.Tools` package (a build-time-only dependency) drives code generation from MSBuild. Add the
`.proto` file as a `<Protobuf>` item in the `.csproj`, with `GrpcServices` set to `Server`, `Client`,
or `Both` depending on which side this project needs:

```xml
<ItemGroup>
  <PackageReference Include="Grpc.AspNetCore" Version="2.83.0" />
</ItemGroup>

<ItemGroup>
  <Protobuf Include="Protos\greet.proto" GrpcServices="Server" />
</ItemGroup>
```

A client-only project references `Grpc.Net.Client`, `Grpc.Tools`, and `Google.Protobuf` instead of
`Grpc.AspNetCore`, with `GrpcServices="Client"` on the same `.proto` item — sharing the identical
`.proto` file between server and client projects (via a shared file reference or a separate contracts
project) is what keeps both sides generated from the same contract rather than hand-copied and prone
to drifting apart.

## What gets generated

For a service, generation produces an abstract base class named `<Service>.<Service>Base` with one
virtual method per RPC (the server implements this — see
[server-implementation.md](server-implementation.md)) and a `<Service>.<Service>Client` class with
one method per RPC (the client calls this — see [dotnet-client.md](dotnet-client.md)). For every
message, generation produces a C# class with a settable property per field, implementing
`Google.Protobuf.IMessage` for serialization. Regeneration happens automatically on build whenever
the `.proto` file changes — there is no separate manual codegen step to remember once the MSBuild
item is wired up, but a stale build output after editing a `.proto` file usually means a rebuild
(not just a build) is needed if the IDE's incremental build didn't pick up the change.

## Common types and imports

`google/protobuf/timestamp.proto`, `duration.proto`, `empty.proto`, and `wrappers.proto` (for
nullable primitive wrapper types) are well-known Protocol Buffers types you `import` into your own
`.proto` file rather than redefining — `Grpc.Tools` resolves these standard imports automatically
without needing the `.proto` source files copied into your project.
