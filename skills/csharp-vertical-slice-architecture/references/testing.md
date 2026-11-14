# Testing slices

## Default: integration-first

Test each slice end to end against the real database engine. Both primary sources point here:

- Bogard's ContosoUniversity has only integration tests. A `SliceFixture` builds a
  `WebApplicationFactory<Program>`, starts SQL Server with Testcontainers, resets data with Respawn, and
  sends each request through real DI, real EF Core, and a real database, below the HTTP layer
  (<https://github.com/jbogard/ContosoUniversityDotNetCore-Pages>).
- The EF Core team: "We recommend that developers have good test coverage of their application running
  against their actual production database system." Avoid the in-memory provider and avoid mocking
  `DbSet` for queries (<https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy>).

Follow the repo's test framework, assertion library, and container tooling (standing rule 2).

## Test levels

| Level | Drives | Use for | Notes |
| --- | --- | --- | --- |
| **HTTP slice test** | `WebApplicationFactory.CreateClient()` | Routing, binding, validation, authorization, status codes, the outcome switch | The most complete; slower |
| **Handler slice test** | The handler resolved from the factory's DI scope | Business outcomes and data effects | Bogard's style; skips HTTP |
| **Unit test** | A domain type or a pure function | Invariants, calculations, domain services | No database |
| **Handler unit test with fakes** | The handler with fake seams | Only when data access sits behind a repository or unit of work the repo already has | Not with a mocked `DbSet` |
| **Architecture test** | Compiled assemblies | Isolation rules | See [architecture-tests.md](architecture-tests.md) |

Minimum per slice:

- One success test.
- One test per non-success outcome case.
- One invalid-input test (400) when the slice has input rules.
- One unauthorized/forbidden test when the slice has a policy.

## Test project layout

```text
tests/MyApp.IntegrationTests/
├── Infrastructure/
│   ├── AppFactory.cs              # WebApplicationFactory<Program> + database container
│   └── DatabaseReset.cs           # Respawn or equivalent
└── Features/Orders/
    ├── CreateOrder/CreateOrderTests.cs
    └── GetOrderById/GetOrderByIdTests.cs
tests/MyApp.UnitTests/Domain/Orders/OrderTests.cs
tests/MyApp.ArchitectureTests/SliceIsolationTests.cs
```

Expose the host to tests with `public partial class Program;` or `InternalsVisibleTo`.

## Fixture shape

```csharp
public sealed class AppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));
        });

    public async ValueTask InitializeAsync()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _db.StartAsync(ct);
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(ct);
    }

    public override async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

The shape uses xUnit v3 lifetimes, Testcontainers, and Npgsql. Swap each for the repo's equivalents.

Async in tests: pass the framework's test token to every async call (xUnit v3
`TestContext.Current.CancellationToken`, which analyzer xUnit1051 checks; MSTest `TestContext.CancellationToken`;
NUnit `TestContext.CurrentContext.CancellationToken`), so a cancelled or timed-out run stops promptly. Do not
add `ConfigureAwait(false)` in test code (xUnit1030); the `.editorconfig` in
[slice-anatomy.md](slice-anatomy.md#async-and-cancellation) turns CA2007 off for `tests/`.
Facts: `WebApplicationFactory` is in `Microsoft.AspNetCore.Mvc.Testing`; override services with
`ConfigureTestServices`; mock authentication with a test `AuthenticationHandler`
(<https://learn.microsoft.com/aspnet/core/test/integration-tests>).

## HTTP slice test

```csharp
public sealed class CreateOrderTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Creates_order_for_existing_customer()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        Guid customerId = await app.SeedCustomerAsync(ct);
        await app.SeedProductAsync("SKU-1", price: 10m, ct);
        HttpClient client = app.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders",
            new CreateOrderRequest(customerId, [new CreateOrderLine("SKU-1", 2)]), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        OrderCreated? body = await response.Content.ReadFromJsonAsync<OrderCreated>(ct);
        Assert.NotNull(body);
        Assert.True(await app.ExistsAsync<Order>(body.OrderId, ct));
    }

    [Fact]
    public async Task Returns_404_for_unknown_customer()
    {
        HttpClient client = app.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders",
            new CreateOrderRequest(Guid.NewGuid(), [new CreateOrderLine("SKU-1", 1)]),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_when_no_lines()
    {
        HttpClient client = app.CreateAuthenticatedClient();

        CancellationToken ct = TestContext.Current.CancellationToken;
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders",
            new CreateOrderRequest(Guid.NewGuid(), []), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        Assert.Contains("Lines", problem!.Errors.Keys);
    }
}
```

`SeedCustomerAsync`, `SeedProductAsync`, `ExistsAsync`, and `CreateAuthenticatedClient` are small helpers
on the factory. Async helpers take `CancellationToken ct = default` last, like production code. Seed through the `DbContext` or session, not through other slices' endpoints, so one
slice's test never depends on another slice.

## Handler slice test

```csharp
[Fact]
public async Task Cancelling_a_shipped_order_returns_already_shipped()
{
    CancellationToken ct = TestContext.Current.CancellationToken;
    Guid orderId = await app.SeedShippedOrderAsync(ct);
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    CancelOrderHandler handler = scope.ServiceProvider.GetRequiredService<CancelOrderHandler>();

    CancelOrderResult result = await handler.HandleAsync(orderId, ct);

    Assert.IsType<OrderAlreadyShipped>(result);
}
```

`CancelOrderResult` here is a closed hierarchy, so you assert on `result` itself. For a C# 15 `union`
(`CreateOrderResult`), assert on `result.Value`, which holds the active case.

## Unit tests

- Domain invariants: construct the entity, call the method, assert state or the returned outcome.
- Pure calculations and domain services.
- A static Minimal API handler that returns `TypedResults` can be unit-tested by asserting the result
  type (`Assert.IsType<Ok<Todo[]>>(result)`) when its dependencies are simple.

## Gotchas

- **Data isolation**: reset the database between tests (Respawn, a transaction per test, or a fresh
  database per test class). Pick one per repo.
- **Parallelism**: tests that share a database cannot run in parallel without isolation per test.
- **Time**: inject `TimeProvider` and use a fake time provider in tests.
- **Authentication**: register a test authentication scheme; never disable authorization globally.
- **Validation**: assert the 400 for every slice with input rules; a misplaced `AddValidation` call fails
  silently otherwise.
- **Outbox and events**: assert the outbox row or the recorded domain event, not the broker.

## Verifying the architecture work itself

For workflows that restructure code (migration, promotion), the behavior contract is the test:

- Capture HTTP-level tests (or snapshot tests of responses) for every endpoint you will move, before you
  move it.
- After each move, the same tests pass unchanged, and the architecture tests pass.
