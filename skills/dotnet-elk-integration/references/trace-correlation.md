# Trace Correlation

A log line is far more useful during an incident investigation when it can be pivoted to the exact
distributed trace/span it occurred within — Kibana's APM/observability views rely on the ECS
`trace.id`/`transaction.id` fields being present on log documents to make that pivot possible.

## Why correlation needs explicit wiring

Serilog has no built-in concept of a distributed trace — trace/span identifiers come from whatever
distributed-tracing mechanism the application already uses (the .NET `System.Diagnostics.Activity`
API, which underlies both `System.Diagnostics.DiagnosticSource` instrumentation and the
OpenTelemetry .NET SDK). Correlation means enriching every log event with the current `Activity`'s
trace/span identifiers at the point the log is written.

## Enriching logs with the current Activity's trace/span IDs

```csharp
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace.id", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("transaction.id", activity.SpanId.ToString()));
    }
}
```

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.With<ActivityEnricher>()
    .Enrich.FromLogContext()
    // ...
    .CreateLogger();
```

- `Activity.Current` is ambient per logical call context (it flows across `await` boundaries the
  same way `AsyncLocal<T>` does) — as long as the enricher runs at the point a log event is created
  (which it does, since enrichers run per-event), it picks up whichever trace/span is active for the
  code path currently executing the log call, with no manual passing of trace context required.
- `trace.id`/`transaction.id` are the ECS field names Kibana's APM/log correlation views look for —
  using these exact names (rather than an ad hoc `TraceId`/`SpanId` property name) is what makes the
  "view related logs" pivot in Kibana's trace view work without additional configuration.

## ASP.NET Core: the Activity is already there

In an ASP.NET Core application, `Activity.Current` is already populated per-request by the hosting
pipeline's own instrumentation (via `DiagnosticSource`), whether or not the application explicitly
started tracing itself — the enricher above works for any ASP.NET Core request without additional
setup beyond registering the enricher, since the ambient `Activity` already exists by the time
request-handling code runs.

## When the project already uses the OpenTelemetry .NET SDK

If tracing already flows through the OpenTelemetry .NET SDK, `Activity.Current` is still the
correct source for trace/span IDs — OpenTelemetry's .NET SDK builds directly on
`System.Diagnostics.Activity` rather than introducing its own parallel context type, so the same
enricher pattern applies whether the underlying tracing exporter is Elastic APM, Jaeger, or any
other OpenTelemetry-compatible backend.

## Common pitfall

Reading trace/span IDs only at the point a request starts (e.g. capturing them once into a field on
a request-scoped object) rather than reading `Activity.Current` fresh at each log event misses
nested spans — a log line emitted from deep inside a request that started its own child span (a
database call, an outbound HTTP call) should correlate to *that* child span's ID, not the
top-level request span's ID, which is exactly what reading `Activity.Current` per log event
(rather than once per request) gets right.
