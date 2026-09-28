# ClaimsPrincipal and ClaimsIdentity

Every authenticated request in ASP.NET Core is represented as a `ClaimsPrincipal` — a collection of
one or more `ClaimsIdentity` instances, each holding a set of `Claim` values. Authentication handlers
build this object; authorization policies and `[Authorize]` attributes read it.

## Building a ClaimsIdentity

A `Claim` is a type/value pair (plus an optional issuer). `ClaimTypes` supplies well-known claim type
constants (`ClaimTypes.Name`, `ClaimTypes.Role`, `ClaimTypes.NameIdentifier`), though nothing
requires using them over a custom string:

```csharp
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Name, user.Username),
    new(ClaimTypes.Role, "Administrator"),
    new("subscription_tier", "pro"),
};

var identity = new ClaimsIdentity(claims, authenticationType: CookieAuthenticationDefaults.AuthenticationScheme);
var principal = new ClaimsPrincipal(identity);
```

The `authenticationType` constructor argument matters: a `ClaimsIdentity` built with a null or empty
authentication type is treated as **unauthenticated** (`IsAuthenticated` returns `false`) regardless
of how many claims it carries — this is the most common cause of "I signed the user in but
`User.Identity.IsAuthenticated` is still false."

## Reading claims from HttpContext.User

Inside a request, `HttpContext.User` is the `ClaimsPrincipal` the authentication handler produced:

```csharp
app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var name = user.FindFirstValue(ClaimTypes.Name);
    var isAdmin = user.IsInRole("Administrator");
    return Results.Ok(new { name, isAdmin });
});
```

- **`FindFirstValue(type)`** — the value of the first claim of that type, or `null` if absent; use
  this over `FindFirst(type)!.Value` to avoid a null-reference exception on a missing claim.
- **`IsInRole(role)`** — checks role claims (`ClaimTypes.Role` by default) across every identity in
  the principal, not just the first.
- **`Claims`** — the flattened enumerable of every claim across every identity in the principal, for
  scanning claims that don't map to a single well-known accessor.

## Multiple identities in one principal

A `ClaimsPrincipal` can hold more than one `ClaimsIdentity` — this is how a multi-scheme setup
surfaces claims from more than one authenticated source at once (see
[multi-scheme-setups.md](multi-scheme-setups.md)). `AddIdentity(identity)` appends one; `Identity`
(singular) returns the first, while claim-lookup methods like `FindFirst`/`IsInRole` search across
all of them by default.

## Transforming claims after authentication

`IClaimsTransformation` runs after the authentication handler produces the principal but before
authorization evaluates it — the extension point for adding claims from a source outside the token
or cookie itself (a database lookup enriching the principal with a tenant ID, for example):

```csharp
public sealed class TenantClaimsTransformation(ITenantLookup tenantLookup) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated is not true)
        {
            return principal;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantId = await tenantLookup.GetTenantIdAsync(userId!);

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("tenant_id", tenantId));
        principal.AddIdentity(identity);

        return principal;
    }
}

builder.Services.AddTransient<IClaimsTransformation, TenantClaimsTransformation>();
```

`IClaimsTransformation` runs on **every** request that reaches authentication, including requests
whose principal is already fully populated — guard against re-adding the same claim repeatedly if
your implementation runs more than once per logical sign-in (e.g. once per redirect in a multi-hop
authentication flow).
