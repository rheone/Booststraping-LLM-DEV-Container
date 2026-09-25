# Testing slices

## Why slices are easy to test in isolation

A layered architecture's service method typically depends on a service interface, which is
implemented by resolving further interfaces (repository, mapper, external client), each of which
needs to be stood up or mocked to exercise even one code path — a layered test setup pulls in the
whole dependency chain the layering was built around. A slice's handler, by contrast, has a small
and explicit dependency set — usually just the persistence context and whatever infrastructure that
one use case genuinely needs — because it was never given a chance to accumulate the shared-service
surface layered architectures grow into. Testing one slice does not require standing up, mocking,
or understanding any other slice, since slices don't depend on each other by construction (see
[philosophy-and-organization.md](philosophy-and-organization.md)).

This is one of VSA's more concrete, low-controversy benefits: minimal setup per test, because the
handler under test has minimal collaborators to begin with.

## Unit-testing a slice's handler logic

The handler's business logic — validation, branching, calculation — can usually be tested directly
against the handler class, substituting only the persistence/infrastructure dependency (e.g., an
in-memory or fake implementation of whatever the handler depends on) rather than a whole mocked
service graph:

```csharp
[Fact]
public async Task HandleAsync_RejectsEmptyOrder()
{
    var handler = new CreateOrderHandler(new FakeOrderRepository());
    var request = new CreateOrderRequest(CustomerId: Guid.NewGuid(), Items: []);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => handler.HandleAsync(request, CancellationToken.None));
}

[Fact]
public async Task HandleAsync_PersistsOrderAndReturnsTotal()
{
    var repository = new FakeOrderRepository();
    var handler = new CreateOrderHandler(repository);
    var request = new CreateOrderRequest(
        CustomerId: Guid.NewGuid(),
        Items: [new OrderLineItem(ProductId: Guid.NewGuid(), Quantity: 2, UnitPrice: 9.99m)]);

    var response = await handler.HandleAsync(request, CancellationToken.None);

    Assert.Equal(19.98m, response.Total);
    Assert.Single(repository.SavedOrders);
}
```

Because the request/response types are small and purpose-built to this one slice (see
[slice-anatomy.md](slice-anatomy.md)), constructing test inputs and asserting on outputs stays
direct — no need to build up an unrelated shared DTO with fields this test doesn't care about.

## Integration-testing a slice end to end

Unit tests validate the handler's internal logic against a substituted dependency; integration
tests validate the whole slice against real infrastructure (a real database, typically via a
disposable/test-scoped instance, and — if the slice is reached through an HTTP entry point — a
real in-process HTTP pipeline). This catches what a handler-only unit test can't: a query that's
wrong against the real schema, a mapping mismatch, middleware/pipeline wiring that never reaches
the handler at all.

The two are complementary, not redundant, and both are cheap in VSA specifically because a slice's
surface area is small:

- **Unit test** — fast, no I/O, exercises the handler's branching logic exhaustively (every
  validation rule, every edge case) without paying database round-trip cost per case.
- **Integration test** — slower, exercises one or two representative paths through the real stack
  (happy path, one realistic failure path) to confirm the slice is wired correctly end to end,
  rather than re-deriving every branch the unit tests already cover.

A common split: every branch in a handler gets a unit test; every slice gets at least one
integration test confirming the request actually reaches the handler and the response actually
comes back correctly shaped, without re-testing every business-rule permutation at the integration
level.

## Test organization mirrors the Features/ folder structure

Because slices are the unit of change, tests are typically organized the same way — a test project
structure that mirrors `Features/`, so a slice and its tests are always the same number of folders
away from each other, and deleting a slice makes it obvious which tests to delete too:

```text
src/
  Features/
    Orders/
      CreateOrder/
        CreateOrderRequest.cs
        CreateOrderHandler.cs
        CreateOrderResponse.cs
tests/
  Features/
    Orders/
      CreateOrder/
        CreateOrderHandlerTests.cs          # unit tests
        CreateOrderEndpointTests.cs         # integration test(s)
```

This mirrors the same "colocate everything about one feature" instinct that motivates the
production folder structure in [philosophy-and-organization.md](philosophy-and-organization.md):
a reader (or an agent) working on `CreateOrder` can find its tests without searching a separately
organized test project, and a slice's tests never need to know about any other slice's tests, the
same way the slices themselves don't depend on each other.
