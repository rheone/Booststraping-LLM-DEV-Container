---
name: dotnet-health-checks
description: Guidance on ASP.NET Core health checks (Microsoft.Extensions.Diagnostics.HealthChecks) — implementing IHealthCheck, registering checks with AddHealthChecks/AddCheck/AddCheck<T>, exposing them with MapHealthChecks and customizing the endpoint response (status codes, JSON output, caching headers), tagging checks and filtering by tag for separate readiness vs. liveness endpoints, the generic pattern for registering a check against an external dependency (database, cache, message broker, downstream HTTP API), the ecosystem of community-maintained health check packages, and what a liveness vs. readiness endpoint means to a container orchestrator. Use when adding a /health or /healthz endpoint, wiring up Kubernetes liveness/readiness probes, writing a custom IHealthCheck for a dependency, or debugging why a health check endpoint reports unhealthy or never recovers.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# ASP.NET Core Health Checks

Guidance on `Microsoft.Extensions.Diagnostics.HealthChecks` (current stable release: **10.0.12**,
shipping alongside **.NET 10**, part of the [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore)
repository). Health checks give an ASP.NET Core app a standard way to report its own status — and the
status of things it depends on — to whatever is watching it (a load balancer, a container
orchestrator, an uptime monitor).

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Writing an `IHealthCheck` implementation, or registering one with `AddHealthChecks`/`AddCheck`/`AddCheck<T>` | [references/core-concepts.md](references/core-concepts.md) |
| Exposing checks at an endpoint with `MapHealthChecks`, customizing the response body/status codes/caching | [references/endpoint-configuration.md](references/endpoint-configuration.md) |
| Splitting checks into readiness vs. liveness endpoints with tags and a `Predicate` | [references/tags-and-filtering.md](references/tags-and-filtering.md) |
| Registering a check for an external dependency (database, cache, message broker, downstream API) | [references/external-dependency-checks.md](references/external-dependency-checks.md) |
| Understanding what a liveness vs. readiness endpoint means to a container orchestrator | [references/orchestrator-integration.md](references/orchestrator-integration.md) |
| Testing custom `IHealthCheck` implementations or the health check endpoint itself | [references/testing.md](references/testing.md) |

## Quick start

The most common shape — register health checks, map an endpoint, done:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
```

With no checks registered at all, `AddHealthChecks()` alone plus `MapHealthChecks` still gives a
minimal liveness signal: the endpoint reports healthy simply by virtue of the app responding to the
request.

## Out of scope

- Application Performance Monitoring / distributed tracing (`System.Diagnostics.Activity`,
  OpenTelemetry) — a related but separate observability concern with its own instrumentation model,
  not the pass/fail dependency-status reporting this skill covers.
- Any specific community-maintained check package's own configuration API — see
  [references/external-dependency-checks.md](references/external-dependency-checks.md) for the
  generic registration pattern that applies regardless of which package (or hand-written
  `IHealthCheck`) supplies a given check.
