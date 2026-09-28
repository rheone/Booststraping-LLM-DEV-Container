# Simple Authorization

`[Authorize]` and `[AllowAnonymous]` (`Microsoft.AspNetCore.Authorization` namespace) are the
declarative baseline every other mechanism in this skill builds on.

## `[Authorize]` with no parameters

```csharp
[Authorize]
public IActionResult VacationBalance() => View();
```

```csharp
app.MapGet("/profile", () => Results.Ok())
    .RequireAuthorization();
```

With no `Roles` or `Policy` set, `[Authorize]`/`RequireAuthorization()` uses the **default policy**:
authenticated users are authorized, unauthenticated users aren't. Endpoints with no authorization
metadata at all don't require authorization unless the app configures a fallback policy — see
[default-and-fallback-policies.md](default-and-fallback-policies.md).

## `[AllowAnonymous]`

```csharp
[Authorize]
public class VacationController : Controller
{
    public IActionResult Index() => View();

    [AllowAnonymous]
    public IActionResult VacationPolicy() => View();
}
```

```csharp
app.MapGet("/login", [AllowAnonymous] () => "Public endpoint");
app.MapGet("/login2", () => "Also public").AllowAnonymous();
```

`[AllowAnonymous]` on an action/endpoint overrides an `[Authorize]` applied at the controller/class
level for that one member — it doesn't merely skip the default policy, it tells the authorization
middleware not to enforce any authorization failure for that endpoint at all. Authentication can
still run and populate `HttpContext.User`; the request just isn't rejected for lacking it.

## Applying `[Authorize]` with `Roles` or `Policy`

```csharp
[Authorize(Roles = "Admin, Superuser")]
public IActionResult Dashboard() => View();

[Authorize(Policy = "Over21")]
public IActionResult PurchaseAlcohol() => View();
```

Setting `Roles` or `Policy` switches the check away from the default policy to a role check or a
named policy, respectively — see [role-based-authorization.md](role-based-authorization.md) and
[policy-based-authorization-and-requirements.md](policy-based-authorization-and-requirements.md).

## Stacking multiple `[Authorize]` attributes

```csharp
[Authorize(Roles = "Administrator, PowerUser")]
[Authorize(Roles = "RemoteEmployee")]
[Authorize(Policy = "CustomPolicy")]
public IActionResult Api1() => Ok();
```

Multiple `[Authorize]` attributes on the same member combine with AND — every attribute's condition
must pass. Here a caller must be in the `Administrator` *or* `PowerUser` role, *and* in the
`RemoteEmployee` role, *and* satisfy the `CustomPolicy` policy. This is different from a single
`Roles = "A, B"` list, which is an OR *within* that one attribute — see
[role-based-authorization.md](role-based-authorization.md) for that distinction.
