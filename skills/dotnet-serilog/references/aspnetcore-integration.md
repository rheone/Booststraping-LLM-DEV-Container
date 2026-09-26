# ASP.NET Core Integration

The `Serilog.AspNetCore` package (verified current release 10.0.0, supporting .NET 9 and .NET 10
including Native AOT) wires Serilog into the generic host's logging abstraction and adds
structured, single-line-per-request HTTP logging.

## UseSerilog: replacing the default logging provider

Call `UseSerilog` on the host builder so every `ILogger<T>` resolved through
`Microsoft.Extensions.Logging` — including the framework's own internal logging — flows through
Serilog's pipeline instead of the default console provider:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var app = builder.Build();
```

`ReadFrom.Services(services)` lets registered `IDestructuringPolicy`/enricher implementations
resolve through DI — needed for any custom enricher with its own constructor dependencies (see
[enrichers.md](enrichers.md)). Combine this with the two-stage bootstrap-logger pattern described
in [core-concepts.md](core-concepts.md) so startup failures before the host finishes building are
still captured.

## UseSerilogRequestLogging: one structured line per request

Add the request-logging middleware early in the pipeline — before `UseRouting`/MVC/endpoint
handlers — so it can time the full request:

```csharp
app.UseSerilogRequestLogging();

app.UseRouting();
app.MapControllers();
```

This replaces ASP.NET Core's default per-request framework log noise (typically several log lines
per request across multiple categories) with a single structured completion event per request,
containing the request method, path, status code, and elapsed time as properties. Placing it after
`UseRouting`/endpoint handlers means it won't time or log whatever ran before it in the pipeline —
it must come first among logging-relevant middleware.

## Customizing the request-logging event

`UseSerilogRequestLogging` accepts an options delegate for the message template, the level per
outcome, and additional properties:

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.GetLevel = (httpContext, elapsed, ex) => ex is not null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode > 499
            ? LogEventLevel.Error
            : LogEventLevel.Information;

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});
```

`EnrichDiagnosticContext` is the correct extension point for adding per-request properties to the
completion event specifically — it runs once per request and only affects this one event, unlike
`Enrich.FromLogContext()` combined with `LogContext.PushProperty`, which affects every event
emitted during the request's lifetime, not just the completion summary.

## Silencing noisy or irrelevant requests

Combine `MinimumLevel.Override` (for framework namespaces) with a `Filter.ByExcluding` on the
`RequestPath` property (for specific routes like health checks) — see
[levels-and-filtering.md](levels-and-filtering.md) for the concrete pattern. Do not try to prevent
`UseSerilogRequestLogging` from running for specific routes via middleware branching; filtering the
emitted event is simpler and keeps the timing/completion behavior consistent for every request.

## Ensuring shutdown flush

`UseSerilog` registers a hook so `Log.CloseAndFlush()` runs automatically as part of host shutdown
— you do not need to call it manually in `Program.cs` when using `UseSerilog`, only when managing
`Log.Logger` entirely outside the generic host (a console app with no `IHost`, for instance).
