# Registering Checks for External Dependencies

## The generic pattern

A check for an external dependency (a database, a cache, a message broker, a downstream HTTP API) is
just an `IHealthCheck` whose `CheckHealthAsync` performs the smallest possible operation that proves
the dependency is reachable and responsive — never a full business operation, just a connectivity/
liveness probe against that dependency:

```csharp
public sealed class DownstreamApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public DownstreamApiHealthCheck(IHttpClientFactory httpClientFactory) =>
        _httpClient = httpClientFactory.CreateClient("downstream-api");

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                "/ping", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"Downstream API returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Downstream API is unreachable.", ex);
        }
    }
}
```

Register it like any other check, injecting whatever client/connection factory the dependency needs
through the normal DI container:

```csharp
builder.Services.AddHttpClient("downstream-api", client =>
    client.BaseAddress = new Uri("https://downstream.example.com"));

builder.Services.AddHealthChecks()
    .AddCheck<DownstreamApiHealthCheck>("downstream-api", tags: new[] { "ready" });
```

## Timeouts matter for dependency checks specifically

An external-dependency check is the case most likely to hang — a database or downstream API that's
unreachable often means a connection attempt that never times out on its own, rather than a fast
failure. Always pass the check's own `timeout` at registration, and honor the `CancellationToken`
passed to `CheckHealthAsync` inside the check's own I/O calls:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DownstreamApiHealthCheck>(
        "downstream-api",
        tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(5));
```

Once the timeout elapses, the health check middleware reports that check as `Unhealthy` regardless of
what the check itself eventually returns — but only if the check's own code actually observes
cancellation; a check performing a call that ignores the passed `CancellationToken` can still hang the
overall health check run past the configured timeout.

## Choosing failureStatus for a non-critical dependency

Not every dependency failing should make the whole app report unhealthy — a dependency the app can
degrade gracefully without (an optional caching layer, a non-critical third-party integration) is a
candidate for `HealthStatus.Degraded` instead of the default `Unhealthy`:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<OptionalCacheHealthCheck>(
        "cache",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready" });
```

Reserve the default `Unhealthy` failure status for dependencies the app genuinely cannot function
correctly without.

## The ecosystem of community-maintained check packages

For common external dependencies (relational databases, caches, message brokers, cloud storage,
search engines, and similar), community-maintained NuGet packages exist that supply ready-made
`IHealthCheck` implementations for a given dependency, so a project doesn't need to hand-write the
connectivity probe itself — typically exposed as an `IHealthChecksBuilder` extension method
(`.Add<Dependency>Check(...)`) that internally does the same `AddCheck` registration shown above with
a client/connection already wired up. Evaluate any such package on the same basis as any other
third-party dependency before adopting it: current maintenance activity, license terms, and whether
its configuration surface actually matches the client library and version already in use in the
project.
