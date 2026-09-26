# Policy-Based Authorization

A policy is a named, reusable set of requirements — evaluated together with AND semantics — that
`[Authorize(Policy = "...")]` or `RequireAuthorization("...")` can reference by name instead of
inlining role/claim checks at every call site.

## Registering policies with AddAuthorizationBuilder

`AddAuthorizationBuilder()` (registered once, typically in `Program.cs`) returns a fluent builder for
defining named policies:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdministrator", policy => policy.RequireRole("Administrator"))
    .AddPolicy("ActiveSubscription", policy => policy
        .RequireClaim("subscription_status", "active")
        .RequireAuthenticatedUser())
    .AddPolicy("SeniorManager", policy => policy
        .RequireRole("Manager")
        .RequireClaim("seniority", "senior"));
```

Every requirement added to a policy builder combines with AND — `SeniorManager` above requires both
the role and the claim, not either.

## RequireClaim and RequireRole

- **`RequireClaim(type)`** — the principal must carry at least one claim of the given type, with any
  value.
- **`RequireClaim(type, allowedValues)`** — the claim must carry one of the specific listed values;
  multiple allowed values act as OR within that single `RequireClaim` call.
- **`RequireRole(roles)`** — equivalent to `RequireClaim(ClaimTypes.Role, roles)` by default; multiple
  role arguments in one call act as OR (any one is sufficient).

```csharp
.AddPolicy("EditorOrAdmin", policy => policy.RequireRole("Editor", "Administrator"))
```

## Custom requirements and handlers

When a check can't be expressed as a claim/role comparison — comparing a resource's owner ID against
the current user, checking a value against another service — implement `IAuthorizationRequirement`
and a matching `AuthorizationHandler<TRequirement>`:

```csharp
public sealed class MinimumAgeRequirement(int minimumAge) : IAuthorizationRequirement
{
    public int MinimumAge { get; } = minimumAge;
}

public sealed class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var dobClaim = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth);
        if (dobClaim is not null && DateTime.Parse(dobClaim.Value) <= DateTime.UtcNow.AddYears(-requirement.MinimumAge))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Over18", policy => policy.Requirements.Add(new MinimumAgeRequirement(18)));
```

Register every custom handler in DI — a requirement added to a policy with no matching registered
handler never succeeds, since nothing ever calls `context.Succeed(...)` for it; the policy silently
fails every evaluation rather than throwing at startup.

## Resource-based authorization

For a check that depends on the specific resource being accessed (its owner, its state), call
`IAuthorizationService.AuthorizeAsync` explicitly inside the handler/endpoint rather than relying on
attribute-level policy evaluation, which runs before the resource is loaded:

```csharp
app.MapGet("/orders/{id}", async (Guid id, ClaimsPrincipal user, IAuthorizationService authorizationService, IOrderRepository repository) =>
{
    var order = await repository.FindAsync(id);
    if (order is null)
    {
        return Results.NotFound();
    }

    var result = await authorizationService.AuthorizeAsync(user, order, "OrderOwnerPolicy");
    return result.Succeeded ? Results.Ok(order) : Results.Forbid();
});
```

A resource-based policy's handler receives the resource as `context.Resource`, letting requirement
logic compare it against the current principal directly.
