# Imperative and Resource-Based Authorization

`[Authorize]` and its policy/role variants evaluate *before* a request's parameters are bound and
before any code has loaded the actual resource being acted on — an attribute alone can't decide "is
this user allowed to edit *this specific* document," only "is this user allowed to hit this
endpoint at all." When the decision genuinely depends on the resource's own data (its owner, its
state), call the authorization service explicitly from inside the handler, after the resource is
loaded — this is **imperative authorization**, and doing it against a specific object is
**resource-based authorization**.

## `IAuthorizationService`

Registered by the framework and available through DI, `IAuthorizationService` has two
`AuthorizeAsync` overloads:

```csharp
Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName);
Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements);
```

Pass `null` for `resource` when a resource isn't needed for the evaluation — this is exactly what
`[Authorize(Policy = "...")]` does internally, minus the resource.

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

## Writing a resource-based handler

A resource-based handler is an `AuthorizationHandler<TRequirement, TResource>` — the resource
argument's type is checked, so a handler only ever runs for a matching resource type:

```csharp
public sealed class SameAuthorRequirement : IAuthorizationRequirement { }

public sealed class DocumentAuthorizationHandler : AuthorizationHandler<SameAuthorRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SameAuthorRequirement requirement, Document resource)
    {
        if (context.User.Identity?.Name == resource.Author)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SameAuthorPolicy", policy => policy.Requirements.Add(new SameAuthorRequirement()));

builder.Services.AddSingleton<IAuthorizationHandler, DocumentAuthorizationHandler>();
```

## `OperationAuthorizationRequirement` for CRUD-shaped checks

Writing a separate requirement type for Create/Read/Update/Delete gets repetitive — the built-in
`OperationAuthorizationRequirement` (`Microsoft.AspNetCore.Authorization.Infrastructure`) carries a
`Name` so one handler can branch on which operation is being checked:

```csharp
public static class Operations
{
    public static readonly OperationAuthorizationRequirement Create = new() { Name = nameof(Create) };
    public static readonly OperationAuthorizationRequirement Read = new() { Name = nameof(Read) };
    public static readonly OperationAuthorizationRequirement Update = new() { Name = nameof(Update) };
    public static readonly OperationAuthorizationRequirement Delete = new() { Name = nameof(Delete) };
}

public sealed class DocumentAuthorizationCrudHandler : AuthorizationHandler<OperationAuthorizationRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Document resource)
    {
        if (requirement.Name == Operations.Read.Name) context.Succeed(requirement);
        if (requirement.Name == Operations.Create.Name && context.User.IsInRole("Admin")) context.Succeed(requirement);
        if (requirement.Name == Operations.Update.Name && context.User.IsInRole("Admin")) context.Succeed(requirement);
        if (requirement.Name == Operations.Delete.Name && context.User.IsInRole("SuperUser")) context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

```csharp
var result = await authorizationService.AuthorizeAsync(user, document, Operations.Delete);
```

## Calling `IAuthorizationService` from an MVC view

Inject `IAuthorizationService` into a Razor view (directly, or globally via `_ViewImports.cshtml`) to
conditionally render UI based on the same policy/resource check used server-side:

```razor
@inject IAuthorizationService AuthService

@if ((await AuthService.AuthorizeAsync(User, Model, Operations.Update)).Succeeded)
{
    <a class="btn" href="@Url.Action("Edit", "Document", new { id = Model.Id })">Edit</a>
}
```

Never treat hiding a UI element as the sole authorization check — a user who knows the underlying
route can still request it directly. The action method behind that link needs its own
`AuthorizeAsync` (or attribute) check regardless of whether the view renders the link.
