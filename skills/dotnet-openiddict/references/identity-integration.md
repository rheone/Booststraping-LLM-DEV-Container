# Integrating with ASP.NET Core Identity

OpenIddict issues and validates tokens; it has no opinion of its own on how users register, sign in
interactively, or store their password. Pairing OpenIddict with ASP.NET Core Identity as the backing
user store is the common shape for an OAuth 2.0/OIDC server that also needs to manage its own user
accounts — Identity owns the user record and password verification, OpenIddict owns the token
issuance built on top of the identity Identity's `SignInManager` already establishes.

This file covers the seam between the two libraries, not ASP.NET Core Identity's own API — its user
store configuration, password/lockout options, and `UserManager`/`SignInManager` surface are a
separate, self-contained concern with plenty of usage on their own terms.

## Sharing one Entity Framework Core DbContext

OpenIddict's `UseEntityFrameworkCore()` and ASP.NET Core Identity's `AddEntityFrameworkStores<T>()`
both attach to the same `DbContext` without conflict — each owns its own tables, and both sets of
entity configuration apply to the same context:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseOpenIddict(); // registers OpenIddict's entity sets on this context
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>());
```

`options.UseOpenIddict()` inside `AddDbContext` (a method on `DbContextOptionsBuilder`, distinct from
the `AddOpenIddict()` service-registration call) tells Entity Framework Core to include OpenIddict's
entity model when building the context's schema — omitting it means OpenIddict's tables never get
created by a migration generated from this context.

## Authenticating the user with Identity before issuing a code

Inside the authorization endpoint handler (see
[authorization-code-flow.md](authorization-code-flow.md)), use Identity's `SignInManager` to
establish who the user is, then build the OpenIddict claims identity from the result rather than
re-implementing credential checking:

```csharp
app.MapMethods("connect/authorize", ["GET", "POST"], async (
    HttpContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
    if (!result.Succeeded)
    {
        return Results.Challenge(authenticationSchemes: [IdentityConstants.ApplicationScheme]);
    }

    var user = await userManager.GetUserAsync(result.Principal!)
        ?? throw new InvalidOperationException("The user details cannot be found.");

    var identity = new ClaimsIdentity(
        TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

    identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
    identity.SetClaim(Claims.Email, await userManager.GetEmailAsync(user));
    identity.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray());

    var request = context.GetOpenIddictServerRequest()!;
    identity.SetScopes(request.GetScopes());
    identity.SetDestinations(claim => [Destinations.AccessToken]);

    return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
        authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});
```

`IdentityConstants.ApplicationScheme` is Identity's own cookie scheme, used here purely to check
whether the user already has an active Identity session — distinct from
`OpenIddictServerAspNetCoreDefaults.AuthenticationScheme`, which is the scheme the OpenIddict server
component itself uses to issue the authorization code/token.

## Mapping Identity roles into token claims

`userManager.GetRolesAsync(user)` returns Identity's own role assignments; mapping them onto
`Claims.Role` (as above) is what makes `[Authorize(Roles = "...")]` on a resource server work against
a token issued this way — the resource server's validation handler (see
[validation-configuration.md](validation-configuration.md)) surfaces whatever claims the token
carries as the `ClaimsPrincipal` on `HttpContext.User`, exactly as with any other authentication
scheme.
