# OpenIddict

Guidance on OpenIddict, the OAuth 2.0/OpenID Connect server and validation library for .NET — the
routing table (by task) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic

| File | Covers |
| --- | --- |
| `server-configuration.md` | `AddOpenIddict`/`AddCore`/`AddServer`, storage backend, signing/encryption certificates |
| `validation-configuration.md` | `AddValidation`, local vs. remote (introspection) token validation |
| `authorization-code-flow.md` | PKCE-protected authorization code flow, authorization/token endpoint handlers, client registration |
| `client-credentials-flow.md` | Service-to-service token issuance, confidential client registration |
| `refresh-token-flow.md` | Refresh token issuance, rotation, explicit revocation |
| `scopes-and-claims.md` | Scope registration, per-client scope permissions, claim destinations |
| `identity-integration.md` | Backing OpenIddict with ASP.NET Core Identity as the user store |
| `testing.md` | Integration-testing token endpoints and flow enforcement |

## Scope

OpenIddict's server (`AddServer`) and validation (`AddValidation`) components: flow configuration,
scope/claim setup, token validation, and the seam with ASP.NET Core Identity as a user store. Out of
scope: ASP.NET Core Identity's own API surface beyond that seam, other OAuth 2.0/OIDC server
implementations, and ASP.NET Core's general authentication model beyond what OpenIddict's own
registration requires.

Each reference file notes a version-specific fact inline where one applies; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).
