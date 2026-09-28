# Testing Your Respawn Configuration

Respawn's own job is resetting state *for* other tests, but the reset configuration itself
(`RespawnerOptions`, which tables/schemas are excluded) is code that can be wrong — a table added
to `TablesToIgnore` by a typo, or a new table nobody remembered to add, both fail silently until
something notices leftover or missing data. Test the configuration directly rather than trusting it
by inspection.

## Asserting the reset actually clears expected tables

```csharp
[Fact]
public async Task ResetAsync_ClearsOrdersTable()
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    await connection.ExecuteAsync("INSERT INTO Orders (Id, Total) VALUES (1, 100)");

    await _respawner.ResetAsync(_connectionString);

    var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
    count.Should().Be(0);
}
```

A test like this, run once as part of the test-infrastructure setup, catches a `RespawnerOptions`
misconfiguration (a table that should be reset but was accidentally excluded) that would otherwise
only surface as unrelated tests mysteriously seeing leftover data from a previous run.

## Asserting excluded tables survive a reset

```csharp
[Fact]
public async Task ResetAsync_PreservesReferenceData()
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    var before = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CountryLookup");

    await _respawner.ResetAsync(_connectionString);

    var after = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CountryLookup");
    after.Should().Be(before);
}
```

This is the complementary check: confirming `TablesToIgnore`/`SchemasToInclude` actually keeps
seeded reference/lookup data intact, not just that the reset clears everything indiscriminately.

## Testing across multiple supported database providers

If a project supports more than one database engine (or maintains provider parity for future
migration), run the same reset-behavior assertions against each configured `DbAdapter` via a
shared, parameterized test base rather than duplicating the assertions per provider — the
assertions themselves (clears expected tables, preserves excluded ones) don't change per provider,
only the connection setup does.

## Common pitfall

Testing the checkpoint/reset logic against a database that doesn't match production schema drift is
a false sense of safety — if `Respawner.CreateAsync` builds its deletion plan once at startup and a
migration adds a new table afterward without recreating the respawner, the new table silently isn't
covered. A configuration test that runs against a freshly-migrated schema (not a long-lived,
possibly-stale test database) catches this class of drift.
