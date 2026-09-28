# Core concepts and SDK setup

## The three signals

OpenTelemetry defines three observability signals, each with its own API and SDK surface but a
shared configuration and export model:

- **Traces** — a request's journey through a system, represented as a tree of `Activity` instances
  (OpenTelemetry's tracing signal maps onto .NET's built-in `System.Diagnostics.Activity` type
  rather than introducing a parallel span type). See `references/custom-tracing.md`.
- **Metrics** — numeric measurements aggregated over time (counts, durations, sizes), recorded
  through `System.Diagnostics.Metrics.Meter` and its instrument types. See
  `references/custom-metrics.md`.
- **Logs** — structured log records, correlated to the active trace/span when one exists so a log
  line and the request it happened during can be cross-referenced.

All three share exporters (`references/exporters.md`) and context propagation
(`references/context-propagation.md`); a service typically wires up all three from one place at
startup.

## Package layout

- `OpenTelemetry.Api` — the vendor-neutral API types (`ActivitySource`/`Activity` come from the BCL
  itself; this package adds `Baggage` and other OpenTelemetry-specific API surface).
- `OpenTelemetry` — the SDK: the actual processing pipeline (processors, samplers, resource
  detection) that turns API calls into exported data. An app with no SDK configured still compiles
  and runs against the API, but nothing is exported anywhere.
- `OpenTelemetry.Extensions.Hosting` — `AddOpenTelemetry()` as an extension on
  `IServiceCollection`/`IHostBuilder`, the normal entry point for an ASP.NET Core or generic-host
  application.

## Wiring up the SDK: `AddOpenTelemetry()`

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "orders-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("OrdersApi")               // your own ActivitySource name(s)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("OrdersApi")                // your own Meter name(s)
        .AddOtlpExporter())
    .WithLogging(logging => logging
        .AddOtlpExporter());

var app = builder.Build();
```

- `ConfigureResource` sets attributes describing *this* service (name, version, instance id) that
  get attached to every exported trace, metric, and log — set `AddService(serviceName:)` at
  minimum; without it, exported telemetry from multiple services is hard to tell apart at the
  backend.
- `WithTracing`/`WithMetrics`/`WithLogging` each take a builder for that signal: instrumentation
  libraries (`references/instrumentation-libraries.md`), your own `ActivitySource`/`Meter` names
  (`AddSource`/`AddMeter` — an SDK opt-in list; an `Activity`/instrument from a source not named
  here is created by your code but never collected or exported), and one or more exporters
  (`references/exporters.md`).
- Logging integrates slightly differently: `WithLogging` configures the SDK's log processing, but
  the actual `ILogger` calls your code already makes are picked up automatically once
  `AddOpenTelemetry()` is wired into the host's logging pipeline — there's no separate "add my
  logger category" opt-in list the way `AddSource`/`AddMeter` require for traces/metrics.

## Why `AddSource`/`AddMeter` are opt-in

The SDK doesn't collect every `Activity`/instrument that exists in the process by default — only
the ones from sources explicitly named via `AddSource`/`AddMeter` (plus whatever instrumentation
libraries register their own sources internally). This keeps a service in control of exactly what
gets collected and exported, rather than every dependency's incidental internal tracing/metrics
work becoming exported telemetry a service didn't ask for.
