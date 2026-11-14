# Cross-cutting concerns

Share the mechanism, keep the rules in the slice. A cross-cutting step knows nothing about orders or
invoices and never branches on which slice called it. Mechanisms by scope are in
[patterns.md](patterns.md#pipelines-filters-and-decorators).

## Where each concern goes

| Concern | Mechanism (framework-native first) | Slice's part |
| --- | --- | --- |
| Exception handling | `UseExceptionHandler` + `IExceptionHandler` + `AddProblemDetails` | None; see [results-and-errors.md](results-and-errors.md) |
| Validation | Built-in validation filter, validator filter, or mediator step | Its rules; see [validation.md](validation.md) |
| Authentication | Middleware | None |
| Authorization (role/policy) | `RequireAuthorization("Policy")` on the area's `MapGroup` or the endpoint; `[Authorize]` on pages/controllers | Choose the policy; `AllowAnonymous()` to opt out |
| Authorization (resource) | `IAuthorizationService.AuthorizeAsync(user, resource, policy)` inside the handler, after loading the resource | Returns a `Forbidden` outcome |
| Logging and timing | Endpoint filter or mediator step; structured logging scope with the slice name | Slice-specific log messages only |
| Tracing and metrics | One `ActivitySource` and `Meter` per app or component | Activity name and tags per slice |
| Transactions | Per-handler save, a transaction filter/step, or a unit of work; see [data-access.md](data-access.md) | Follow the repo's boundary |
| Output caching | `CacheOutput(policy)` per endpoint or group; tag eviction | Choose the policy; evict tags on writes |
| Rate limiting, CORS | Middleware + endpoint metadata (`RequireRateLimiting`, `RequireCors`) | Choose the policy |
| Idempotency | Filter or step keyed by an idempotency header; see [messaging-and-consistency.md](messaging-and-consistency.md) | Opt in for commands that need it |
| Current user, tenant, clock | Injected services (`ICurrentUser`, `ITenantContext`, `TimeProvider`) | Use them; never read `HttpContext` in the handler |

Sources: <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis#authorization>,
<https://learn.microsoft.com/aspnet/core/performance/caching/output>. Output caching caches only GET and
HEAD 200 responses by default, and skips authenticated responses.

## Authorization per slice

- Put the coarse rule on the area group: `app.MapGroup("/orders").RequireAuthorization()`.
- Put the use-case rule on the endpoint: `.RequireAuthorization("orders:cancel")`. Name policies after
  permissions, not roles.
- Put the resource rule in the handler, because only the handler has the resource:

```csharp
Order? order = await db.Orders.FindAsync([orderId], ct).ConfigureAwait(false);
if (order is null)
{
    return new OrderNotFound(orderId);
}

AuthorizationResult allowed = await authorization.AuthorizeAsync(user.Principal, order, "OrderOwner")
    .ConfigureAwait(false);
if (!allowed.Succeeded)
{
    return new CancelForbidden(orderId);
}
```

- Every slice has a test for its unauthorized and forbidden paths.
- Message consumers and jobs run as the system. Record the original user on the message when an audit
  trail needs it; do not re-run user policies.

## Observability per slice

```csharp
internal static class AppTelemetry
{
    public const string Name = "MyApp";
    public static readonly ActivitySource Source = new(Name);
    public static readonly Meter Meter = new(Name);
}

// In an endpoint filter or mediator step, generic over requests:
using Activity? activity = AppTelemetry.Source.StartActivity($"slice {sliceName}");
activity?.SetTag("app.slice", sliceName);
activity?.SetTag("app.area", areaName);
```

- Create sources and meters once, statically, and register them with `AddSource(AppTelemetry.Name)` /
  `AddMeter(AppTelemetry.Name)`
  (<https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs>).
- Derive the slice name once (the endpoint name from `.WithName(...)`, or the request type name) so logs,
  traces, and metrics share one key. Per-slice naming is community practice.
- Add a logging scope with the slice name so every log line inside the handler carries it.
- Keep tag values bounded (slice names, outcome case names). Never tag with IDs or user input.

## Feature flags

A flag on a whole slice belongs at the edge: `.AddEndpointFilter<FeatureGateFilter>()` or the flag
library's endpoint extension, returning 404 when off. A flag inside a slice that switches between two
behaviors is a temporary second slice; remove the losing branch when the flag retires.

## Ownership

Feature folders map to owners. Add `CODEOWNERS` entries per area (`/src/MyApp/Features/Billing/
@billing-team`) when the repo uses code owners. Shared infrastructure and the domain need an explicit owner
too, because every team changes them.

## Boundary test

Before adding a step to shared infrastructure, delete every caller but one in your head. If the step
still makes sense unchanged, it is cross-cutting. If it branches on a request type or a domain state, it
belongs in a slice.
