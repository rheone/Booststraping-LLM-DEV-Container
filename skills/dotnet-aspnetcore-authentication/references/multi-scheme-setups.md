# Multi-Scheme Setups

An application can register more than one authentication scheme at once — the common shape is a
cookie scheme for a browser-facing UI and a JWT bearer scheme for an API surface served from the same
host — and route each request to the right one.

## Registering both schemes

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/account/login";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Authority = "https://issuer.example.com";
    options.Audience = "my-api";
});
```

Setting a single default scheme here means `[Authorize]` with no `AuthenticationSchemes` specified
authenticates against the cookie scheme only — API endpoints under `/api` need to opt into the JWT
scheme explicitly.

## Pinning routes to a scheme

```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase { }

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public sealed class AccountController : Controller { }
```

For minimal APIs, group the endpoints and apply the scheme once per group rather than per endpoint:

```csharp
var api = app.MapGroup("/api").RequireAuthorization(policy =>
    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser());
```

## Accepting either scheme on the same endpoint

`[Authorize(AuthenticationSchemes = "Cookies,Jwt")]` (comma-separated) accepts a principal
authenticated by **either** scheme — useful for an endpoint reachable from both the browser UI and
external API clients:

```csharp
[Authorize(AuthenticationSchemes = "Cookies,Jwt")]
public IActionResult SharedEndpoint() => Ok(...);
```

The framework tries each listed scheme's `AuthenticateAsync` in turn and succeeds if any one
produces an authenticated principal.

## Making challenge behavior match the client

The biggest correctness risk in a multi-scheme setup is a challenge response shaped for the wrong
client — a JWT client receiving an HTML login-page redirect instead of a plain 401, or a browser
request receiving a bare 401 with no redirect. Setting `DefaultChallengeScheme` correctly per route
(via the `[Authorize(AuthenticationSchemes = ...)]`/`RequireAuthorization` pinning above) is what
determines which scheme's challenge behavior actually runs — see
[cookie-authentication.md](cookie-authentication.md) for overriding a cookie scheme's redirect
behavior specifically for API-shaped requests within the same scheme, when routes can't be cleanly
separated by scheme alone.
