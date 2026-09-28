# Testing

Code that depends on a Refit interface depends on nothing more than that interface — Refit's
generated implementation is an internal detail the consuming code never touches directly. This
makes the interface itself the natural seam for a test double.

## Unit testing a consumer by faking the interface

```csharp
[Fact]
public async Task GetUser_WhenApiReturnsUser_ReturnsMappedResult()
{
    var api = Substitute.For<IUsersApi>();
    api.GetUser(42).Returns(new User(42, "Ada"));
    var service = new UserService(api);

    var result = await service.GetUser(42);

    result.DisplayName.Should().Be("Ada");
}
```

This is the fastest and most common layer — it verifies the consuming code's own logic (mapping,
branching on a null/not-found result, orchestration across multiple calls) without touching HTTP,
serialization, or Refit's generated code at all.

## Simulating an `ApiException`

```csharp
[Fact]
public async Task GetUser_WhenNotFound_ReturnsNull()
{
    var api = Substitute.For<IUsersApi>();
    var response = new HttpResponseMessage(HttpStatusCode.NotFound);
    api.GetUser(42).Throws(await ApiException.Create(
        new HttpRequestMessage(HttpMethod.Get, "/users/42"),
        HttpMethod.Get,
        response,
        new RefitSettings()));
    var service = new UserService(api);

    var result = await service.GetUser(42);

    result.Should().BeNull();
}
```

`ApiException.Create` is Refit's own factory (the same one the generated client uses internally),
so a test built this way constructs a genuinely representative exception rather than a hand-rolled
approximation of one.

## Integration-testing the generated client against a real or stubbed server

Reserve this layer for verifying the interface's routing attributes, serialization settings, and
header wiring actually produce the HTTP request you expect — not for testing the consuming code's
business logic, which the unit-test layer above already covers. Point `RestService.For<T>()` or
`AddRefitClient<T>()` at a `WireMock.Net` or ASP.NET Core `WebApplicationFactory`-hosted stub
server, then assert on the request the stub actually received (method, path, headers, body) and
the response Refit ultimately deserialized into the interface's return type. Avoid pointing tests
at the real, live third-party API — route/contract drift on the far end should fail loudly and
separately from a routine test run, not silently pass or intermittently flake a unit test suite.

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| Consuming code's own logic (mapping, branching, orchestration) | Fake the Refit interface directly; no HTTP involved |
| A specific failure path (404, validation error payload) | Fake the interface to throw a constructed `ApiException` |
| That routing attributes/serialization/headers produce the right request on the wire | Point the real generated client at a stub server and assert on the received request |

Most of the suite belongs in the first row. The stub-server layer should be a small, separate set
of tests whose only job is catching a mismatch between the interface definition and what the real
API actually expects.
