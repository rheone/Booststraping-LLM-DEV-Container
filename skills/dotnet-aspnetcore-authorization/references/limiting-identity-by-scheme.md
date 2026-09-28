# Limiting Identity by Scheme

An app with more than one authentication handler registered (cookies for browser sign-in, JWT bearer
for API calls) needs a way to say which scheme's identity a given check should evaluate against —
otherwise "the current user" is ambiguous whenever more than one scheme could have produced a
principal for the same request.

## Restricting an `[Authorize]` check to specific schemes

```csharp
[Authorize(AuthenticationSchemes =
    CookieAuthenticationDefaults.AuthenticationScheme + "," +
    JwtBearerDefaults.AuthenticationScheme)]
public class MixedAuthSchemesController : Controller { }
```

```csharp
app.MapGet("/api/data", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] () => "...");
```

Listing multiple schemes in `AuthenticationSchemes` is a union — authorization succeeds if *any*
listed scheme authenticates the request; if more than one does, the resulting identities are merged
into a single `ClaimsPrincipal` for the request. Listing exactly one scheme runs only that scheme's
handler and ignores identities any other scheme might otherwise have produced for the same request.

## Restricting a named policy to specific schemes

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Over18", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new MinimumAgeRequirement(18));
    });
```

Setting `AuthorizationPolicyBuilder.AuthenticationSchemes` scopes the policy's evaluation to that
scheme's identity specifically, regardless of which scheme(s) an `[Authorize]` attribute elsewhere on
the same endpoint might also list — when both an attribute and its policy specify schemes, the
final set is the union of both.

## `[Authorize]` attribute and policy scheme interaction

When an endpoint has both attribute-level `AuthenticationSchemes` and a policy with its own
`AuthenticationSchemes`, the two combine into a union — any listed scheme may authenticate the
request. A cookie scheme added by the attribute alongside a policy restricted to bearer
authentication still allows a cookie-only request through, provided the resulting principal
satisfies the policy's actual requirements.

## Supporting multiple identity providers of the same scheme type

Registering a second JWT bearer handler needs an explicit, unique scheme name — only one handler can
use the default scheme name:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { options.Authority = "https://issuer-a.example.com/"; })
    .AddJwtBearer("MEID", options => { options.Authority = "https://sts.windows.net/{tenant}/"; });
```

Update the default policy to accept both schemes so `[Authorize]` with no explicit scheme still
works against either issuer:

```csharp
var policyBuilder = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme, "MEID")
    .RequireAuthenticatedUser();

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(policyBuilder.Build());
```

## Choosing the scheme dynamically per request

Deciding which scheme should even run, per request (rather than fixing a static list at the policy
or attribute), is a job for `AddPolicyScheme` with `ForwardDefaultSelector` at the *authentication*
layer, evaluated before any authorization policy ever runs — out of scope here since it configures
which handler authenticates the request in the first place, not which already-authenticated identity
a policy evaluates.
