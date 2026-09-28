# Testing

## How to test it

Hub logic splits into two testable layers: the business logic inside a hub method, and the
messaging/broadcast behavior (who receives what). Test them differently.

**Extract business logic out of the hub method body when it's non-trivial, and unit-test it
directly.** A hub method that validates input, calls a domain service, and then broadcasts a result
is really three concerns glued together — the validation and domain-service call don't need a `Hub`
instance at all to test; only the broadcast step touches SignalR-specific types.

**Test the broadcast behavior by substituting `Hub.Clients`.** `Hub.Clients` is a settable property
of type `IHubCallerClients<T>` (for `Hub<T>`) — construct the hub instance directly in a test, assign
a substitute/fake for `Clients` (and `Groups`, `Context` as needed), call the hub method, and assert
on what was invoked against the substitute:

```csharp
var mockClients = Substitute.For<IHubCallerClients<IChatClient>>();
var mockClientProxy = Substitute.For<IChatClient>();
mockClients.All.Returns(mockClientProxy);

var hub = new ChatHub { Clients = mockClients };
await hub.SendMessage("Alice", "Hello");

await mockClientProxy.Received(1).ReceiveMessage("Alice", "Hello");
```

This tests exactly what the hub method decided to do (which client group it targeted, what arguments
it sent) without needing a real connection, a real server, or a real transport.

**Reserve an integration test (a real `TestServer` plus a real `HubConnection`) for verifying the
end-to-end wiring** — that the hub is actually mapped at the expected route, that authorization is
actually enforced for an unauthenticated connection, that a client really receives a message sent via
`Clients.All`. This is slower and each test covers more surface at once; use it for the handful of
scenarios where the wiring itself (not the business logic) is what could be wrong.

## Most likely scenarios

**Testing that a hub method broadcasts to the right target.** The `Clients.Group`/`Clients.Caller`/
`Clients.User` substitution pattern above, asserting the correct targeting method was called with the
correct group name/user ID and the correct message arguments — this is the single most common hub
test.

**Testing authorization is enforced on a hub.** An integration test using `TestServer` and a real
`HubConnection`: attempt to connect (or invoke a specific method) without a valid token and assert
the connection or invocation fails; then repeat with a valid token and assert it succeeds. This is a
wiring concern (does `[Authorize]` actually apply the way you think) that a substituted-`Clients`
unit test can't exercise, since it never goes through the real authentication middleware pipeline.

**Testing a streaming hub method's cancellation behavior.** Call the streaming method directly
(`IAsyncEnumerable<T>` methods are plain async iterators outside of any SignalR machinery), start
enumerating it, cancel the token partway through, and assert the loop actually stops rather than
continuing to produce values — this catches a missing `[EnumeratorCancellation]` attribute (see
[streaming.md](streaming.md)) without needing a real client connection.
