# Tags and Filtering

## Why one endpoint often isn't enough

A single `/health` endpoint that runs every registered check conflates two different questions:
"is the process itself alive and not deadlocked" and "is the app ready to serve real traffic right
now, including everything it depends on." Conflating them means a slow or temporarily-unavailable
dependency can trigger the same response as an actually-crashed process, which usually implies the
wrong remedial action (see [orchestrator-integration.md](orchestrator-integration.md) for the
consequence of getting this wrong). Splitting into separate readiness and liveness endpoints — each
running a different subset of checks — solves this.

## Tagging checks

Every `AddCheck*` overload accepts a `tags` parameter — an arbitrary set of strings you define, with
no special meaning to the framework beyond what you filter on:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<MessageBrokerHealthCheck>("broker", tags: new[] { "ready" })
    .AddCheck<StartupHealthCheck>("startup", tags: new[] { "ready" });
    // no tag at all on a check means it only runs against an endpoint with no filtering predicate
```

## Filtering with `HealthCheckOptions.Predicate`

`MapHealthChecks` accepts a `Predicate` delegate that decides, per check, whether it runs for that
particular endpoint:

```csharp
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // runs no checks at all — the endpoint responding at all is the signal
});
```

The `/health/live` pattern above is deliberately minimal: excluding every check means the liveness
endpoint only reports whether the process can respond to an HTTP request at all, independent of
whether any dependency is currently reachable — exactly the narrow question a liveness probe should
answer (see [orchestrator-integration.md](orchestrator-integration.md)).

## A startup-gated readiness check

A common pattern: an app that needs to run a one-time startup task (warm a cache, run a migration
check, wait for a downstream dependency to become reachable) before it should receive real traffic,
tracked via a dedicated health check whose state a background service flips once startup completes:

```csharp
public sealed class StartupHealthCheck : IHealthCheck
{
    public bool StartupCompleted { get; set; }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(StartupCompleted
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Startup has not completed."));
}

builder.Services.AddSingleton<StartupHealthCheck>();
builder.Services.AddHostedService<StartupBackgroundService>(); // sets StartupCompleted = true when done
builder.Services.AddHealthChecks()
    .AddCheck<StartupHealthCheck>("startup", tags: new[] { "ready" });
```

Until the background service marks startup complete, `/health/ready` reports unhealthy while
`/health/live` (which runs no checks) reports healthy the whole time — telling an orchestrator "the
process is fine, just not ready for traffic yet" rather than triggering a restart.

## Choosing tag names

Nothing in the framework requires the specific strings `ready`/`live` — they're a convention, not an
API contract. Pick names that describe what a given check actually verifies (`ready`, `live`, or more
granular tags like `db`, `cache` for checks you want to expose at additional, narrower diagnostic
endpoints) and keep them consistent across the checks you register.
