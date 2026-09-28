# Client Credentials Flow

The client credentials flow authenticates the client application itself — no end user is involved —
and is the standard shape for service-to-service token issuance (a background job, a machine
client, another API calling this one on its own behalf).

## Enabling the flow

```csharp
options.AllowClientCredentialsFlow();

options.SetTokenEndpointUris("connect/token");

options.UseAspNetCore()
       .EnableTokenEndpointPassthrough();
```

No authorization endpoint is needed for this flow — there's no interactive consent step, so only the
token endpoint matters.

## Handling the token request

```csharp
app.MapPost("connect/token", async (HttpContext context, IOpenIddictApplicationManager applicationManager) =>
{
    var request = context.GetOpenIddictServerRequest()!;

    if (request.IsClientCredentialsGrantType())
    {
        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The application details cannot be found.");

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, await applicationManager.GetClientIdAsync(application));
        identity.SetClaim(Claims.Name, await applicationManager.GetDisplayNameAsync(application));
        identity.SetScopes(request.GetScopes());

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    return Results.BadRequest(new { error = Errors.UnsupportedGrantType });
});
```

OpenIddict authenticates the client itself (validating `client_id`/`client_secret` from the request,
per the application's registered `ClientType`) before your handler runs — a request with an invalid
client secret never reaches this code.

## Registering a confidential client

Client credentials is exclusively a confidential-client flow — the client must be able to keep a
secret, since there's no user present to authenticate instead:

```csharp
await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
{
    ClientId = "background-worker",
    ClientSecret = workerSecret,
    ClientType = ClientTypes.Confidential,
    Permissions =
    {
        Permissions.Endpoints.Token,
        Permissions.GrantTypes.ClientCredentials,
        Permissions.Scopes.Prefix + "api",
    },
});
```

`ClientSecret` is hashed by OpenIddict's application manager when stored — the plaintext value you
pass to `CreateAsync` is not retrievable afterward; the client application must be given its secret
out-of-band at provisioning time.

## No refresh tokens for this flow

Client credentials tokens represent the client itself, not a user session — there is nothing to
"refresh" a client's own identity into, so `AllowRefreshTokenFlow()` is irrelevant here. A client
credentials client simply requests a new access token again (repeating the same flow) when its
current one expires, rather than exchanging a refresh token.
