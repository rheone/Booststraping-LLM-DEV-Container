# Server Configuration

OpenIddict's server component issues and manages OAuth 2.0/OpenID Connect tokens. Registration
starts with `AddOpenIddict()`, which returns a builder exposing `AddCore`, `AddServer`, and
`AddValidation` — each configuring a distinct, independently usable piece.

## Package references

The current stable releases (verified against NuGet as of this skill's writing): `OpenIddict.Core`
7.6.0, `OpenIddict.AspNetCore` 7.5.0, `OpenIddict.Server.AspNetCore` 7.5.0, and
`OpenIddict.EntityFrameworkCore` 7.4.0 (for Entity Framework Core-backed storage). A 8.0.0 preview
line exists in parallel; the guidance here targets the stable 7.x line.

```xml
<PackageReference Include="OpenIddict.AspNetCore" Version="7.5.0" />
<PackageReference Include="OpenIddict.EntityFrameworkCore" Version="7.4.0" />
```

## Minimal server registration

```csharp
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("connect/token")
               .SetAuthorizationEndpointUris("connect/authorize");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        options.RegisterScopes("api");

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough();
    });
```

- **`AddCore`** wires OpenIddict's entity model (applications, authorizations, scopes, tokens) to a
  storage backend — Entity Framework Core is the most common, via `UseEntityFrameworkCore()`.
- **`AddServer`** configures the OAuth 2.0/OpenID Connect endpoints themselves: which flows are
  allowed, which endpoint URIs are exposed, and how tokens are signed/encrypted.
- **`UseAspNetCore()`** (inside `AddServer`) integrates the server with the ASP.NET Core request
  pipeline; `EnableTokenEndpointPassthrough()`/`EnableAuthorizationEndpointPassthrough()` let your
  own controller/minimal API endpoint handle the actual request instead of OpenIddict intercepting it
  outright — the common shape when you need custom logic (looking up a user, rendering a consent
  page) around the standard flow.

## Signing and encryption certificates

`AddDevelopmentEncryptionCertificate()`/`AddDevelopmentSigningCertificate()` generate ephemeral,
self-signed certificates suitable only for local development — they are not persisted and regenerate
on every app restart, invalidating any previously issued token. For any deployed environment, supply
real certificates instead:

```csharp
options.AddEncryptionCertificate(encryptionCertificate)
       .AddSigningCertificate(signingCertificate);
```

or, for a symmetric-key setup that avoids certificate management entirely:

```csharp
options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(encryptionKeySecret)))
       .AddSigningKey(new SymmetricSecurityKey(Convert.FromBase64String(signingKeySecret)));
```

## Registering scopes

`RegisterScopes(...)` declares which scopes the server recognizes and can issue in a token; a scope
requested by a client that isn't registered here is rejected during authorization. See
[scopes-and-claims.md](scopes-and-claims.md) for the full scope/claim configuration surface,
including per-client scope permissions.

## Choosing which flows to allow

`AddServer` only issues tokens for flows explicitly enabled — `AllowAuthorizationCodeFlow()`,
`AllowClientCredentialsFlow()`, `AllowRefreshTokenFlow()`, and others each opt in one grant type. See
[authorization-code-flow.md](authorization-code-flow.md),
[client-credentials-flow.md](client-credentials-flow.md), and
[refresh-token-flow.md](refresh-token-flow.md) for the specific setup and request/response shape of
each.
