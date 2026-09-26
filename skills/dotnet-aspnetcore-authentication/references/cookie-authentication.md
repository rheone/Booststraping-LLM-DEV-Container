# Cookie Authentication

`AddCookie` registers a scheme backed by an encrypted authentication cookie — the standard approach
for browser-based applications where the server issues a session after a successful sign-in and the
browser presents that cookie automatically on subsequent requests.

## Registration

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
```

- **`LoginPath`** — where an unauthenticated request challenging this scheme redirects to (a 401
  challenge on a cookie scheme becomes a 302 redirect, since the browser can't otherwise present a
  credential in response to a bare 401).
- **`ExpireTimeSpan`** / **`SlidingExpiration`** — how long the cookie remains valid and whether each
  request that arrives more than halfway through the expiration window resets the clock.
- **`Cookie.SecurePolicy`** — `CookieSecurePolicy.Always` requires HTTPS for the cookie to be sent;
  set this in production regardless of `SameSite` value, since a cookie without `Secure` is
  interceptable over plain HTTP.

## Signing in and out

`SignInAsync` builds the encrypted cookie from a `ClaimsPrincipal` you construct; `SignOutAsync`
clears it:

```csharp
app.MapPost("/account/login", async (HttpContext context, LoginRequest request) =>
{
    // validate request.Username/request.Password against your store...
    var claims = new[] { new Claim(ClaimTypes.Name, request.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Ok();
});

app.MapPost("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});
```

See [claims-and-principals.md](claims-and-principals.md) for building the `ClaimsIdentity` itself.

## Cookie authentication and non-browser clients

Cookie authentication's redirect-on-challenge behavior (`LoginPath`) assumes a browser that follows
redirects and stores cookies — an API client expecting a plain 401/403 status code gets a 302
instead unless you override `Events.OnRedirectToLogin`/`OnRedirectToAccessDenied` to return the raw
status code for API-shaped requests:

```csharp
options.Events.OnRedirectToLogin = context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    context.Response.Redirect(context.RedirectUri);
    return Task.CompletedTask;
};
```

This distinction matters most in a multi-scheme setup serving both a browser UI and an API from the
same application — see [multi-scheme-setups.md](multi-scheme-setups.md).
