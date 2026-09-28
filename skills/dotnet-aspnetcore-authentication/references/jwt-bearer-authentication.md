# JWT Bearer Authentication

`AddJwtBearer` (from the `Microsoft.AspNetCore.Authentication.JwtBearer` package, versioned
alongside the shared ASP.NET Core framework) registers a scheme that validates a signed JWT
presented in the `Authorization: Bearer <token>` header — the standard approach for API clients that
authenticate against an external or self-hosted token issuer rather than holding a browser session.

## Registration

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://issuer.example.com";
        options.Audience = "my-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });
```

- **`Authority`** — the issuer's base URL; when set, the handler fetches the issuer's OpenID Connect
  discovery document and signing keys automatically rather than requiring you to configure them by
  hand.
- **`TokenValidationParameters`** — governs what the handler checks on every incoming token.
  `ValidateIssuer`/`ValidateAudience` should stay `true` in essentially every real deployment;
  turning them off accepts tokens meant for a different issuer or a different API.
- **`ClockSkew`** defaults to five minutes; tightening it reduces the window in which an expired
  token near the boundary is still accepted, at the cost of being less forgiving of clock drift
  between the issuer and this API.

## Validating without an Authority (fixed signing key)

When the issuer doesn't expose OpenID Connect discovery (an internal, hand-rolled token issuer),
supply the signing key directly instead of setting `Authority`:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "my-issuer",
    ValidateAudience = true,
    ValidAudience = "my-api",
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeySecret)),
};
```

## Reacting to validation events

`JwtBearerOptions.Events` exposes hooks into the validation pipeline — most commonly used to
translate a validation failure into a custom response shape, or to pull additional claims from a
source the token itself doesn't carry:

```csharp
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context =>
    {
        context.NoResult();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync("""{"error":"invalid_token"}""");
    },
};
```

## Common pitfall: forgetting UseAuthorization or endpoint-level [Authorize]

Registering `AddJwtBearer` alone does not protect any endpoint — the handler only runs to *populate*
`HttpContext.User` when the scheme is invoked; it's `[Authorize]` (or `RequireAuthorization()` on a
minimal API endpoint/group) that actually enforces authentication on a given route. A JWT scheme
configured but never referenced by an `[Authorize]` attribute or policy silently leaves every
endpoint open. See [authorization-attributes.md](authorization-attributes.md).
