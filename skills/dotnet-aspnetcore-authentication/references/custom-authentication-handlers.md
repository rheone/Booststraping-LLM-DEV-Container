# Custom Authentication Handlers

When no built-in scheme fits — an API-key header, a signature-based scheme specific to one
integration partner — implement `AuthenticationHandler<TOptions>` directly rather than forcing the
scenario into `AddJwtBearer` or `AddCookie`.

## Implementing a handler

```csharp
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";
}

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IApiKeyStore apiKeyStore)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var providedKey))
        {
            return AuthenticateResult.NoResult();
        }

        var client = await apiKeyStore.FindByKeyAsync(providedKey.ToString());
        if (client is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, client.Id) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
```

- **`AuthenticateResult.NoResult()`** — this handler found nothing relevant to this request (no
  header present); a different scheme may still authenticate it. Distinct from `Fail`, which means
  this handler found a credential and it was invalid.
- **`AuthenticateResult.Fail(...)`** — a credential was present but rejected. In a single-scheme
  setup this and `NoResult()` behave the same downstream (both leave the request unauthenticated),
  but they matter in a multi-scheme setup where a subsequent scheme might still succeed after this
  one returns `NoResult()`.

## Registering a custom scheme

```csharp
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        "ApiKey", options => options.HeaderName = "X-Api-Key");
```

`AddScheme<TOptions, THandler>` is the generic registration method every built-in `Add<Scheme>`
extension (`AddCookie`, `AddJwtBearer`) itself wraps — writing a custom handler uses the same
registration surface the framework's own schemes do.

## Overriding challenge/forbid behavior

Override `HandleChallengeAsync`/`HandleForbiddenAsync` when the default 401/403 status-code response
isn't right for this scheme — for example, returning a scheme-specific error body:

```csharp
protected override Task HandleChallengeAsync(AuthenticationProperties properties)
{
    Response.StatusCode = StatusCodes.Status401Unauthorized;
    Response.Headers.Append("WWW-Authenticate", $"ApiKey realm=\"{Scheme.Name}\"");
    return Task.CompletedTask;
}
```

## When a custom handler is the wrong tool

A requirement that's really "run some extra logic after a built-in scheme authenticates" (enriching
claims, rejecting based on a database check) belongs in `IClaimsTransformation` or a policy
requirement handler instead — see [claims-and-principals.md](claims-and-principals.md) and
[policy-based-authorization.md](policy-based-authorization.md). Reach for a custom
`AuthenticationHandler<TOptions>` only when the credential-extraction and validation mechanism itself
(reading and interpreting the actual header/token/signature) has no built-in scheme that already does
it.
