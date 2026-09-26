# Role-Based Authorization

A role is exposed to the authorization system as a claim — `ClaimsPrincipal.IsInRole` checks for a
role claim on the current identity. Roles are claims, but not every claim is a role: a role
typically represents group membership assigned by the identity issuer, while a claim in general is
meant to describe an individual user's own attributes.

## Declarative role checks

```csharp
[Authorize(Roles = "Admin, Superuser")]
public IActionResult Dashboard() => View();
```

A comma-separated `Roles` list is an OR — the user needs *any one* of the listed roles. To require
*every* one of several roles, stack multiple `[Authorize(Roles = "...")]` attributes (each one an
AND against the others):

```csharp
[Authorize(Roles = "Admin")]
[Authorize(Roles = "SuperUser")]
public IActionResult RequiresBothRoles() => View();
```

## Checking a role imperatively

```csharp
if (User.IsInRole("Administrator"))
{
    // ...
}
```

## Role requirements expressed as a policy

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("ElevatedRights", policy => policy.RequireRole("Admin", "SuperUser"));
```

`RequireRole("Admin", "SuperUser")` in a single call is OR — any one of the listed roles satisfies
it. To require multiple roles simultaneously inside one policy, chain `RequireRole` calls (each
adds its own AND'd requirement):

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ElevatedRights", policy => policy
        .RequireRole("Admin")
        .RequireRole("SuperUser"));
```

Reach for a named policy over a bare `Roles` list once a check needs to combine a role with a claim
or a custom requirement — `Roles` alone can only ever express "one of these roles."

## Case sensitivity

Role *names* are compared with ordinal string comparison — `Admin` and `admin` are different roles,
regardless of which authentication handler produced the identity. The role claim *type* itself
(the claim type used to locate role claims, not the role name) may be compared case-sensitively or
case-insensitively depending on which `ClaimsIdentity` implementation issued it; this distinction
rarely matters in practice as long as role names and claim types are used consistently throughout an
app.

## Setting up Identity's role support

Role-based authorization only has role claims to check once the identity system is configured to
produce them. With ASP.NET Core Identity:

```csharp
builder.Services.AddDefaultIdentity<IdentityUser>(/* ... */)
    .AddRoles<IdentityRole>();
```

Or, building Identity manually:

```csharp
builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>();
```

For JWT-bearer-authenticated APIs, role claims typically arrive already populated in the token
itself — the JWT bearer handler surfaces role claims found in the token directly onto the resulting
`ClaimsPrincipal`, with no separate `AddRoles` call needed on the API side.
