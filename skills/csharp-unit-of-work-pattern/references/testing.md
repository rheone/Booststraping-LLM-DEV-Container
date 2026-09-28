# Testing Code That Depends on IUnitOfWork

## How to test it

Code that depends on `IUnitOfWork` is a prime candidate for a unit test with an in-memory fake
rather than an integration test against a real store, because the interface's whole job is to
present a narrow, storage-agnostic surface (`Repository<TEntity>()`/named properties, plus
`SaveChanges()`) — exactly the kind of seam a fake can stand in for cleanly.

- **Fake the repositories, not the store.** Give the fake `IUnitOfWork` in-memory collections
  backing each repository, so assertions can inspect what was added/updated/removed without a
  database.
- **Assert on committed state, not on individual repository calls.** The pattern's contract is
  "these changes commit together" — a good test checks that the *entities* changed as expected
  after `SaveChanges()`, not that a specific repository method was called a specific number of
  times, which couples the test to the handler's implementation rather than its outcome.
- **Test atomicity by forcing a mid-operation failure.** A test that makes the second of three
  writes throw, then asserts the first write's effect was rolled back, verifies the property the
  whole pattern exists for. A fake `SaveChanges()` that only ever succeeds never exercises this.
- **Reserve a real transaction test for the actual implementation.** Whether a specific
  `IUnitOfWork` implementation correctly opens, commits, and rolls back a *real* transaction is an
  integration-test concern against a real or embedded store — a unit test of a caller's logic
  should not need one.

## Fake implementation

```csharp
public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();
    public bool SaveChangesCalled { get; private set; }
    public bool ShouldFailOnSave { get; set; }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if (!_repositories.TryGetValue(typeof(TEntity), out object? repo))
        {
            repo = new InMemoryRepository<TEntity>();
            _repositories[typeof(TEntity)] = repo;
        }
        return (IRepository<TEntity>)repo;
    }

    public int SaveChanges()
    {
        if (ShouldFailOnSave)
        {
            throw new InvalidOperationException("Simulated commit failure.");
        }
        SaveChangesCalled = true;
        return 1;
    }

    public void Dispose() { }
}

public sealed class InMemoryRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    public List<TEntity> Added { get; } = new();
    public List<TEntity> Updated { get; } = new();
    public List<TEntity> Removed { get; } = new();

    public TEntity? Find(object id) => Added.FirstOrDefault();
    public void Add(TEntity entity) => Added.Add(entity);
    public void Update(TEntity entity) => Updated.Add(entity);
    public void Remove(TEntity entity) => Removed.Add(entity);
}
```

## Most likely scenarios

**1. Verifying a handler commits every write it queued**

```csharp
[Fact]
public void PlaceOrder_AddsOrderAndReservesInventory()
{
    var unitOfWork = new FakeUnitOfWork();
    var handler = new PlaceOrderHandler(new StubUnitOfWorkFactory(unitOfWork));

    handler.Handle(new PlaceOrderCommand(customerId: 1, lineItems: [new(skuId: 42, quantity: 2)]));

    Assert.Single(unitOfWork.Repository<Order>().Added);
    Assert.Single(unitOfWork.Repository<InventoryItem>().Updated);
    Assert.True(unitOfWork.SaveChangesCalled);
}
```

**2. Verifying nothing commits partway through on failure**

```csharp
[Fact]
public void PlaceOrder_WhenSaveFails_DoesNotReportSuccess()
{
    var unitOfWork = new FakeUnitOfWork { ShouldFailOnSave = true };
    var handler = new PlaceOrderHandler(new StubUnitOfWorkFactory(unitOfWork));

    Assert.Throws<InvalidOperationException>(() =>
        handler.Handle(new PlaceOrderCommand(customerId: 1, lineItems: [new(skuId: 42, quantity: 2)])));

    Assert.False(unitOfWork.SaveChangesCalled);
}
```

**3. Verifying a validation failure never reaches SaveChanges**

```csharp
[Fact]
public void PlaceOrder_WithUnknownSku_ThrowsBeforeSaving()
{
    var unitOfWork = new FakeUnitOfWork();
    var handler = new PlaceOrderHandler(new StubUnitOfWorkFactory(unitOfWork));

    Assert.Throws<InvalidOperationException>(() =>
        handler.Handle(new PlaceOrderCommand(customerId: 1, lineItems: [new(skuId: 999, quantity: 1)])));

    Assert.False(unitOfWork.SaveChangesCalled);
}
```
