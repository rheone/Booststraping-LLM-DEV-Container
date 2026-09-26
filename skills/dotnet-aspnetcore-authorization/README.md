# ASP.NET Core Authorization

Every authorization form ASP.NET Core ships with — simple `[Authorize]`, role-based, claims-based,
full policy-based requirements and handlers, resource-based checks against a loaded object, custom
policy providers, and the Razor Pages/MVC/minimal API conventions that apply a policy across a whole
folder or route group at once.

## When to reach for it

- You're deciding whether a check belongs in `Roles`, `RequireClaim`, or a custom
  `IAuthorizationRequirement`/handler pair.
- A policy you registered never seems to succeed, and you suspect a requirement with no matching
  registered handler.
- You need to authorize against the specific resource being accessed (its owner, its state), not
  just endpoint metadata — an `[Authorize]` attribute alone can't see the loaded object.
- You want every endpoint in an app to require authentication by default, protecting anything a
  future change forgets to annotate.
- You're applying authorization to an entire Razor Pages folder, MVC controller, or minimal API
  route group instead of one page/action/endpoint at a time.

## Using it

This skill is model-invoked: it activates automatically when you're writing, reviewing, or
debugging authorization checks in an ASP.NET Core app. You can also invoke it directly by asking for
it or typing `/dotnet-aspnetcore-authorization`.

## What it covers

| Topic | Reference |
| --- | --- |
| `[Authorize]`/`[AllowAnonymous]` and the default policy | [references/simple-authorization.md](references/simple-authorization.md) |
| Role-based checks | [references/role-based-authorization.md](references/role-based-authorization.md) |
| Claims-based checks | [references/claims-based-authorization.md](references/claims-based-authorization.md) |
| Named policies, custom requirements and handlers, AND vs. OR evaluation | [references/policy-based-authorization-and-requirements.md](references/policy-based-authorization-and-requirements.md) |
| Default vs. fallback policies, requiring global authentication | [references/default-and-fallback-policies.md](references/default-and-fallback-policies.md) |
| Imperative and resource-based authorization via `IAuthorizationService` | [references/imperative-and-resource-based-authorization.md](references/imperative-and-resource-based-authorization.md) |
| Restricting a check to a specific authentication scheme | [references/limiting-identity-by-scheme.md](references/limiting-identity-by-scheme.md) |
| Dynamically-generated policies via a custom `IAuthorizationPolicyProvider` | [references/custom-authorization-policy-providers.md](references/custom-authorization-policy-providers.md) |
| Injecting services into a requirement handler | [references/dependency-injection-in-handlers.md](references/dependency-injection-in-handlers.md) |
| Razor Pages/MVC/minimal API folder- and group-level conventions | [references/mvc-razor-pages-and-minimal-api-conventions.md](references/mvc-razor-pages-and-minimal-api-conventions.md) |
| Testing requirement handlers, policies, and guarded endpoints | [references/testing-authorization.md](references/testing-authorization.md) |

## Example prompts

- "Why does this policy never let anyone through, even users with the right role?"
- "I need to check that the current user actually owns this order before letting them cancel it —
  where does that check belong?"
- "How do I require every page under `/Admin` to need the `RequireAdminRole` policy without adding
  `[Authorize]` to each page individually?"
