---
name: dotnet-aspnetcore-authentication
description: Guidance on ASP.NET Core's authentication and authorization middleware (part of the shared framework, verified current against ASP.NET Core 10.0) — the AuthenticationScheme model and AddAuthentication/AddCookie/AddJwtBearer setup, [Authorize]/[AllowAnonymous] attributes and RequireAuthorization()/AllowAnonymous() for minimal APIs, policy-based authorization with AddAuthorizationBuilder/RequireClaim/RequireRole and custom IAuthorizationRequirement handlers, ClaimsPrincipal/ClaimsIdentity construction and IClaimsTransformation, writing a custom AuthenticationHandler<TOptions>, and multi-scheme setups combining cookie and JWT authentication in one app. Use when wiring up authentication/authorization in an ASP.NET Core app, debugging a 401 vs. 403 response, choosing between cookie and JWT bearer schemes, writing a custom authentication handler, or defining a policy with claim/role requirements.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# ASP.NET Core Authentication

Guidance on ASP.NET Core's own authentication and authorization abstractions and middleware — the
scheme model, built-in cookie and JWT bearer handlers, `[Authorize]`/policy-based authorization, and
the `ClaimsPrincipal`/`ClaimsIdentity` types they all operate on. Organized by task, not by ASP.NET
Core version — the scheme/policy model has been stable across recent releases; each reference file
notes a version-specific fact inline where one applies.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Registering `AddAuthentication`, understanding scheme selection and default-scheme properties | [references/authentication-schemes.md](references/authentication-schemes.md) |
| Setting up cookie-based sign-in for a browser-facing app | [references/cookie-authentication.md](references/cookie-authentication.md) |
| Setting up JWT bearer validation for an API | [references/jwt-bearer-authentication.md](references/jwt-bearer-authentication.md) |
| Building or reading `ClaimsPrincipal`/`ClaimsIdentity`, or transforming claims post-authentication | [references/claims-and-principals.md](references/claims-and-principals.md) |
| Applying `[Authorize]`/`[AllowAnonymous]`, or `RequireAuthorization()`/`AllowAnonymous()` on minimal API endpoints | [references/authorization-attributes.md](references/authorization-attributes.md) |
| Defining a named policy with `AddAuthorizationBuilder`, `RequireClaim`/`RequireRole`, or a custom requirement handler | [references/policy-based-authorization.md](references/policy-based-authorization.md) |
| Writing a custom `AuthenticationHandler<TOptions>` for a credential type with no built-in scheme | [references/custom-authentication-handlers.md](references/custom-authentication-handlers.md) |
| Combining cookie and JWT (or any two schemes) in one application | [references/multi-scheme-setups.md](references/multi-scheme-setups.md) |
| Testing authorization policies, requirement handlers, or `[Authorize]`-guarded endpoints | [references/testing.md](references/testing.md) |

## Quick start

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://issuer.example.com";
        options.Audience = "my-api";
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdministrator", policy => policy.RequireRole("Administrator"));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/admin/report", () => Results.Ok())
    .RequireAuthorization("RequireAdministrator");
```

The single most common miss: registering a scheme (`AddJwtBearer`, `AddCookie`) without ever
referencing it from `[Authorize]`, a policy, or `RequireAuthorization()` — a registered scheme
authenticates a request when asked to, but nothing *requires* authentication on any given endpoint
until something asks. See [references/authorization-attributes.md](references/authorization-attributes.md).

## Out of scope

- Any specific external identity provider or identity-server product — this skill covers ASP.NET
  Core's own scheme/handler/policy abstractions, which any provider integration sits on top of, not
  a particular provider's configuration surface.
- User account storage and management (registration, password hashing, user stores) — orthogonal to
  the authentication middleware and claims model covered here.
