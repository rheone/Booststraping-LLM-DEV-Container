# OpenTelemetry for .NET

OpenTelemetry for .NET is a vendor-neutral observability SDK spanning traces, metrics, and logs.
This skill covers wiring up the SDK, adding instrumentation, creating your own spans and metrics,
and exporting telemetry to a backend.

## When to reach for it

- Wiring `AddOpenTelemetry()`/`WithTracing`/`WithMetrics`/`WithLogging` into a new service.
- Adding an instrumentation library like `AddAspNetCoreInstrumentation` or
  `AddHttpClientInstrumentation` and understanding what it captures automatically.
- Creating a custom span with `ActivitySource`/`Activity` or a custom metric with `Meter`.
- Debugging a distributed trace that's broken or disconnected across service boundaries.
- Choosing and configuring an exporter (OTLP, console, or a vendor-specific backend).

## Using it

This skill fires automatically when your request involves OpenTelemetry setup, instrumentation, or
distributed tracing in a .NET service. You can also invoke it directly with
`/dotnet-opentelemetry`.

## What it covers

| Topic | Reference |
| --- | --- |
| The three signals, `AddOpenTelemetry()`, `ConfigureResource` | [references/core-concepts.md](references/core-concepts.md) |
| `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, other instrumentation packages | [references/instrumentation-libraries.md](references/instrumentation-libraries.md) |
| `ActivitySource`/`Activity`, tags, events, span status | [references/custom-tracing.md](references/custom-tracing.md) |
| `Meter`, `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>`, `ObservableGauge<T>` | [references/custom-metrics.md](references/custom-metrics.md) |
| `AddOtlpExporter`, `AddConsoleExporter`, vendor backends | [references/exporters.md](references/exporters.md) |
| W3C Trace Context, `traceparent`, `Baggage` propagation | [references/context-propagation.md](references/context-propagation.md) |
| Verifying instrumentation with `ActivityListener`/`MeterListener` | [references/testing-with-opentelemetry.md](references/testing-with-opentelemetry.md) |

## Example prompts

- "Add OpenTelemetry tracing and metrics to this ASP.NET Core API and export via OTLP."
- "I need a custom span around this database call with a tag for the query name."
- "Traces from service A never show up as children of service B's traces. What's wrong with
  context propagation?"
