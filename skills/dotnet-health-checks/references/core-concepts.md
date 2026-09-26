# Core Concepts

## Implementing IHealthCheck

A health check is any class implementing `IHealthCheck`, a single-method interface:

```csharp
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database connection succeeded.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }
}
```

`CheckHealthAsync` returns a `HealthCheckResult` — one of three statuses, each with an optional
description and an optional data dictionary for extra diagnostic key/value pairs:

- `HealthCheckResult.Healthy(description?, data?)`
- `HealthCheckResult.Degraded(description?, exception?, data?)` — functioning, but impaired (e.g. a
  slow but successful response, or a non-critical dependency that's unavailable)
- `HealthCheckResult.Unhealthy(description?, exception?, data?)`

The class itself participates in dependency injection like any other service — constructor-inject
whatever the check needs (a connection factory, an `HttpClient`, a configuration object) rather than
constructing dependencies inline.

## Registering checks

`AddHealthChecks()` on `IServiceCollection` returns an `IHealthChecksBuilder`, the entry point for
registering every check:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")          // resolved from DI, one instance per check run
    .AddCheck("self", () => HealthCheckResult.Healthy()) // inline delegate, no class needed
    .AddCheck("disk-space", new DiskSpaceHealthCheck(thresholdBytes: 1_000_000_000));
```

- **`AddCheck<TCheck>(name, ...)`** — resolves `TCheck` from the DI container each time the check
  runs. Use this for any check with dependencies (a database, an `HttpClient`, configuration).
- **`AddCheck(name, () => HealthCheckResult...)`** — a synchronous inline delegate. Use this only for
  trivial, dependency-free checks (a hardcoded "yes, I'm alive" signal); anything doing real I/O needs
  the async `IHealthCheck` form instead.
- **`AddCheck(name, instance)`** — registers an already-constructed `IHealthCheck` instance directly,
  bypassing DI resolution for that check. Useful when the instance needs constructor arguments that
  aren't themselves DI-registered services.

Every `AddCheck*` overload accepts optional `failureStatus`, `tags`, and `timeout` parameters —
`failureStatus` overrides what status an exception or a `Degraded`/`Unhealthy` result reports as
overall (defaulting to `HealthStatus.Unhealthy`), and `tags` is the mechanism
[tags-and-filtering.md](tags-and-filtering.md) builds on.

## Registering DI dependencies the check itself needs

A check registered via `AddCheck<TCheck>` only resolves correctly if `TCheck` (and everything it
depends on) is itself registered in the container — register the check's own dependencies the same
way as any other service, before or after the `AddHealthChecks()` call:

```csharp
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");
```

## Throwing vs. returning Unhealthy

Prefer catching an exception inside `CheckHealthAsync` and returning `HealthCheckResult.Unhealthy`
with the exception attached (as in the example above) over letting the exception propagate — a
returned `Unhealthy` result lets the health check middleware continue running every *other*
registered check and produce a complete report; an unhandled exception from one check can prevent the
rest of the report from being generated at all.
