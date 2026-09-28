# Testing NHibernate Code

## Strategy: real DB (in a container) over in-memory substitutes

Prefer testing repositories/mappings against a real instance of your target database engine (spun up via a test container) over an in-memory SQLite substitute. Reasoning specific to NHibernate: mapping bugs (cascade misconfiguration, `Inverse()` mismatches, dialect-specific SQL generation, custom `IUserType` `SqlTypes` mismatches) frequently only surface against the real dialect — SQLite's type affinity and constraint behavior differs enough from SQL Server/PostgreSQL that these tests can pass against SQLite and fail in production. If the team already has SQLite-based tests from before this convention, don't mass-migrate them, but don't add new ones that way — flag it in review.

## Session-per-test

Each test should get its own `ISession` from a shared, expensive-to-build `ISessionFactory` (build once per test run/fixture, not per test) — mirrors the actual session-per-request lifetime convention (`session-lifecycle.md`) and catches lazy-loading boundary bugs that a longer-lived shared session across tests would mask.

```csharp
[SetUp]
public void SetUp()
{
    _session = SharedSessionFactory.OpenSession(); // factory built once in [OneTimeSetUp]
    _transaction = _session.BeginTransaction();
}

[TearDown]
public void TearDown()
{
    _transaction.Rollback(); // roll back, don't commit — keeps tests isolated without needing a full DB reset per test
    _session.Dispose();
}
```

Rolling back rather than committing per test is the key trick — it gives test isolation without needing to truncate/reseed the database between every test.

## Testing lazy-loading boundary bugs specifically

Since `LazyInitializationException` only manifests when the session is actually closed, a test that keeps the session open the whole time (as above) won't catch a boundary bug that would occur in production's session-per-request lifetime. For code specifically meant to validate a repository/service boundary, write a dedicated test that closes the session between the "load" and "use" steps, mirroring the actual request lifecycle, rather than relying on the standard rollback-per-test session for this class of bug.

## Schema for tests

Generate the test database schema from the current NHibernate mappings (`SchemaExport`) at test-run startup rather than maintaining a hand-written parallel test schema — this also functions as a drift check (see `schema-mapping-roundtrip.md`): if `SchemaExport` fails or produces something unexpected, that's a mapping problem worth investigating on its own, independent of whatever test triggered it.
