# Claims-Based Authorization

A claim is a name/value pair describing what the subject *is* (an employee number, a department, a
date of birth), not what it can do — authorization turns a claim into a decision by checking for its
presence, a specific value, or a computed condition over it.

## Presence-only claim check

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EmployeeOnly", policy => policy.RequireClaim("EmployeeNumber"));
```

```csharp
[Authorize(Policy = "EmployeeOnly")]
public IActionResult VacationBalance() => View();
```

`RequireClaim("EmployeeNumber")` with no values passed only checks that the claim exists on the
principal, regardless of its value.

## Value-restricted claim check

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Founders", policy =>
        policy.RequireClaim("EmployeeNumber", "1", "2", "3", "4", "5"));
```

Listing allowed values is an OR — the claim's value must match *any one* of them.

## Requiring several claims at once

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CustomerServiceMember", policy => policy.RequireClaim("Department", "Customer Service"))
    .AddPolicy("HumanResourcesMember", policy => policy.RequireClaim("Department", "Human Resources"));
```

```csharp
[Authorize(Policy = "CustomerServiceMember")]
[Authorize(Policy = "HumanResourcesMember")]
public IActionResult Restricted() => View();
```

Stacking two `[Authorize(Policy = "...")]` attributes is an AND — both policies must pass, same as
stacking `[Authorize(Roles = "...")]` attributes.

## Claim logic beyond presence/value matching

Pattern matching, checking the claim's issuer, or parsing a complex value (a date-of-birth string
converted to an age) needs `RequireAssertion` with `ClaimsPrincipal.HasClaim`, not `RequireClaim`:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ContosoOnly", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c =>
            c.Type == "email" &&
            c.Value.EndsWith("@contoso.com", StringComparison.OrdinalIgnoreCase))));
```

For a check with a parameter (a minimum age computed from a birth-date claim), a custom requirement
and handler is the better fit than `RequireAssertion` — see
[policy-based-authorization-and-requirements.md](policy-based-authorization-and-requirements.md). Use
`RequireAssertion` for logic simple enough to express inline; reach for a full requirement/handler
pair once the logic needs injected dependencies or gets reused across several policies.

## Claim case sensitivity

Claim *values* are always compared with `StringComparison.Ordinal` — `Admin` and `admin` are
different values regardless of issuer. Claim *type* comparison (matching `"EmployeeNumber"` against
whatever claim type string the identity actually carries) may be case-sensitive or
case-insensitive depending on the `ClaimsIdentity` implementation that created it: tokens validated
through `Microsoft.IdentityModel` (used by `AddJwtBearer`, `AddOpenIdConnect`, `AddWsFederation`, and
Microsoft Identity Web) produce a `CaseSensitiveClaimsIdentity` as of ASP.NET Core 8.0, while the
.NET runtime's default `ClaimsIdentity` (used by cookie-based flows) still matches claim types
case-insensitively. Use consistent casing for claim type strings throughout an app to avoid this
distinction mattering in practice.
