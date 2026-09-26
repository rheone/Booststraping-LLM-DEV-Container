# Error Handling

## RpcException and Status

Every gRPC error surfaces on the client as an `RpcException` carrying a `Status` — a `StatusCode`
plus a detail message:

```csharp
try
{
    var reply = await client.SayHelloAsync(new HelloRequest { Name = "" });
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
{
    Console.WriteLine($"Bad request: {ex.Status.Detail}");
}
```

`ex.StatusCode` is the enum value to branch on programmatically; `ex.Status.Detail` is a
human-readable message, not something to parse for structured error data — use rich error details
(below) when the caller needs more than a message string.

## Raising a specific status from the server

Throw `RpcException` with an explicit `Status` from a service method to signal a specific failure
category rather than letting an unrelated exception surface as an opaque `Unknown` status:

```csharp
public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required."));
    }
    return Task.FromResult(new HelloReply { Message = $"Hello, {request.Name}" });
}
```

An unhandled exception of any other type thrown from a service method is caught by the gRPC
infrastructure and translated into `StatusCode.Unknown` with a generic message, deliberately not
including the original exception's details in the response sent to the client — treat an unexpected
`Unknown` status on the client as a signal to check server-side logs, not something the client can
usefully branch on beyond "something went wrong."

## Choosing a status code

The standard codes map to specific, well-defined situations — using the closest matching code (rather
than defaulting to `Internal` or `Unknown` for everything) is what lets a well-behaved client apply
the right handling automatically (e.g. some client libraries and proxies retry `Unavailable`
automatically, but never `InvalidArgument`, since retrying a request that's wrong by construction
would just fail the same way again):

- **`InvalidArgument`** — the request itself is malformed or fails validation.
- **`NotFound`** — the requested resource doesn't exist.
- **`AlreadyExists`** — a create-like operation conflicts with an existing resource.
- **`PermissionDenied`** — the caller is authenticated but not authorized for this operation.
- **`Unauthenticated`** — the caller's credentials are missing or invalid.
- **`FailedPrecondition`** — the operation can't proceed given the system's current state, but a
  different request state (or a retry after some other change) could succeed.
- **`DeadlineExceeded`** — the call didn't complete before its deadline (see
  [deadlines-and-cancellation.md](deadlines-and-cancellation.md); usually raised by the gRPC
  infrastructure itself, not thrown by application code).
- **`Unavailable`** — the service is transiently unreachable; safe for a client to retry.
- **`Internal`** — an unexpected server-side invariant was violated; not something the caller can
  fix by changing the request.

## Rich error details

For structured error data beyond a status code and a message (field-level validation errors, a
retry-after duration), attach `google.rpc.Status`-based error details via
`Grpc.Core.Metadata`/the `Google.Rpc.ErrorDetails` well-known types, which the client decodes back
into a strongly-typed object rather than parsing the plain-text detail string. This is the mechanism
for anything beyond "here's what went wrong in one sentence" — reach for it once a client needs to
programmatically act on specific pieces of the error, not just log or display it.
