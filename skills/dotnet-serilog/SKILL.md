---
name: dotnet-serilog
description: Guidance on Serilog (verified current release 4.4.0, Apache-2.0), the structured logging library for .NET — LoggerConfiguration setup and the fluent configuration pipeline, sinks and their configuration (console, file, and the general sink-configuration shape), structured/message-template logging with @ destructuring, enrichers (thread/environment/property enrichment, custom ILogEventEnricher), log levels and per-source-context minimum-level overrides, sub-loggers and event filtering, and ASP.NET Core request logging integration via Serilog.AspNetCore (verified current release 10.0.0) and UseSerilogRequestLogging. Use when configuring Serilog, choosing or wiring a sink, writing structured log messages, deciding what to enrich logs with, silencing noisy log sources, or integrating Serilog into an ASP.NET Core host's logging pipeline.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Serilog

Guidance on Serilog, the structured logging library for .NET built around named properties in log
events rather than plain-text messages. Organized by task, not by version — the core
`LoggerConfiguration` API has been stable across major versions; each reference file notes a
version-introduced fact inline where it matters.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Setting up `LoggerConfiguration`, `Log.Logger`, and the overall pipeline shape | [references/core-concepts.md](references/core-concepts.md) |
| Writing log messages with named properties and `@` destructuring | [references/structured-logging.md](references/structured-logging.md) |
| Configuring where log events go (console, file, and sinks generally) | [references/sinks.md](references/sinks.md) |
| Attaching contextual data (machine name, thread id, custom properties) to every event | [references/enrichers.md](references/enrichers.md) |
| Silencing a noisy namespace, or changing verbosity for one source at runtime | [references/levels-and-filtering.md](references/levels-and-filtering.md) |
| Routing events to different sinks or minimum levels via sub-loggers | [references/levels-and-filtering.md](references/levels-and-filtering.md) |
| Wiring Serilog into an ASP.NET Core host, or logging one structured line per HTTP request | [references/aspnetcore-integration.md](references/aspnetcore-integration.md) |
| Asserting on what a piece of code logged | [references/testing.md](references/testing.md) |

## Quick start

A minimal `LoggerConfiguration`, current API (v4.4.0):

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Order {OrderId} created for {CustomerId} totaling {Total:C}", orderId, customerId, total);
}
finally
{
    Log.CloseAndFlush();
}
```

The single most common miss: logging with string concatenation or interpolation
(`Log.Information($"Order {orderId} created")`) instead of a message template with named
placeholders — this throws away the structured properties Serilog exists to capture. See
[structured-logging.md](references/structured-logging.md).

## Out of scope

- Other logging libraries (NLog, log4net, the built-in `Microsoft.Extensions.Logging` console
  provider) — not documented here; this skill is Serilog-only.
- Distributed tracing / OpenTelemetry span correlation as its own concern — out of scope beyond
  what a Serilog enricher can attach to a log event; this skill does not cover configuring a
  tracing pipeline itself.
- Log aggregation platform setup (configuring Seq, Elasticsearch, Splunk, or any other backend
  service that a sink sends events to) — covered only from Serilog's side (how to configure the
  sink that sends to it), never the backend's own administration.
