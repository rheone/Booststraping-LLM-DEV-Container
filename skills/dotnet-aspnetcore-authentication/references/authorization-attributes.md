# [Authorize] and [AllowAnonymous]

`[Authorize]` enforces that a request reaching the decorated endpoint carries an authenticated (and,
optionally, policy-satisfying) `ClaimsPrincipal`; `[AllowAnonymous]` carves out an exception. Both
attributes apply to controllers/actions in a controller-based API and, through
`RequireAuthorization()`/`AllowAnonymous()`, to minimal API endpoints and route groups.

## Basic enforcement

```csharp
[Authorize]
public sealed class OrdersController : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(GetOrdersForCurrentUser());

    [AllowAnonymous]
    [HttpGet("public-summary")]
    public IActionResult PublicSummary() => Ok(GetPublicSummary());
}
```

`[Authorize]` on a class applies to every action in it; `[AllowAnonymous]` on a specific action
overrides that for just that action. `[AllowAnonymous]` always wins over any `[Authorize]` at any
level (class, or a base class) — there's no way to require authorization on an action explicitly
marked anonymous.

For minimal APIs, the equivalent is `RequireAuthorization()`/`AllowAnonymous()` chained off the
endpoint or group builder:

```csharp
app.MapGet("/orders", GetOrders).RequireAuthorization();
app.MapGet("/orders/public-summary", GetPublicSummary).AllowAnonymous();
```

## Pinning to a scheme or policy

`[Authorize]` accepts named parameters to narrow what it enforces beyond "any authenticated user":

```csharp
[Authorize(AuthenticationSchemes = "Jwt")]
[Authorize(Policy = "RequireAdministrator")]
[Authorize(Roles = "Administrator,Manager")]
```

- **`AuthenticationSchemes`** — restrict which scheme(s) can satisfy this endpoint, in a multi-scheme
  setup (see [multi-scheme-setups.md](multi-scheme-setups.md)).
- **`Policy`** — require a named policy registered via `AddAuthorizationBuilder` (see
  [policy-based-authorization.md](policy-based-authorization.md)); this is the mechanism for any
  requirement beyond a simple role check.
- **`Roles`** — a comma-separated list acts as OR (any one role listed is sufficient); stacking
  multiple `[Authorize(Roles = "...")]` attributes on the same target acts as AND (each attribute
  must independently be satisfied).

## Stacking multiple [Authorize] attributes

```csharp
[Authorize(Roles = "Manager")]
[Authorize(Policy = "ActiveSubscription")]
public IActionResult RestrictedReport() => Ok(...);
```

Both requirements must pass — multiple `[Authorize]` attributes on the same target combine with AND
semantics, unlike the comma-separated roles within a single attribute's `Roles` property, which
combine with OR.

## What happens when [Authorize] fails

An unauthenticated request (no valid principal at all) fails as a **401 Challenge** — routed to
whichever scheme's handler is responsible for challenging (`DefaultChallengeScheme`, or the scheme
named on the attribute). An authenticated request that fails a role/policy check fails as a **403
Forbid** instead — the framework already knows who the caller is, it's just refusing them, so it
skips the challenge and goes straight to a forbid response.
