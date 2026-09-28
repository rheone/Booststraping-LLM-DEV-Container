# Token Validation with AddValidation

`AddValidation` configures an OpenIddict component that **validates** incoming access tokens on a
resource server (an API) — distinct from `AddServer`, which **issues** tokens. A pure resource server
that never issues tokens itself needs only `AddValidation`, not `AddServer`.

## Local validation (server and resource server are the same app)

When the API validating tokens is the same application that issued them, point validation at the
local server directly:

```csharp
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });
```

`UseLocalServer()` reads the signing/encryption configuration from the same process's `AddServer`
registration — no network round-trip, no separate configuration duplication.

## Remote validation (resource server is a separate app from the issuer)

When the API is a separate application from the token issuer, configure the issuer's address and let
OpenIddict fetch its configuration/signing keys via OAuth 2.0 introspection or the OpenID Connect
discovery document:

```csharp
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("https://issuer.example.com/");
        options.AddAudiences("resource-server-1");

        // Introspection (opaque or reference tokens validated by calling back to the issuer):
        options.UseIntrospection()
               .SetClientId("resource-server-1")
               .SetClientSecret(resourceServerSecret);

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });
```

Use introspection (`UseIntrospection()`) when the issued tokens are **reference tokens** (opaque
identifiers OpenIddict looks up server-side) rather than self-contained JWTs — see
[server-configuration.md](server-configuration.md) for choosing between the two at issuance time.
For self-contained JWTs, remote validation can instead fetch the issuer's public signing keys via
discovery and validate locally, without a network call per request.

## Wiring [Authorize] against the validation handler

`AddValidation` registers the `OpenIddict.Validation.AspNetCore` authentication scheme automatically;
guard endpoints the same way as any ASP.NET Core authentication scheme:

```csharp
app.MapGet("/api/resource", () => Results.Ok())
    .RequireAuthorization();
```

If the application also registers other authentication schemes, pin the endpoint to OpenIddict's
validation scheme explicitly (`OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`) rather
than relying on whichever scheme ends up as the default.

## Scope enforcement

`AddAudiences(...)` restricts validation to tokens issued for this specific resource server;
enforcing a required **scope** on a given endpoint is a policy concern, not a validation-registration
one — combine `RequireAuthorization()` with a policy checking the `scope` claim (see
[scopes-and-claims.md](scopes-and-claims.md)).
