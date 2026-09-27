# Testing Commands and Queries

Because commands and queries have deliberately different responsibilities, they're worth testing
differently too — a command handler's value is its business-rule enforcement, while a query
handler's value is that it returns the right shape from the right data, which usually means testing
it against something closer to a real store.

## Testing a command handler: unit test the business rules

```csharp
[Fact]
public async Task HandleAsync_ValidOrder_PersistsAndReturnsId()
{
    var repository = Substitute.For<IOrderRepository>();
    var handler = new CreateOrderHandler(repository);
    var command = new CreateOrderCommand("customer-1", [new OrderLineDto("sku-1", 2)]);

    var orderId = await handler.HandleAsync(command, CancellationToken.None);

    await repository.Received(1).AddAsync(Arg.Is<Order>(o => o.Id == orderId), Arg.Any<CancellationToken>());
}

[Fact]
public async Task HandleAsync_NoLines_ThrowsDomainException()
{
    var repository = Substitute.For<IOrderRepository>();
    var handler = new CreateOrderHandler(repository);
    var command = new CreateOrderCommand("customer-1", []);

    await Assert.ThrowsAsync<InvalidOrderException>(() => handler.HandleAsync(command, CancellationToken.None));
}
```

A command handler's dependencies (a repository, a domain service) are exactly the seams to fake —
the point of the test is the decision the handler and the domain model it calls into make, not
whatever storage technology sits behind the repository. Cover both the happy path (valid command
persists, returns expected identifier) and the business-rule rejections (invalid state transitions,
failed invariants) — these rejections are the entire reason the write side has a domain model at
all, and are the highest-value assertions a command handler's tests can make.

## Testing a query handler: closer to the real read path

A query handler that projects directly against a database (raw SQL, an ORM's `.Select()`) has very
little logic of its own to unit-test in isolation — its entire value is that the SQL/projection it
issues actually returns the right shape from real data. Faking the connection/`DbContext` verifies
almost nothing; test it against a real (or realistically disposable) database instead:

```csharp
[Fact]
public async Task HandleAsync_ExistingOrder_ReturnsSummary()
{
    await using var connection = await CreateTestDatabaseConnectionAsync();
    await SeedOrderAsync(connection, orderId: KnownOrderId, customerName: "Ada Lovelace", total: 150m);
    var handler = new GetOrderSummaryHandler(connection);

    var summary = await handler.HandleAsync(new GetOrderSummaryQuery(KnownOrderId), CancellationToken.None);

    Assert.NotNull(summary);
    Assert.Equal("Ada Lovelace", summary.CustomerName);
    Assert.Equal(150m, summary.Total);
}
```

Seed just enough data for the specific projection under test, then assert on the returned DTO's
shape and values — this catches the class of bug a mocked-connection unit test never can: a typo'd
column name, a join that silently drops rows, a projection that doesn't match the DTO's actual
fields.

## Testing a separate, asynchronously-synchronized read model

When the read side is populated by an event-driven projection (see
[separate-read-models-and-synchronization.md](separate-read-models-and-synchronization.md)), a test
that issues a command and immediately queries the read model races the projection consumer. Poll
with a timeout instead of asserting immediately:

```csharp
[Fact]
public async Task CreateOrder_Test_SummaryEventuallyAppearsInReadModel()
{
    var orderId = await commandDispatcher.SendAsync(new CreateOrderCommand("customer-1", [/* ... */]));

    var summary = await PollUntilAsync(
        () => queryDispatcher.SendAsync(new GetOrderSummaryQuery(orderId)),
        result => result is not null,
        timeout: TimeSpan.FromSeconds(5));

    Assert.NotNull(summary);
}
```

A test that instead sleeps a fixed duration before asserting is both slower than necessary on a fast
run and flaky on a slow one — polling with a timeout is the reliable way to assert against
eventually-consistent state without hard-coding how long "eventually" takes.
