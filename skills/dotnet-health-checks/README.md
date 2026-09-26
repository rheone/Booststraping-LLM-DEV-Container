# ASP.NET Core Health Checks

Guidance on `Microsoft.Extensions.Diagnostics.HealthChecks` — the routing table (by situation) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per concern

| File | Covers |
| --- | --- |
| `core-concepts.md` | `IHealthCheck`, `HealthCheckResult`, `AddHealthChecks`/`AddCheck`/`AddCheck<T>` registration |
| `endpoint-configuration.md` | `MapHealthChecks`, `HealthCheckOptions` (status codes, response writer, caching headers), restricting endpoint access |
| `tags-and-filtering.md` | tagging checks, `HealthCheckOptions.Predicate`, separate readiness/liveness endpoints, startup-gated readiness |
| `external-dependency-checks.md` | the generic registration pattern for a dependency check, timeouts, `failureStatus`, the community-package ecosystem |
| `orchestrator-integration.md` | what liveness vs. readiness means to a container orchestrator, and the consequence of mixing them up |
| `testing.md` | unit-testing an `IHealthCheck`, integration-testing the mapped endpoint, testing a custom response writer |

## Scope

`Microsoft.Extensions.Diagnostics.HealthChecks` (current stable release **10.0.12**, shipping
alongside **.NET 10**, part of the [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore)
repository). Covers `IHealthCheck` implementation, registration, endpoint mapping and response
customization, tag-based filtering for readiness/liveness separation, the generic pattern for
external-dependency checks, the community-package ecosystem at a generic level, orchestrator
integration concepts, and testing.

Out of scope: Application Performance Monitoring / distributed tracing, and any specific
community-maintained check package's own configuration API. See [SKILL.md](SKILL.md) for the full
out-of-scope list and rationale.

This skill is self-contained: it does not assume any other skill is installed.
