# Fixture Integration Pattern

Respawn's own API — `Respawner.CreateAsync` once, `ResetAsync` repeatedly — is test-framework
agnostic. The pattern below describes how to wire that into a test suite generically: build one
shared fixture object that owns the `Respawner` and the connection, share it across every test
class that needs a clean database, and call `ResetAsync` at the boundary between tests. Adapt the
exact base type or attribute names to whatever your test framework's shared-fixture mechanism looks
like — the shape is the same regardless of which one you use.

## The shape

1. **A fixture class** constructed once for the whole run (or once per collection of test classes
   sharing a database), responsible for:
   - Opening the connection or confirming the database is reachable.
   - Calling `Respawner.CreateAsync` exactly once and storing the result.
   - Exposing the connection string (or a way to open a connection) for both the code under test and
     `ResetAsync` to use.

   ```csharp
   public class DatabaseFixture : IAsyncDisposable
   {
       public string ConnectionString { get; }
       private Respawner _respawner = null!;

       public DatabaseFixture(string connectionString)
       {
           ConnectionString = connectionString;
       }

       public async Task InitializeAsync()
       {
           _respawner = await Respawner.CreateAsync(ConnectionString, new RespawnerOptions
           {
               DbAdapter = DbAdapter.SqlServer,
               TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
           });
       }

       public Task ResetAsync() => _respawner.ResetAsync(ConnectionString);

       public ValueTask DisposeAsync() => ValueTask.CompletedTask;
   }
   ```

2. **A reset call at the right boundary.** Call `fixture.ResetAsync()` in whatever your test
   framework calls "runs before each test" — not "runs once before all tests," since the entire
   point is a clean slate per test, not per suite. Running it *before* each test (rather than after)
   is usually the safer default: a test that fails partway through and leaves the database dirty
   doesn't then poison the next test's teardown-driven reset.

   ```csharp
   public class OrderRepositoryTests
   {
       private readonly DatabaseFixture _fixture;

       public OrderRepositoryTests(DatabaseFixture fixture)
       {
           _fixture = fixture;
       }

       public async Task ResetDatabaseBeforeEachTest()
       {
           await _fixture.ResetAsync();
       }

       public async Task FindById_ReturnsInsertedOrder()
       {
           // Arrange: insert directly, or via the code under test.
           // Act / Assert against a database guaranteed empty except for this test's own inserts.
       }
   }
   ```

3. **One fixture instance shared across every test class touching that database**, not one per test
   class — recreating the `Respawner` per class re-runs the expensive schema inspection
   ([performance-considerations.md](performance-considerations.md)) for no benefit, since the schema
   itself doesn't change between test classes.

## Sequencing tests that share the fixture

Because every test using the same fixture resets the same physical database, tests sharing a
fixture instance must not run concurrently against it — two tests running in parallel could reset
the database out from under each other mid-assertion. Whatever your test framework's mechanism is
for serializing a group of tests that share expensive state, apply it to every test class using the
same `DatabaseFixture` instance.

## What this pattern does not cover

Provisioning the database server itself (starting a container, running migrations before the first
test) is a separate setup step that happens before the fixture's `InitializeAsync` — this pattern
assumes a reachable, already-migrated database exists by the time the fixture starts.
