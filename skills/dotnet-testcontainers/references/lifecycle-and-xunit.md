# Container lifecycle in xUnit-style tests

## The problem `IAsyncLifetime` solves

Starting a container is asynchronous (pulling an image, waiting for readiness), but a constructor
can't be asynchronous. An xUnit-style test class that needs a container implements
`IAsyncLifetime`, which the test framework calls at the right async points instead:

```csharp
public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly IContainer _dbContainer = new ContainerBuilder()
        .WithImage("postgres:16")
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public Task InitializeAsync() => _dbContainer.StartAsync();

    public Task DisposeAsync() => _dbContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task Save_PersistsOrder()
    {
        var connectionString = BuildConnectionString(_dbContainer);
        // ... exercise the repository against the real database ...
    }
}
```

- The constructor only builds the container object (cheap, synchronous); `InitializeAsync` starts
  it before any `[Fact]`/`[Theory]` in the class runs.
- `DisposeAsync` stops and removes the container after every test in the class has run — implement
  it by delegating to the container's own `DisposeAsync()` rather than reimplementing teardown.
- One container per test class (not per test method) is the default shape when the container is
  expensive to start and the tests inside the class don't require test-level isolation from each
  other — see `references/performance.md` for when to split further or share more broadly.

## Sharing a container across a test class with a collection fixture

When several test classes need the *same* running container rather than one each, wrap the
container in a fixture class implementing `IAsyncLifetime` and share it via a collection fixture:

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public IContainer Container { get; } = new ContainerBuilder()
        .WithImage("postgres:16")
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database collection")]
public class OrderRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public OrderRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Save_PersistsOrder()
    {
        // use _fixture.Container
    }
}
```

The container starts once for every test class in the collection and is disposed once after the
last one finishes — trading test-to-test isolation (state from one test can leak into the next via
the shared container) for a large reduction in total container-start overhead. Reach for this only
when the tests sharing the container clean up their own state (transactions, per-test schemas/
databases, explicit deletes) rather than relying on a fresh container per test to paper over shared
mutable state.

## Ordering guarantees

`InitializeAsync` on a class fixture or collection fixture always completes before any test method
that depends on it runs, and `DisposeAsync` always runs after every dependent test has finished —
you don't need to guard against a test observing a not-yet-started or already-disposed container as
long as the container lives inside `IAsyncLifetime` rather than being started lazily inside a test
method body.
