# OpenIddict

Guidance on OpenIddict, the OAuth 2.0/OpenID Connect server and token-validation library for .NET:
server and validation setup, the authorization code, client credentials, and refresh token flows,
scope and claim configuration, and integrating with ASP.NET Core Identity.

## When to reach for it

- You're standing up an OAuth 2.0/OIDC authorization server with OpenIddict.
- You're adding a new grant type to an existing OpenIddict server, or configuring token validation
  on a resource server.
- You're debugging why a claim isn't showing up in an issued token, or wiring OpenIddict to an
  existing ASP.NET Core Identity user store.

## Using it

This skill fires automatically when your request involves configuring an OpenIddict server or
validation, implementing a flow, or debugging token contents. You can also invoke it directly with
`/dotnet-openiddict`.

## What it covers

| Topic | Reference |
| --- | --- |
| `AddOpenIddict`/`AddCore`/`AddServer`, storage backend, signing/encryption certificates | [references/server-configuration.md](references/server-configuration.md) |
| `AddValidation`, local vs. remote (introspection) token validation | [references/validation-configuration.md](references/validation-configuration.md) |
| The authorization code flow with PKCE | [references/authorization-code-flow.md](references/authorization-code-flow.md) |
| The client credentials flow for service-to-service tokens | [references/client-credentials-flow.md](references/client-credentials-flow.md) |
| Refresh tokens, rotation, and explicit revocation | [references/refresh-token-flow.md](references/refresh-token-flow.md) |
| Scope registration, per-client scope permissions, claim destinations | [references/scopes-and-claims.md](references/scopes-and-claims.md) |
| Backing OpenIddict with ASP.NET Core Identity as the user store | [references/identity-integration.md](references/identity-integration.md) |
| Testing token endpoints and flow enforcement | [references/testing.md](references/testing.md) |

## Example prompts

- "Set up OpenIddict with the client credentials flow for service-to-service calls."
- "Why isn't my custom claim making it into the access token?"
- "Wire OpenIddict's user store to our existing ASP.NET Core Identity setup."
