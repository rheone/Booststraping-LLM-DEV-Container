# Custom Authorization Policy Providers

Registering every policy by name with `AddPolicy` works until a policy needs a parameter with an
unbounded range — a minimum age, a room number, anything where writing one named policy per possible
value would mean dozens or hundreds of near-identical registrations. `IAuthorizationPolicyProvider`
lets an app generate a policy dynamically from the policy name itself, instead of looking it up from
a fixed, pre-registered set.

## The interface

```csharp
public interface IAuthorizationPolicyProvider
{
    Task<AuthorizationPolicy?> GetPolicyAsync(string policyName);
    Task<AuthorizationPolicy> GetDefaultPolicyAsync();
    Task<AuthorizationPolicy?> GetFallbackPolicyAsync();
}
```

ASP.NET Core only ever uses one `IAuthorizationPolicyProvider` instance app-wide —
`DefaultAuthorizationPolicyProvider` is the framework's own implementation, and registering a custom
one in the service container replaces it entirely for every policy lookup, not just the custom
ones.

## A naming scheme parsed at lookup time

```csharp
public sealed class MinimumAgePolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "MinimumAge";
    private DefaultAuthorizationPolicyProvider DefaultPolicyProvider { get; } = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(policyName.AsSpan(PolicyPrefix.Length), out var age) && age >= 0)
        {
            var policy = new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme);
            policy.AddRequirements(new MinimumAgeRequirement(age));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return DefaultPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => DefaultPolicyProvider.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => DefaultPolicyProvider.GetFallbackPolicyAsync();
}
```

```csharp
builder.Services.AddSingleton<IAuthorizationPolicyProvider, MinimumAgePolicyProvider>();
```

The provider parses the age straight out of the policy name (`MinimumAge21` → `21`) instead of
requiring an `AddPolicy("MinimumAge21", ...)` call for every age an app might ever check. Most custom
providers, like this one, delegate to `DefaultAuthorizationPolicyProvider` for any name they don't
recognize, so ordinary role/claim/named policies registered the normal way keep working unchanged.

An `AuthorizationPolicyBuilder` needs at least one authentication scheme name (or
`RequireAssertion`/an always-succeeding requirement) — otherwise there's no information for the
framework to base a challenge on, and building the policy throws. An empty (unassigned)
`AuthenticationSchemes` list evaluates against the app's default schemes; it does not mean "any
registered scheme."

## Pairing the provider with a strongly-typed attribute

A hand-written policy-name string (`"MinimumAge" + age`) at every call site is error-prone — a custom
`AuthorizeAttribute` subclass wraps the naming convention behind a typed property:

```csharp
public sealed class MinimumAgeAuthorizeAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "MinimumAge";

    public MinimumAgeAuthorizeAttribute(int age) => Age = age;

    public int Age
    {
        get => !string.IsNullOrEmpty(Policy) && Policy.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(Policy.AsSpan(PolicyPrefix.Length), out var age) ? age : default;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            Policy = $"{PolicyPrefix}{value}";
        }
    }
}
```

```csharp
[MinimumAgeAuthorize(21)]
public IActionResult PurchaseAlcohol() => View();

app.MapGet("/must-be-21", [MinimumAgeAuthorize(21)] () => "Requires a 21+ birthdate claim.");
```

The requirement (`MinimumAgeRequirement`) and its handler (`MinimumAgeHandler`) are ordinary
policy-based authorization pieces — see
[policy-based-authorization-and-requirements.md](policy-based-authorization-and-requirements.md) —
the custom provider only changes how the *policy itself* gets produced, not how it's evaluated once
produced.

## Combining policies inside a custom provider

`AuthorizationPolicy.Combine` merges two already-built policies into one, requiring every combined
requirement to succeed — useful for a default or fallback policy assembled from more than one
smaller policy:

```csharp
var standardUserPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .RequireClaim("Status", "Active")
    .Build();

var managerPolicy = new AuthorizationPolicyBuilder()
    .RequireRole("Manager")
    .RequireClaim("Department", "Sales")
    .Build();

var combined = AuthorizationPolicy.Combine(standardUserPolicy, managerPolicy);
```
