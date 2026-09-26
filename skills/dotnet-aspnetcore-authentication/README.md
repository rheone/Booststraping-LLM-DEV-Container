# ASP.NET Core Authentication

Guidance on ASP.NET Core's authentication and authorization middleware — the routing table (by task)
is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic

| File | Covers |
| --- | --- |
| `authentication-schemes.md` | `AddAuthentication`, scheme registration, default-scheme properties, middleware ordering |
| `cookie-authentication.md` | `AddCookie`, sign-in/sign-out, redirect-vs-status-code behavior |
| `jwt-bearer-authentication.md` | `AddJwtBearer`, `TokenValidationParameters`, validation events |
| `claims-and-principals.md` | `ClaimsPrincipal`/`ClaimsIdentity` construction and reading, `IClaimsTransformation` |
| `authorization-attributes.md` | `[Authorize]`/`[AllowAnonymous]`, `RequireAuthorization()`/`AllowAnonymous()`, 401 vs. 403 |
| `policy-based-authorization.md` | `AddAuthorizationBuilder`, `RequireClaim`/`RequireRole`, custom `IAuthorizationRequirement` handlers, resource-based authorization |
| `custom-authentication-handlers.md` | `AuthenticationHandler<TOptions>` for credential types with no built-in scheme |
| `multi-scheme-setups.md` | Combining cookie and JWT bearer (or any two schemes) in one app |
| `testing.md` | Testing requirement handlers, policies, and `[Authorize]`-guarded endpoints |

## Scope

ASP.NET Core's own authentication scheme model, built-in cookie and JWT bearer handlers, and
policy-based authorization. Out of scope: any specific external identity provider or
identity-server product (this skill documents the framework abstractions any such integration sits
on top of, not a provider's own configuration surface), and user account storage/management.

Each reference file notes a version-specific fact inline where one applies; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).
