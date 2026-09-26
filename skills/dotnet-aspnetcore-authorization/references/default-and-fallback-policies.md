# Default and Fallback Policies

The authorization middleware combines an endpoint's authorization metadata into a single policy to
evaluate. Which policy that ends up being depends on exactly what metadata the endpoint carries —
this is the part of ASP.NET Core authorization most likely to produce a surprising "why did this
endpoint let an anonymous request through" bug.

## Selection rules

| Authorization metadata on the endpoint | Policy or behavior used |
| --- | --- |
| None | `AuthorizationOptions.FallbackPolicy`, if configured. Defaults to `null` — no authorization required. |
| `[Authorize]` / `RequireAuthorization()`, no policy name | `AuthorizationOptions.DefaultPolicy` (defaults to requiring an authenticated user), unless an explicit `AuthorizationPolicy` instance is also present. |
| `[Authorize(Policy = "Name")]` / `RequireAuthorization("Name")` | The named policy. |
| `[Authorize(Roles = "...")]` | A policy built from the listed roles — the default policy is *not* added alongside it. |
| `[Authorize(AuthenticationSchemes = "...")]`, no policy/roles | The specified scheme, plus the default policy (unless an explicit policy instance is present). |
| Multiple `[Authorize]` attributes / policy-selecting `RequireAuthorization(...)` calls | Every named policy, role list, scheme, and explicit policy combines; all resulting requirements must succeed. The fallback policy is never used here. |
| `[AllowAnonymous]` / `AllowAnonymous()` | The middleware doesn't enforce an authorization failure for the endpoint at all. |

The fallback policy is never combined with a named or default policy — it only applies when no
policy is produced from the endpoint's metadata at all. `[Authorize]` alone always uses the default
policy instead of the fallback, even when both are configured.

## Requiring authentication globally

For a server-side app where most endpoints require authentication, set a fallback policy so newly
added endpoints are protected by default even if a developer forgets to annotate them:

```csharp
var requireAuthPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(requireAuthPolicy);
```

Apply `[AllowAnonymous]`/`AllowAnonymous()` to any endpoint that's intentionally public — the
fallback policy only applies where no metadata already opted out.

## What the fallback policy doesn't cover

- It applies only to requests the authorization middleware actually processes. A request that never
  reaches a matched endpoint still goes through it, but static files served by the static file
  middleware ahead of the authorization middleware in the pipeline aren't protected by it.
- A public endpoint that depends on static assets (a public page's CSS/JS) needs those assets to
  independently allow anonymous access — the fallback policy protecting the *endpoint* doesn't
  retroactively protect or unprotect the *files* it references.

## Requirements supplied via `IAuthorizationRequirementData`

Requirements attached to endpoint metadata through `IAuthorizationRequirementData` (a custom
attribute implementing that interface) are combined *after* policy selection runs — if no other
metadata produces a policy, these requirements combine with the fallback policy when one is
configured. In ASP.NET Core 8.0 through 10.0, attributes implementing this interface are enforced
only on minimal API and other routed endpoints, not on MVC controllers/actions or Blazor's
`AuthorizeView`/`AuthorizeRouteView` — that support extends to those hosting models starting with
.NET 11.
