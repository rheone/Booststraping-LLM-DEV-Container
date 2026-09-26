# Endpoint Configuration

## Mapping the endpoint

`MapHealthChecks(pattern)` on the endpoint-routing builder exposes registered checks at a URL:

```csharp
app.MapHealthChecks("/health");
```

With no `HealthCheckOptions`, this runs every registered check and returns a plaintext body (the
string value of the overall `HealthStatus`) with an HTTP status code of 200 for `Healthy`/`Degraded`
and 503 for `Unhealthy`.

## Customizing status codes

`HealthCheckOptions.ResultStatusCodes` remaps any of the three `HealthStatus` values to a different
HTTP status code:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
});
```

## Customizing the response body

The default response writer emits only a plaintext status string. Set
`HealthCheckOptions.ResponseWriter` to produce a structured body (typically JSON) that includes each
individual check's name, status, description, duration, and data:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (httpContext, report) =>
    {
        httpContext.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
            }),
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload));
    },
});
```

There's no built-in structured JSON format — the framework deliberately leaves this open since the
right shape depends on whatever's consuming the endpoint (a monitoring dashboard, an orchestrator that
only reads the status code, a custom uptime check).

## Caching headers

By default the middleware sets `Cache-Control`, `Expires`, and `Pragma` headers to prevent the
response from being cached anywhere along the way — a stale cached health response defeats the
purpose of a live check. Set `AllowCachingResponses = true` only if a specific scenario genuinely
needs the response cacheable:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = true,
});
```

## Restricting access to the endpoint

Health check endpoints often need to be reachable by infrastructure (an orchestrator, a load
balancer) without full application authentication, but exposing detailed dependency status publicly
can itself be an information leak. Common options, applied to the mapped endpoint like any other
routed endpoint:

```csharp
app.MapHealthChecks("/health")
    .RequireAuthorization(); // require the app's normal auth for this endpoint

app.MapHealthChecks("/health")
    .RequireHost("*:5001"); // only answer on a specific port, e.g. an internal-only management port
```

Combine `RequireHost` with `RequireAuthorization` when a port restriction alone isn't sufficient
protection against host/port spoofing.
