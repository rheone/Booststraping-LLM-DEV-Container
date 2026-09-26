---
name: dotnet-aspnetcore-authorization
description: Comprehensive reference for every authorization form ASP.NET Core provides (verified current against ASP.NET Core 10.0) — simple `[Authorize]`/`[AllowAnonymous]` and the default policy, role-based checks (`Roles`, `RequireRole`, `IsInRole`), claims-based checks (`RequireClaim`, `RequireAssertion`), policy-based authorization with `AddAuthorizationBuilder`, custom `IAuthorizationRequirement`/`AuthorizationHandler<T>` pairs, one-requirement/many-handler (OR) vs. one-policy/many-requirement (AND) evaluation, default vs. fallback policies and requiring global authentication, imperative and resource-based authorization via `IAuthorizationService.AuthorizeAsync`, `OperationAuthorizationRequirement` for CRUD-shaped checks, limiting an authorization check to a specific authentication scheme, custom `IAuthorizationPolicyProvider` implementations for dynamically-parameterized policies, dependency injection inside a requirement handler, and the Razor Pages/MVC/minimal API conventions (`RequireAuthorization`, `AuthorizeFolder`/`AuthorizePage`) that apply a policy without touching every endpoint individually. Use when deciding which authorization mechanism fits a given check, writing a custom requirement/handler pair, debugging why a policy silently never succeeds, distinguishing role- from claims- from policy-based authorization, authorizing against a loaded resource instead of just endpoint metadata, or applying authorization across a folder/group of Razor Pages or minimal API endpoints at once.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# ASP.NET Core Authorization

Authorization decides what an already-authenticated (or explicitly anonymous) user is allowed to do.
Every mechanism in this skill ultimately reduces to the same two moving parts: a **policy** (one or
more requirements, evaluated with AND semantics) and one or more **handlers** that inspect the
current user — and, for resource-based checks, a loaded resource — to decide whether each
requirement succeeds. Role-based and claims-based authorization are a declarative shorthand over
that same requirement/handler model, not a separate system.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Applying `[Authorize]`/`[AllowAnonymous]`, understanding the built-in default policy | [references/simple-authorization.md](references/simple-authorization.md) |
| Restricting access by role (`Roles`, `RequireRole`, `IsInRole`) | [references/role-based-authorization.md](references/role-based-authorization.md) |
| Restricting access by claim presence or value (`RequireClaim`, `RequireAssertion`) | [references/claims-based-authorization.md](references/claims-based-authorization.md) |
| Defining a named policy, writing a custom `IAuthorizationRequirement`/`AuthorizationHandler<T>`, understanding AND vs. OR evaluation | [references/policy-based-authorization-and-requirements.md](references/policy-based-authorization-and-requirements.md) |
| Requiring authentication globally, or understanding which policy an endpoint actually gets | [references/default-and-fallback-policies.md](references/default-and-fallback-policies.md) |
| Authorizing against a loaded resource (an order, a document) instead of just endpoint metadata; calling `IAuthorizationService` directly from code or a view | [references/imperative-and-resource-based-authorization.md](references/imperative-and-resource-based-authorization.md) |
| Restricting a check to one specific authentication scheme, or combining cookie + JWT | [references/limiting-identity-by-scheme.md](references/limiting-identity-by-scheme.md) |
| Generating policies dynamically instead of registering each one by name | [references/custom-authorization-policy-providers.md](references/custom-authorization-policy-providers.md) |
| Injecting a service (a repository, `ILoggerFactory`) into a requirement handler | [references/dependency-injection-in-handlers.md](references/dependency-injection-in-handlers.md) |
| Applying authorization to an entire Razor Pages folder, MVC controller, or minimal API route group | [references/mvc-razor-pages-and-minimal-api-conventions.md](references/mvc-razor-pages-and-minimal-api-conventions.md) |
| Testing a requirement handler, a policy, or an authorization-guarded endpoint | [references/testing-authorization.md](references/testing-authorization.md) |

## Quick start

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AtLeast21", policy => policy.Requirements.Add(new MinimumAgeRequirement(21)));

builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/drinks/order", () => Results.Ok())
    .RequireAuthorization("AtLeast21");
```

The single most common miss: registering a requirement with a policy but never registering its
handler in DI. Nothing throws at startup — the requirement simply never has anything call
`context.Succeed(...)` for it, so the policy fails every evaluation. See
[references/policy-based-authorization-and-requirements.md](references/policy-based-authorization-and-requirements.md).

## Out of scope

- Authentication itself (schemes, cookie/JWT handler setup, `ClaimsPrincipal` construction) — this
  skill assumes an already-authenticated (or explicitly anonymous) `ClaimsPrincipal` and covers only
  what happens to it after that point.
- ASP.NET Core Identity's user/role storage and management (`UserManager`, `RoleManager`,
  registration flows) — orthogonal to the authorization requirement/handler/policy model this skill
  covers, which operates on whatever claims and roles already exist on the principal.
- Blazor-specific component-level authorization UI (`AuthorizeView`'s rendering behavior, cascading
  `AuthenticationState`) — this skill's policy, requirement, and handler content applies identically
  inside Blazor, but the component markup and rendering lifecycle are a UI framework concern of their
  own.
