# ASP.NET Core Health Checks

Guidance on `Microsoft.Extensions.Diagnostics.HealthChecks` for reporting an app's status, and the
status of what it depends on, to whatever is watching it: a load balancer, a container
orchestrator, or an uptime monitor.

## When to reach for it

- You're adding a `/health` or `/healthz` endpoint to an ASP.NET Core app.
- You're wiring up separate Kubernetes liveness and readiness probes.
- You're writing a custom `IHealthCheck` for a database, cache, message broker, or downstream API,
  or debugging why a check reports unhealthy and never recovers.

## Using it

This skill fires automatically when your request involves adding a health check endpoint, writing
a custom check, or wiring up orchestrator probes. You can also invoke it directly with
`/dotnet-health-checks`.

## What it covers

| Topic | Reference |
| --- | --- |
| `IHealthCheck`, `HealthCheckResult`, `AddHealthChecks`/`AddCheck`/`AddCheck<T>` registration | [references/core-concepts.md](references/core-concepts.md) |
| `MapHealthChecks`, `HealthCheckOptions` (status codes, response writer, caching headers) | [references/endpoint-configuration.md](references/endpoint-configuration.md) |
| Tagging checks, `HealthCheckOptions.Predicate`, separate readiness/liveness endpoints | [references/tags-and-filtering.md](references/tags-and-filtering.md) |
| The generic registration pattern for a dependency check, timeouts, `failureStatus` | [references/external-dependency-checks.md](references/external-dependency-checks.md) |
| What liveness vs. readiness means to a container orchestrator | [references/orchestrator-integration.md](references/orchestrator-integration.md) |
| Unit-testing an `IHealthCheck` and integration-testing the mapped endpoint | [references/testing.md](references/testing.md) |

## Example prompts

- "Add a health check endpoint that verifies the database connection."
- "Split my health checks into separate liveness and readiness endpoints for Kubernetes."
- "Write a custom IHealthCheck for our downstream payments API with a timeout."
