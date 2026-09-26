# Testing

## How to test it

Code that depends on a `DbContext` splits into two testing concerns: does your query/business logic
produce the right result against real relational semantics, and does your code call the context
correctly (the right entity states, the right `SaveChanges` calls). These need different techniques.

**Prefer a real provider over an in-memory fake for anything that runs a query.** EF Core ships an
in-memory provider (`Microsoft.EntityFrameworkCore.InMemory`) that looks tempting for unit tests,
but it does not enforce relational constraints, does not translate LINQ the way a real provider
does, and silently accepts queries a real database would reject (a `GroupBy` translation gap, a
type-conversion difference) — a test passing against the in-memory provider is not evidence the
same query works against SQL Server, Npgsql, or SQLite. Use SQLite's in-memory mode
(`UseSqlite("DataSource=:memory:")`, connection kept open for the test's duration) or a real
disposable database (a SQL Server/PostgreSQL container spun up for the test run) instead — either
gives you real relational query translation and constraint enforcement.

**Test against the same provider production uses when the query's translation is what's under
test.** A provider-specific translation gap (a function that translates against SQL Server but not
against SQLite) won't surface in a suite that only runs against SQLite if production runs SQL
Server. When that distinction matters for the code under test, run the query-translation tests
against the real target provider, not a stand-in.

**Isolate each test's data.** Wrap each test in a transaction rolled back at the end, or recreate the
schema per test class (`context.Database.EnsureDeleted()` / `EnsureCreated()`, or apply migrations
fresh) — tests that share mutable state through a persistent test database become order-dependent
and flaky.

**Test business logic that only reads/writes tracked entities against a context, not against
mocked `DbSet<T>` objects.** Mocking `DbSet<T>` (via a substitute or a hand-rolled fake
`IQueryable`) to unit-test a LINQ query only proves the query works against
`IEnumerable`/`IQueryable`-to-objects semantics, not against a real provider's translation — the
same trap as the in-memory provider, one level more manual.

## Most likely scenarios

**Testing a repository method that queries with filters and `Include`s.** Seed a real (SQLite or
disposable) database with known rows, call the repository method, and assert on the returned shape
— including that navigations you expect `Include`d are actually populated (a missing `Include`
often still compiles and still returns results, just with `null`/empty navigations, which a
same-provider test with real relational data will actually catch).

**Testing that a service correctly marks an entity for update/delete.** Load an entity into a real
context, call the service method, then assert on `context.Entry(entity).State` before calling
`SaveChanges` — this verifies your code produced the state transition it intended without needing a
full round trip to the database for every test.

**Testing a migration's `Up`/`Down` methods.** Apply the migration to a disposable database, assert
the resulting schema matches expectations (query `INFORMATION_SCHEMA`/provider-equivalent
metadata, or simply that the entity CRUD operations that depend on the new schema now succeed), then
apply `Down` and assert the schema reverts — this catches a migration whose `Down` method doesn't
actually undo the `Up` method's effect, a gap that otherwise only surfaces the first time someone
needs to roll back a real deployment.
