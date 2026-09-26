# Refresh Token Flow

The refresh token flow lets a client obtain a new access token without re-running the full
interactive authorization (login/consent) — issued alongside an access token from a flow that
supports it, then exchanged later when the access token expires.

## Enabling the flow

```csharp
options.AllowAuthorizationCodeFlow()
       .AllowRefreshTokenFlow();

options.SetTokenEndpointUris("connect/token");
```

Refresh tokens are only issued for flows that produce a user-associated session — enable
`AllowRefreshTokenFlow()` alongside `AllowAuthorizationCodeFlow()` (or another interactive flow), not
on its own.

## Requesting a refresh token at issuance time

A refresh token is issued automatically alongside the access token when the client's registered
scopes include `offline_access` and the flow issuing the initial token supports refresh:

```csharp
identity.SetScopes(request.GetScopes().Concat([Scopes.OfflineAccess]));
```

Without requesting the `offline_access` scope, OpenIddict issues an access token only — no refresh
token — even with `AllowRefreshTokenFlow()` enabled server-wide.

## Handling the refresh token grant

```csharp
app.MapPost("connect/token", async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest()!;

    if (request.IsRefreshTokenGrantType())
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Optional: re-check the user is still active/enabled before reissuing.
        var userId = result.Principal!.GetClaim(Claims.Subject);
        // if (!await userStore.IsActiveAsync(userId)) return Results.Forbid();

        return Results.SignIn(result.Principal!, properties: null,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    return Results.BadRequest(new { error = Errors.UnsupportedGrantType });
});
```

OpenIddict validates the refresh token itself (signature, expiration, whether it's been revoked)
before your handler's `AuthenticateAsync` call succeeds — re-checking the underlying user/account
state (disabled account, revoked access) inside the handler is your responsibility, since OpenIddict
has no way to know about application-specific account status changes on its own.

## Refresh token rotation

By default, OpenIddict issues a new refresh token on every refresh grant and invalidates the
previous one (rolling/rotating refresh tokens) — a client that reuses an already-exchanged refresh
token fails, which is the expected behavior for detecting a leaked token being replayed by an
attacker after the legitimate client has already rotated past it.

## Revoking refresh tokens explicitly

Use `IOpenIddictTokenManager` to revoke a specific token or all tokens for a subject/application
directly — for a "sign out everywhere" or "revoke this device" feature:

```csharp
await foreach (var token in tokenManager.FindBySubjectAsync(userId))
{
    await tokenManager.TryRevokeAsync(token);
}
```
