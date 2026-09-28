# Testing

Testing an OpenIddict-based server splits into testing your own endpoint handlers' logic in
isolation and integration-testing the actual token endpoints end-to-end — the latter is where most
of the value is, since the bulk of the behavior worth verifying (does the code exchange actually
work, is PKCE actually enforced) lives in the full request/response round trip.

## Integration testing the token endpoint with WebApplicationFactory

Stand up the app with `WebApplicationFactory<TEntryPoint>`, pre-register a test client application
through `IOpenIddictApplicationManager`, and drive the flow with plain HTTP requests — this exercises
OpenIddict's real validation logic (PKCE, redirect URI matching, grant type permissions) rather than
mocking it away:

```csharp
public sealed class OpenIddictTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("openiddict-tests").UseOpenIddict());
        });
    }
}

[Fact]
public async Task TokenEndpoint_ClientCredentials_ReturnsAccessToken()
{
    await using var factory = new OpenIddictTestFactory();
    using var scope = factory.Services.CreateScope();
    var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
    {
        ClientId = "test-client",
        ClientSecret = "test-secret",
        ClientType = ClientTypes.Confidential,
        Permissions = { Permissions.Endpoints.Token, Permissions.GrantTypes.ClientCredentials },
    });

    var client = factory.CreateClient();
    var response = await client.PostAsync("connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = "test-client",
        ["client_secret"] = "test-secret",
    }));

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
    payload.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
}
```

Use an in-memory Entity Framework Core provider (or SQLite in-memory, for a closer-to-production
storage engine) for the test database — OpenIddict's application/token entities need a real, queried
store, not a mocked manager, since the flow's correctness depends on data actually round-tripping
through it.

## Testing the authorization code + PKCE flow

Verify PKCE enforcement specifically, since a misconfigured `RequireProofKeyForCodeExchange()` (or a
test accidentally skipping the code verifier) is the most common flow-specific bug:

```csharp
[Fact]
public async Task TokenEndpoint_AuthorizationCode_WithoutCodeVerifier_Fails()
{
    // Register a public client requiring PKCE, drive /connect/authorize to obtain a code
    // (following redirects and capturing the code from the callback), then:
    var response = await client.PostAsync("connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "authorization_code",
        ["code"] = capturedCode,
        ["client_id"] = "public-client",
        ["redirect_uri"] = "https://client.example.com/callback",
        // "code_verifier" intentionally omitted
    }));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

## Testing your own endpoint handler logic in isolation

Logic you write inside a passthrough endpoint handler (mapping a user's roles onto claims, checking
account status before reissuing a refresh token) is ordinary code once you extract it from the
endpoint delegate — pull it into a plain method or class and unit test it directly rather than
routing every case through the full HTTP pipeline:

```csharp
[Fact]
public void BuildIdentity_ForActiveUser_IncludesRoleClaims()
{
    var user = new ApplicationUser { Id = "user-1", Email = "a@b.com" };
    var identity = ClaimsIdentityBuilder.Build(user, roles: ["Administrator"], scopes: ["api"]);

    identity.GetClaim(Claims.Subject).Should().Be("user-1");
    identity.FindAll(Claims.Role).Select(c => c.Value).Should().Contain("Administrator");
}
```

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| Your own claim-mapping/business logic inside an endpoint handler | Extract it to a plain method/class and unit test directly |
| A single flow's grant-type handling and success response shape | `WebApplicationFactory` driving the real token endpoint with a registered test client |
| PKCE, redirect URI matching, or per-client permission enforcement | `WebApplicationFactory` with deliberately invalid requests (missing verifier, mismatched URI, unpermitted grant type), asserting the failure |

Most of what's worth testing here lives at the integration layer — OpenIddict's validation rules are
the product being exercised, and mocking `IOpenIddictApplicationManager`/`IOpenIddictTokenManager`
to fake their outcomes tests your mock's behavior, not OpenIddict's actual enforcement.
