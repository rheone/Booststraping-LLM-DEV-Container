# Testing

Authentication and authorization logic splits into two layers worth testing differently: the
authorization *decision* (does this policy/role/claim combination pass?) and the HTTP-level pipeline
wiring (does an unauthenticated request actually get challenged, does an authenticated one reach the
endpoint?).

## Unit testing a custom requirement handler

A requirement handler is a plain class — build an `AuthorizationHandlerContext` directly rather than
going through the full authorization service:

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

    context.HasSucceeded.Should().BeTrue();
}
```

## Testing a policy end-to-end with a real AuthorizationService

For a test that wants to verify the policy itself (registered requirements, AND semantics across
multiple `RequireX` calls) rather than one handler in isolation, build a minimal
`ServiceCollection` with the real policy registration:

```csharp
[Fact]
public async Task SeniorManagerPolicy_RequiresBothRoleAndClaim()
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddAuthorizationBuilder()
        .AddPolicy("SeniorManager", policy => policy
            .RequireRole("Manager")
            .RequireClaim("seniority", "senior"));

    using var provider = services.BuildServiceProvider();
    var authorizationService = provider.GetRequiredService<IAuthorizationService>();

    var claims = new[] { new Claim(ClaimTypes.Role, "Manager") }; // missing the seniority claim
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    var result = await authorizationService.AuthorizeAsync(principal, "SeniorManager");

    result.Succeeded.Should().BeFalse();
}
```

## Integration testing endpoints with WebApplicationFactory

For controller or minimal API endpoints guarded by `[Authorize]`/`RequireAuthorization()`, use
`WebApplicationFactory<TEntryPoint>` and replace the real authentication scheme with a test scheme
that issues a known, fixed principal — this exercises the actual middleware pipeline (challenge on
missing auth, forbid on failed policy) without needing a real token issuer or cookie flow in tests:

```csharp
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory loggerFactory, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "test-user"), new Claim(ClaimTypes.Role, "Administrator") };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class AuthenticatedTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}

[Fact]
public async Task Get_AdminEndpoint_WithTestPrincipal_ReturnsOk()
{
    var client = new AuthenticatedTestFactory().CreateClient();
    client.DefaultRequestHeaders.Add("Authorization", "Bearer test");

    var response = await client.GetAsync("/admin/report");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

Set `DefaultAuthenticateScheme`/`DefaultChallengeScheme` to `"Test"` in the test host's
configuration override so `[Authorize]` attributes with no explicit scheme resolve to the fake
handler.

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| A single requirement handler's pass/fail logic | Construct `AuthorizationHandlerContext` directly |
| A policy's combined requirements (AND semantics across `RequireX` calls) | A real `ServiceCollection` with `AddAuthorizationBuilder`, calling `IAuthorizationService.AuthorizeAsync` |
| An endpoint's `[Authorize]`/`RequireAuthorization()` wiring, challenge/forbid HTTP behavior | `WebApplicationFactory` with a test authentication scheme issuing a fixed principal |

Reserve the `WebApplicationFactory` layer for tests that specifically want to verify the HTTP-level
outcome (401 vs. 403 vs. 200) — policy logic itself is cheaper and faster to verify at the
`IAuthorizationService` layer.
