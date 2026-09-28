# ASP.NET Core Authentication

Guidance on ASP.NET Core's built-in authentication and authorization middleware: the scheme model
behind `AddAuthentication`, the built-in cookie and JWT bearer handlers, `ClaimsPrincipal`
construction, and policy-based authorization with `[Authorize]`.

## When to reach for it

- You're wiring up authentication schemes for a new ASP.NET Core app, or adding a second scheme
  alongside an existing one.
- You're debugging why a request comes back `401` when you expected `403`, or the reverse.
- You're defining a policy that requires a specific claim or role, or writing a custom
  `AuthenticationHandler<TOptions>` for a credential type with no built-in scheme.

## Using it

This skill fires automatically when your request involves wiring up authentication, debugging a
401/403 response, or defining an authorization policy. You can also invoke it directly with
`/dotnet-aspnetcore-authentication`.

## What it covers

| Topic | Reference |
| --- | --- |
| `AddAuthentication`, scheme registration, default-scheme properties | [references/authentication-schemes.md](references/authentication-schemes.md) |
| Cookie-based sign-in for a browser-facing app | [references/cookie-authentication.md](references/cookie-authentication.md) |
| JWT bearer validation for an API | [references/jwt-bearer-authentication.md](references/jwt-bearer-authentication.md) |
| Building or reading `ClaimsPrincipal`/`ClaimsIdentity`, transforming claims | [references/claims-and-principals.md](references/claims-and-principals.md) |
| `[Authorize]`/`[AllowAnonymous]` and their minimal API equivalents | [references/authorization-attributes.md](references/authorization-attributes.md) |
| Named policies with `AddAuthorizationBuilder`, claim/role requirements, custom handlers | [references/policy-based-authorization.md](references/policy-based-authorization.md) |
| Writing a custom `AuthenticationHandler<TOptions>` | [references/custom-authentication-handlers.md](references/custom-authentication-handlers.md) |
| Combining cookie and JWT (or any two schemes) in one app | [references/multi-scheme-setups.md](references/multi-scheme-setups.md) |
| Testing policies, requirement handlers, and `[Authorize]`-guarded endpoints | [references/testing.md](references/testing.md) |

## Example prompts

- "Set up JWT bearer authentication for this API and require an 'Administrator' role on the
  admin endpoints."
- "Why is my endpoint returning 403 instead of 401 when there's no token at all?"
- "I need cookie sign-in for the browser-facing pages and JWT for the API, in the same app."
