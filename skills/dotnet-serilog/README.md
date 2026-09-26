# Serilog

Serilog is the structured logging library for .NET built around named properties in log events
rather than plain-text messages. This skill covers configuring `LoggerConfiguration`, wiring up
sinks and enrichers, and integrating Serilog into an ASP.NET Core host.

## When to reach for it

- Setting up `LoggerConfiguration` for a new project or deciding between a static `Log.Logger` and
  an injected logger.
- Choosing or configuring a sink (console, file, rolling files) and getting its options right.
- Writing a log message and deciding what to destructure with `@` versus stringify with `$`.
- Silencing a noisy log source with a per-source-context minimum-level override or a sub-logger.
- Wiring `UseSerilogRequestLogging` into an ASP.NET Core pipeline.

## Using it

This skill fires automatically when your request matches Serilog configuration, sinks, structured
logging, or enrichment. You can also invoke it directly with `/dotnet-serilog`.

## What it covers

| Topic | Reference |
| --- | --- |
| `LoggerConfiguration`, `Log.Logger`, static vs. injected logger | [references/core-concepts.md](references/core-concepts.md) |
| Message templates, named properties, `@`/`$` destructuring | [references/structured-logging.md](references/structured-logging.md) |
| Console/file sinks, rolling files, general sink shape | [references/sinks.md](references/sinks.md) |
| Built-in and custom `ILogEventEnricher` enrichment | [references/enrichers.md](references/enrichers.md) |
| Minimum level, per-source overrides, sub-loggers, filtering | [references/levels-and-filtering.md](references/levels-and-filtering.md) |
| `UseSerilog`, `UseSerilogRequestLogging`, two-stage init | [references/aspnetcore-integration.md](references/aspnetcore-integration.md) |
| Asserting on emitted log events without brittle string checks | [references/testing.md](references/testing.md) |

## Example prompts

- "Set up Serilog to log to the console and a rolling file, and enrich every event with the
  machine name."
- "Our ASP.NET Core app logs every health check ping at Information. How do I quiet that down?"
- "How do I write a test that asserts a specific structured property was logged?"
