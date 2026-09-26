# Dependency Injection in Requirement Handlers

An authorization handler is registered in the service container like any other service, so it can
declare constructor dependencies exactly like a controller or a hosted service — a rule repository,
a database context, `ILoggerFactory` — and the container resolves them the same way.

```csharp
public sealed class SampleAuthorizationHandler : AuthorizationHandler<SampleRequirement>
{
    private readonly ILogger _logger;

    public SampleAuthorizationHandler(ILoggerFactory loggerFactory) =>
        _logger = loggerFactory.CreateLogger(GetType().FullName);

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SampleRequirement requirement)
    {
        _logger.LogInformation("Inside my handler");
        // ...
        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddSingleton<IAuthorizationHandler, SampleAuthorizationHandler>();
```

A handler can be registered with any service lifetime — singleton, scoped, or transient — the same
lifetime rules that apply to any other DI-resolved service apply here too. The one lifetime pitfall
specific to this scenario: don't register a handler that depends on Entity Framework Core as a
singleton. EF Core's `DbContext` is meant to be scoped to a single unit of work; a singleton handler
holding a long-lived reference to a scoped `DbContext` (injected once at startup and reused for
every subsequent request) reuses that same context instance across every request's authorization
check, which is exactly the concurrency and stale-tracking failure mode scoped-lifetime services
exist to prevent. Register a handler that needs EF Core as scoped instead.
