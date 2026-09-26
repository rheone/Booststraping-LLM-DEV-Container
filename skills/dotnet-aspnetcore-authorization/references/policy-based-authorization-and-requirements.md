# Policy-Based Authorization and Requirements

A policy is a named set of one or more requirements, registered once and referenced by name from
`[Authorize(Policy = "...")]` or `RequireAuthorization("...")`. Role-based and claims-based checks
(`RequireRole`, `RequireClaim`) are themselves shorthand for adding a pre-built requirement to a
policy — everything in this file is the general mechanism underneath both.

## Registering policies

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AtLeast21", policy => policy.Requirements.Add(new MinimumAgeRequirement(21)));
```

`AddAuthorizationBuilder()` (available since ASP.NET Core 7.0) registers the authorization services
and returns a fluent builder for named policies in one call, replacing the older
`AddAuthorization(options => options.AddPolicy(...))` form. Apply a registered policy from an
endpoint with `RequireAuthorization("AtLeast21")`, or from an MVC/Razor Pages/Blazor `[Authorize]`
attribute with `Policy = "AtLeast21"`.

## Requirements

A requirement implements `IAuthorizationRequirement`, an empty marker interface — it carries
whatever data a handler needs to make its decision, or nothing at all if the handler's entire logic
runs off the current user/resource with no parameters:

```csharp
public sealed class MinimumAgeRequirement(int minimumAge) : IAuthorizationRequirement
{
    public int MinimumAge { get; } = minimumAge;
}
```

## Handlers

A handler evaluates a requirement against an `AuthorizationHandlerContext`. Inheriting
`AuthorizationHandler<TRequirement>` handles exactly one requirement type:

```csharp
public sealed class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var dobClaim = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth);
        if (dobClaim is not null &&
            DateTime.Parse(dobClaim.Value) <= DateTime.UtcNow.AddYears(-requirement.MinimumAge))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
```

A handler must be registered in DI (any lifetime) to ever run — a requirement added to a policy with
no matching registered handler never succeeds, since nothing ever calls `context.Succeed(...)` for
it. This fails *silently*, not at startup: the policy simply rejects every evaluation.

### A handler for more than one requirement type

Implementing `IAuthorizationHandler` directly (instead of the generic base class) lets one handler
evaluate several unrelated requirement types by inspecting `context.PendingRequirements`:

```csharp
public sealed class PermissionHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.ToList())
        {
            if (requirement is ReadPermission && (IsOwner(context.User, context.Resource) || IsSponsor(context.User, context.Resource)))
            {
                context.Succeed(requirement);
            }
            else if (requirement is EditPermission or DeletePermission && IsOwner(context.User, context.Resource))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
```

## What a handler returns, and how success/failure actually propagate

`HandleRequirementAsync`/`HandleAsync` returns no success/failure value directly:

- Call `context.Succeed(requirement)` to mark that specific requirement satisfied.
- A handler that finds the requirement *not* met simply doesn't call `Succeed` — it isn't required
  to call a "fail" method, because another handler for the same requirement might still succeed.
- Call `context.Fail()` to force the whole evaluation to fail even if other handlers later succeed
  on the same requirement.
- All registered handlers still run even after `Succeed`/`Fail` is called, unless
  `AuthorizationOptions.InvokeHandlersAfterFailure` is set to `false` (it defaults to `true`) — this
  lets a requirement's side effects (logging, auditing) run regardless of which handler decided the
  outcome. Handlers can execute in any order — never depend on registration order between them.

## AND vs. OR: two different multiplicities

- **Multiple requirements in one policy = AND.** Every requirement added to a policy must succeed
  for the policy to succeed.
- **Multiple handlers for one requirement = OR.** When more than one handler is registered for the
  same requirement type, the requirement succeeds as soon as *any* of them calls `Succeed` — useful
  when a single logical rule ("may enter the building") has more than one way to satisfy it (a
  badge, or a temporary sticker from reception):

```csharp
public sealed class BuildingEntryRequirement : IAuthorizationRequirement { }

public sealed class BadgeEntryHandler : AuthorizationHandler<BuildingEntryRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BuildingEntryRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == "BadgeId")) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

public sealed class TemporaryStickerHandler : AuthorizationHandler<BuildingEntryRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BuildingEntryRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == "TemporaryBadgeId")) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

Register both handlers; the policy succeeds if either one calls `Succeed`.

## Expressing a policy inline with `RequireAssertion`

For logic simple enough not to justify a dedicated requirement/handler pair:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("BadgeEntry", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c => c.Type == "BadgeId" || c.Type == "TemporaryBadgeId")));
```

## Bundling a requirement and its handler into one class

A type implementing both `IAuthorizationRequirement` and `IAuthorizationHandler` doesn't need
separate DI registration — the framework's built-in `PassThroughAuthorizationHandler` recognizes
self-handling requirements and invokes them directly. `AssertionRequirement` (in
`Microsoft.AspNetCore.Authorization.Infrastructure`) is the framework's own example of this shape,
used internally by `RequireAssertion`. Reserve this bundling for genuinely simple, self-contained
requirements — it tightly couples the requirement to one specific handling strategy, which a
separate requirement/handler pair avoids.
