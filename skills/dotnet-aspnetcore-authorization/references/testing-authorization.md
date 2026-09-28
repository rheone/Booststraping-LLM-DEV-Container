# Testing Authorization

Authorization logic splits into three layers worth testing at different granularity: one
requirement handler's own pass/fail logic, a whole policy's combined requirements, and an endpoint's
actual HTTP-level authorization wiring.

## Unit testing a single requirement handler

Construct an `AuthorizationHandlerContext` directly — no service provider, no HTTP pipeline needed:

```csharp
[Fact]
public async Task HandleRequirementAsync_UserMeetsMinimumAge_Succeeds()
{
    var handler = new MinimumAgeHandler();
    var claims = new[] { new Claim(ClaimTypes.DateOfBirth, DateTime.UtcNow.AddYears(-25).ToString("O")) };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
    var requirement = new MinimumAgeRequirement(18);
    var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

    await handler.HandleRequirementAsync(context, requirement);

    Assert.True(context.HasSucceeded);
}
```

For a resource-based handler, pass the resource object as the context's third constructor argument
instead of `null`, and assert against `context.HasSucceeded` the same way.

## Testing a policy's combined requirements end-to-end

To verify the *policy* itself — every `RequireX` call combining with AND, a custom
`IAuthorizationPolicyProvider`'s name-parsing logic — build a minimal `ServiceCollection` with the
real registration instead of hand-rolling a context:

```csharp
[Fact]
public async Task SeniorManagerPolicy_RequiresBothRoleAndClaim()
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddAuthorizationBuilder()
        .AddPolicy("SeniorManager", policy => policy.RequireRole("Manager").RequireClaim("seniority", "senior"));

    using var provider = services.BuildServiceProvider();
    var authorizationService = provider.GetRequiredService<IAuthorizationService>();

    var claims = new[] { new Claim(ClaimTypes.Role, "Manager") }; // missing the seniority claim
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var result = await authorizationService.AuthorizeAsync(principal, resource: null, "SeniorManager");

    Assert.False(result.Succeeded);
}
```

## Testing a resource-based check through the service

```csharp
[Fact]
public async Task SameAuthorPolicy_AuthorMatchesUser_Succeeds()
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddAuthorizationBuilder()
        .AddPolicy("SameAuthorPolicy", policy => policy.Requirements.Add(new SameAuthorRequirement()));
    services.AddSingleton<IAuthorizationHandler, DocumentAuthorizationHandler>();

    using var provider = services.BuildServiceProvider();
    var authorizationService = provider.GetRequiredService<IAuthorizationService>();

    var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "author-1")], "TestAuth"));
    var document = new Document { Author = "author-1" };

    var result = await authorizationService.AuthorizeAsync(principal, document, "SameAuthorPolicy");

    Assert.True(result.Succeeded);
}
```

## Integration testing endpoint-level authorization

For an `[Authorize]`/`RequireAuthorization()`-guarded endpoint, `WebApplicationFactory<TEntryPoint>`
with a fake authentication scheme exercises the actual middleware pipeline — challenge on missing
auth, forbid on a failed policy — without a real token issuer or cookie flow:

```csharp
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory loggerFactory, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "test-user"), new Claim(ClaimTypes.Role, "Administrator")], "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class AuthenticatedTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { }));
    }
}

[Fact]
public async Task Get_AdminEndpoint_WithTestPrincipal_ReturnsOk()
{
    var client = new AuthenticatedTestFactory().CreateClient();

    var response = await client.GetAsync("/admin/report");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

Set `DefaultAuthenticateScheme`/`DefaultChallengeScheme` to `"Test"` in the overridden configuration
so an `[Authorize]` attribute with no explicit scheme resolves to the fake handler.

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| A single requirement handler's pass/fail logic (including resource-based handlers) | Construct `AuthorizationHandlerContext` directly |
| A policy's combined requirements, or a custom `IAuthorizationPolicyProvider`'s name parsing | A real `ServiceCollection` with `AddAuthorizationBuilder`, calling `IAuthorizationService.AuthorizeAsync` |
| An endpoint's `[Authorize]`/`RequireAuthorization()` wiring, challenge/forbid HTTP behavior | `WebApplicationFactory` with a test authentication scheme issuing a fixed principal |

Reserve the `WebApplicationFactory` layer for tests that specifically care about the HTTP-level
outcome (401 vs. 403 vs. 200) — the policy/requirement logic itself is cheaper and faster to verify
directly through `IAuthorizationService` or a bare handler.
