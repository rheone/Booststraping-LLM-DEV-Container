# Authorization Code Flow

The authorization code flow is the standard flow for a user-facing client (a web app, a mobile app)
that needs a user to authenticate interactively and grant consent before the client receives a
token. OpenIddict expects you to implement the interactive parts (login, consent) yourself; it
handles the code issuance, exchange, and token issuance mechanics.

## Enabling the flow

```csharp
options.AllowAuthorizationCodeFlow()
       .RequireProofKeyForCodeExchange();

options.SetAuthorizationEndpointUris("connect/authorize")
       .SetTokenEndpointUris("connect/token");

options.UseAspNetCore()
       .EnableAuthorizationEndpointPassthrough()
       .EnableTokenEndpointPassthrough();
```

`RequireProofKeyForCodeExchange()` enforces PKCE (Proof Key for Code Exchange) on every client using
this flow — mandatory for public clients (mobile/SPA apps that can't securely hold a client secret),
and recommended even for confidential clients as defense against authorization-code interception.

## Implementing the authorization endpoint

With `EnableAuthorizationEndpointPassthrough()`, your own endpoint receives the request and is
responsible for authenticating the user (redirecting to a login page if needed) and rendering
consent, then calling `SignInAsync` with the OpenIddict scheme to issue the authorization code:

```csharp
app.MapMethods("connect/authorize", ["GET", "POST"], async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest()!;

    var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!result.Succeeded)
    {
        return Results.Challenge(authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme]);
    }

    var identity = new ClaimsIdentity(
        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        nameType: Claims.Name,
        roleType: Claims.Role);

    identity.SetClaim(Claims.Subject, result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
    identity.SetScopes(request.GetScopes());

    return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
        authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});
```

`identity.SetScopes(...)` and the other `Set*`/`Get*` extension methods (from
`OpenIddict.Abstractions`) attach OpenIddict-specific data to the claims identity that the server
component reads when issuing the code and, later, the token — see
[scopes-and-claims.md](scopes-and-claims.md) for the full set.

## Implementing the token endpoint

The token endpoint exchanges the authorization code for tokens. With passthrough enabled, your
endpoint still delegates the actual exchange logic back to OpenIddict by re-signing in with the
principal it already validated from the code:

```csharp
app.MapPost("connect/token", async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest()!;

    if (request.IsAuthorizationCodeGrantType())
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return Results.SignIn(result.Principal!, properties: null,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    return Results.BadRequest(new { error = Errors.UnsupportedGrantType });
});
```

OpenIddict validates the code, PKCE verifier, and redirect URI internally before your handler's
`AuthenticateAsync` call succeeds — a mismatched `code_verifier` or `redirect_uri` fails before
reaching this code.

## Registering a client application

Every client using this flow must be registered as an OpenIddict application, with its allowed
redirect URIs and permissions declared explicitly:

```csharp
await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
{
    ClientId = "web-client",
    ClientType = ClientTypes.Public,
    RedirectUris = { new Uri("https://client.example.com/callback") },
    Permissions =
    {
        Permissions.Endpoints.Authorization,
        Permissions.Endpoints.Token,
        Permissions.GrantTypes.AuthorizationCode,
        Permissions.ResponseTypes.Code,
        Permissions.Scopes.Prefix + "api",
    },
});
```

A request for a redirect URI or grant type not declared in `Permissions` fails validation even if
the flow itself is enabled server-wide — per-application permissions are a second gate on top of the
server-wide `AllowAuthorizationCodeFlow()` call.
