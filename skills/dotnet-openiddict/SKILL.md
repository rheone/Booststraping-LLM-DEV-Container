---
name: dotnet-openiddict
description: Guidance on OpenIddict (verified current stable release OpenIddict 7.7.0 / OpenIddict.AspNetCore 7.5.0 / OpenIddict.Server.AspNetCore 7.5.0 / OpenIddict.EntityFrameworkCore 7.4.0, Apache-2.0 license), the third-party OAuth 2.0/OpenID Connect server and validation library for .NET — AddOpenIddict/AddCore/AddServer/AddValidation setup, the authorization code flow with PKCE, the client credentials flow, the refresh token flow and rotation, scope registration and per-client scope permissions, claim destinations and serialization into issued tokens, local vs. remote token validation on a resource server, and integrating OpenIddict's token issuance with ASP.NET Core Identity as the underlying user store. Use when setting up an OAuth 2.0/OIDC authorization server with OpenIddict, adding a new grant type, debugging why a claim isn't showing up in an issued token, configuring token validation on a resource server, or wiring OpenIddict to an existing ASP.NET Core Identity user store.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# OpenIddict

Guidance on OpenIddict, the OAuth 2.0/OpenID Connect server and token-validation library for .NET.
Organized by task, not by OpenIddict version — the `AddServer`/`AddValidation` builder surface has
been stable across the 3.x through 7.x line; each reference file notes a version-specific fact
inline where one applies.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Setting up `AddOpenIddict`/`AddCore`/`AddServer`, choosing a storage backend, configuring signing/encryption | [references/server-configuration.md](references/server-configuration.md) |
| Configuring `AddValidation` on a resource server, local vs. remote (introspection) validation | [references/validation-configuration.md](references/validation-configuration.md) |
| Implementing the authorization code flow with PKCE for a user-facing client | [references/authorization-code-flow.md](references/authorization-code-flow.md) |
| Implementing the client credentials flow for service-to-service tokens | [references/client-credentials-flow.md](references/client-credentials-flow.md) |
| Implementing refresh tokens, rotation, and explicit revocation | [references/refresh-token-flow.md](references/refresh-token-flow.md) |
| Registering scopes, gating scopes per client, controlling which claims land in a token | [references/scopes-and-claims.md](references/scopes-and-claims.md) |
| Backing OpenIddict's user identity with ASP.NET Core Identity | [references/identity-integration.md](references/identity-integration.md) |
| Testing token endpoints, flow enforcement (PKCE, redirect URIs), and your own handler logic | [references/testing.md](references/testing.md) |

## Quick start

A minimal client-credentials server, current API surface (7.5.0/7.6.0/7.7.0 line):

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("connect/token");
        options.AllowClientCredentialsFlow();
        options.RegisterScopes("api");
        options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        options.UseAspNetCore().EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });
```

The single most common miss: enabling a flow with `AllowXFlow()` server-wide but forgetting the
matching `Permissions.GrantTypes.X` on the specific client application — a client's request fails
even when the flow is enabled globally if its own registered permissions don't include it. See
[authorization-code-flow.md](references/authorization-code-flow.md)'s application-registration
section.

## Out of scope

- ASP.NET Core Identity's own API surface (user store configuration, password/lockout policy,
  `UserManager`/`SignInManager` usage beyond the seam with OpenIddict) — a separate, self-contained
  concern; [references/identity-integration.md](references/identity-integration.md) covers only the
  integration seam, not Identity's API on its own terms.
- Other OAuth 2.0/OIDC server implementations — this skill is OpenIddict-only.
- ASP.NET Core's general authentication scheme/policy model beyond what OpenIddict's own
  `AddServer`/`AddValidation` registration requires.
