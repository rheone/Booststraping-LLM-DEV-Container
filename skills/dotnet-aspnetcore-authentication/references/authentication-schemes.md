# Authentication Schemes

ASP.NET Core's authentication system is built around **schemes** — named, independently configured
authentication handlers. A request can be authenticated by more than one scheme at once (see
[multi-scheme-setups.md](multi-scheme-setups.md)); every piece of authentication configuration
attaches to a scheme name, not to the application globally.

## Registering the authentication services

`AddAuthentication` registers the core authentication services and, optionally, sets the default
scheme(s) used when a request doesn't specify one explicitly:

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies");
```

`AddAuthentication(...)` returns an `AuthenticationBuilder`; every `Add<Scheme>` call
(`AddCookie`, `AddJwtBearer`, and so on) chains off it and registers one named scheme.

## The default scheme properties

`AuthenticationOptions` exposes several default-scheme properties, each governing a different
operation when no explicit scheme is specified at the call site:

| Property | Governs |
| --- | --- |
| `DefaultAuthenticateScheme` | Which scheme's handler runs to construct the `ClaimsPrincipal` for `HttpContext.User` |
| `DefaultChallengeScheme` | Which scheme handles a 401 challenge (redirecting to a login page, or returning a `WWW-Authenticate` header) |
| `DefaultForbidScheme` | Which scheme handles a 403 forbid response |
| `DefaultSignInScheme` / `DefaultSignOutScheme` | Which scheme handles `SignInAsync`/`SignOutAsync` when no scheme is passed explicitly |
| `DefaultScheme` | A single fallback used for any of the above not set individually |

With only one scheme registered, `DefaultScheme` alone is enough — the framework uses it for every
operation. With more than one scheme, set the specific defaults explicitly rather than relying on
whichever scheme happened to register itself as available first.

## Middleware ordering

`app.UseAuthentication()` must run before `app.UseAuthorization()`, and both must run after routing
is established (`UseRouting()` for the classic middleware pipeline; implicit in minimal hosting)
and before endpoints execute:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // or app.MapGet(...), etc.
```

`UseAuthentication` populates `HttpContext.User` by running the default-scheme handler; a request
reaching an endpoint before this middleware runs sees an unauthenticated `ClaimsPrincipal`
regardless of any credential the request actually carries.

## Selecting a scheme per call

Any authentication operation can target a specific scheme by name instead of the default, when more
than one scheme is registered:

```csharp
await context.ChallengeAsync("Jwt");
var result = await context.AuthenticateAsync("Cookies");
```

`[Authorize(AuthenticationSchemes = "Jwt")]` similarly pins an endpoint to a specific scheme rather
than whatever the default happens to be — see
[authorization-attributes.md](authorization-attributes.md).
