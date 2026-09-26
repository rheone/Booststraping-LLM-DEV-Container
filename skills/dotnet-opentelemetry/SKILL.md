---
name: dotnet-opentelemetry
description: Guidance on OpenTelemetry for .NET, a vendor-neutral observability SDK covering traces, metrics, and logs (current stable release 1.19.1). Covers AddOpenTelemetry()/WithTracing/WithMetrics/WithLogging SDK setup, instrumentation libraries (AddAspNetCoreInstrumentation, AddHttpClientInstrumentation, and other OpenTelemetry.Instrumentation.* auto-instrumentation packages), creating custom spans via ActivitySource/Activity (System.Diagnostics), custom metrics via Meter/Counter/Histogram/UpDownCounter/ObservableGauge (System.Diagnostics.Metrics), exporters (OTLP via AddOtlpExporter, console via AddConsoleExporter, and the general concept of vendor-specific backends), context propagation across service boundaries (W3C Trace Context, traceparent headers, Baggage), and testing instrumentation with ActivityListener/MeterListener. Use when adding or reviewing tracing/metrics/logging instrumentation in a .NET service, wiring up the OpenTelemetry SDK, or debugging a broken/disconnected distributed trace.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# OpenTelemetry for .NET

Guidance on OpenTelemetry for .NET, a vendor-neutral observability SDK spanning traces, metrics, and
logs. Current stable release as of this writing: **1.19.1** (core SDK and API packages;
instrumentation packages such as `OpenTelemetry.Instrumentation.AspNetCore` version independently —
see `references/instrumentation-libraries.md`). Organized by concern/topic, not by version — each
reference file notes a version-introduced fact inline where relevant.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Wiring up telemetry for the first time, or reviewing startup configuration | `AddOpenTelemetry()`, `ConfigureResource`, `WithTracing`/`WithMetrics`/`WithLogging` | [references/core-concepts.md](references/core-concepts.md) |
| Getting spans/metrics for a framework or client automatically | `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, other `OpenTelemetry.Instrumentation.*` packages | [references/instrumentation-libraries.md](references/instrumentation-libraries.md) |
| Making your own code's steps visible in a trace | `ActivitySource`, `Activity`, tags, events, status | [references/custom-tracing.md](references/custom-tracing.md) |
| Recording a count, duration, or other numeric measurement | `Meter`, `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>`, `ObservableGauge<T>` | [references/custom-metrics.md](references/custom-metrics.md) |
| Getting telemetry to a collector or backend | `AddOtlpExporter`, `AddConsoleExporter`, vendor-backend concepts in general | [references/exporters.md](references/exporters.md) |
| A trace breaks across a service call, or you need to carry custom context along | W3C Trace Context, `traceparent`, `Baggage`, non-HTTP transport propagation | [references/context-propagation.md](references/context-propagation.md) |
| Verifying your own spans/metrics actually get produced correctly | `ActivityListener`, `MeterListener`, in-process assertions without a real exporter | [references/testing-with-opentelemetry.md](references/testing-with-opentelemetry.md) |

## Quick start

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "orders-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("OrdersApi")
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddMeter("OrdersApi")
        .AddOtlpExporter());
```

```csharp
private static readonly ActivitySource ActivitySource = new("OrdersApi");

using var activity = ActivitySource.StartActivity("ProcessOrder");
activity?.SetTag("order.id", order.Id);
```

## Out of scope

- The OpenTelemetry Collector itself (its own configuration, pipelines, and deployment) — this
  skill covers the .NET SDK and application-side instrumentation that sends telemetry toward a
  collector or backend, not the collector's own operation.
- Any specific observability backend's dashboards, query language, or alerting configuration —
  covered generically as "a vendor-specific backend" in `references/exporters.md`, never by naming
  or assuming a specific product.
- Sampling strategy tuning beyond what's needed to explain why `StartActivity` can return `null` —
  a deep topic (head vs. tail sampling, adaptive sampling) with configuration that varies by SDK
  version and backend.
