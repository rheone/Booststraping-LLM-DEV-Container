# Core Concepts

Serilog builds a logging pipeline through a fluent `LoggerConfiguration`: you declare a minimum
level, attach enrichers, attach sinks, then call `CreateLogger()` once to produce an immutable
`ILogger` that every log call flows through.

## LoggerConfiguration and the static Log.Logger

The most common bootstrap shape assigns the built logger to the static `Log.Logger` property,
making `Log.Information(...)`, `Log.Warning(...)`, etc. available from anywhere without
constructor injection:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Application starting");
```

`Log.Logger` defaults to a no-op `SilentLogger` until you assign it — logging calls before
`CreateLogger()` runs silently rather than throwing, which is easy to miss if `Log.Logger` is
never actually assigned in some code path (e.g. a misconfigured test host).

Always flush on shutdown:

```csharp
Log.CloseAndFlush();
```

Skipping this risks losing buffered events in sinks that batch writes (file, most network sinks) —
put it in a `finally` block around `app.Run()`, or rely on `Host.CreateDefaultBuilder`'s
`UseSerilog` integration, which wires the flush into host shutdown automatically (see
[aspnetcore-integration.md](aspnetcore-integration.md)).

## Injected ILogger vs. the static Log class

Prefer injecting `Serilog.ILogger` (or the framework-agnostic `Microsoft.Extensions.Logging.
ILogger<T>`, which Serilog implements via its `Microsoft.Extensions.Logging` provider) into classes
that need to log, over calling the static `Log` class from deep inside application code:

```csharp
public sealed class OrderService(ILogger<OrderService> logger)
{
    public void Create(Order order)
    {
        logger.LogInformation("Order {OrderId} created for {CustomerId}", order.Id, order.CustomerId);
    }
}
```

Injected loggers are trivially fakeable/substitutable in unit tests and make a class's logging
dependency an explicit constructor parameter instead of a hidden global; reserve the static `Log`
class for bootstrap code (`Program.cs`) and genuinely global infrastructure that predates or
outlives the DI container's lifetime.

## Two-stage initialization

Configure a minimal bootstrap logger before the host/DI container exists (so startup failures are
still logged), then replace it with the fully configured logger once configuration and DI are
available:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var app = builder.Build();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

`CreateBootstrapLogger()` (rather than `CreateLogger()`) marks the logger as replaceable —
`UseSerilog` swaps it out for the fully configured logger once the host builds it, without losing
whatever the bootstrap logger already emitted.

## Configuring from appsettings.json

`ReadFrom.Configuration(...)` (from the `Serilog.Settings.Configuration` package) lets sinks,
minimum levels, and enrichers live in configuration instead of code:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft.AspNetCore": "Warning" }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/app-.log", "rollingInterval": "Day" } }
    ],
    "Enrich": ["FromLogContext", "WithMachineName"]
  }
}
```

This is the standard shape for anything beyond a fixed, hardcoded pipeline — it lets ops change
verbosity or sink destinations per environment without a code change or redeploy.
