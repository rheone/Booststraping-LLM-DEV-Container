# Testing Health Checks

## Unit-testing an IHealthCheck directly

An `IHealthCheck` implementation is an ordinary class with an ordinary DI-injected dependency — test
it the same way as any other unit under test, calling `CheckHealthAsync` directly with a fake/mock
dependency and a default `HealthCheckContext`:

```csharp
[Fact]
public async Task CheckHealthAsync_ReturnsHealthy_WhenConnectionSucceeds()
{
    var factory = new FakeConnectionFactory(shouldSucceed: true);
    var sut = new DatabaseHealthCheck(factory);

    HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext());

    Assert.Equal(HealthStatus.Healthy, result.Status);
}

[Fact]
public async Task CheckHealthAsync_ReturnsUnhealthy_WhenConnectionThrows()
{
    var factory = new FakeConnectionFactory(shouldSucceed: false);
    var sut = new DatabaseHealthCheck(factory);

    HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext());

    Assert.Equal(HealthStatus.Unhealthy, result.Status);
    Assert.NotNull(result.Exception);
}
```

Never let a check's unit test hit a real external dependency (a real database connection, a real
downstream API) — fake the dependency's client/connection interface the same way any other test would
isolate I/O, since a health check test that depends on real infrastructure being up defeats the point
of testing the check's own logic in isolation.

## Integration-testing the mapped endpoint

Use `WebApplicationFactory<TEntryPoint>` (or the equivalent in-memory test host) to verify the actual
HTTP contract — status code, tag-based filtering, response shape — end to end:

```csharp
[Fact]
public async Task ReadyEndpoint_Returns503_WhenADependencyCheckFails()
{
    await using var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IDbConnectionFactory>(new FailingConnectionFactory());
        }));

    using HttpClient client = factory.CreateClient();

    HttpResponseMessage response = await client.GetAsync("/health/ready");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
}

[Fact]
public async Task LiveEndpoint_Returns200_EvenWhenADependencyCheckFails()
{
    await using var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IDbConnectionFactory>(new FailingConnectionFactory());
        }));

    using HttpClient client = factory.CreateClient();

    HttpResponseMessage response = await client.GetAsync("/health/live");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

The second test is the one worth keeping around specifically — it asserts the readiness/liveness
separation actually holds (see [orchestrator-integration.md](orchestrator-integration.md)) rather than
just that the endpoints exist, which is exactly the property a future change to tagging or `Predicate`
configuration could silently break.

## Testing custom response-writer output

If `HealthCheckOptions.ResponseWriter` produces structured JSON (see
[endpoint-configuration.md](endpoint-configuration.md)), assert on the deserialized shape rather than
a raw string match, so the test survives incidental formatting changes:

```csharp
[Fact]
public async Task HealthEndpoint_ReturnsJsonWithPerCheckStatus()
{
    await using var factory = new WebApplicationFactory<Program>();
    using HttpClient client = factory.CreateClient();

    HttpResponseMessage response = await client.GetAsync("/health");
    var payload = await response.Content.ReadFromJsonAsync<HealthResponsePayload>();

    Assert.Contains(payload!.Checks, c => c.Name == "database" && c.Status == "Healthy");
}
```

## Gotchas specific to testing health checks

- **A `Degraded`/`Unhealthy` result and a thrown exception are different failure paths** — test both
  where a check's logic distinguishes them (e.g. a slow-but-successful dependency reported as
  `Degraded` vs. a connection failure reported as `Unhealthy` with an attached exception), since the
  registration-time `failureStatus` override only affects the latter.
- **A check `timeout` needs its own test if the check's own code paths handle cancellation
  explicitly** — otherwise a check that ignores its `CancellationToken` can pass every unit test (which
  typically completes fast against a fake) while still hanging past its configured timeout against the
  real dependency; verify the check's I/O calls actually accept and pass through the token.

## Most likely scenarios

1. **Unit-testing a custom `IHealthCheck`** against fakes for both the healthy and unhealthy/degraded
   paths.
2. **Integration-testing that readiness and liveness endpoints genuinely diverge** when a dependency
   check fails — the property most worth protecting with a test, since it's also the property most
   likely to regress silently when tags or registrations change.
3. **Testing a custom JSON response-writer's output shape** against the deserialized payload rather
   than exact string matching.
