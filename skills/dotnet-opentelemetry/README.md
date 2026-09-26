# OpenTelemetry for .NET

Guidance on OpenTelemetry for .NET, a vendor-neutral observability SDK covering traces, metrics, and
logs — the routing table (by situation, not by version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per version

| File | Covers |
| --- | --- |
| `core-concepts.md` | The three signals, package layout, `AddOpenTelemetry()`, `ConfigureResource`, `WithTracing`/`WithMetrics`/`WithLogging` |
| `instrumentation-libraries.md` | `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, other `OpenTelemetry.Instrumentation.*` packages |
| `custom-tracing.md` | `ActivitySource`, `Activity`, tags, events, status, parent/child span relationships |
| `custom-metrics.md` | `Meter`, `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>`, `ObservableGauge<T>`, tag cardinality |
| `exporters.md` | `AddOtlpExporter`, `AddConsoleExporter`, vendor-specific backends in general |
| `context-propagation.md` | W3C Trace Context, `traceparent`, `Baggage`, non-HTTP transport propagation |
| `testing-with-opentelemetry.md` | `ActivityListener`, `MeterListener`, verifying your own instrumentation without a real exporter |

## Scope

The `OpenTelemetry`/`OpenTelemetry.Api`/`OpenTelemetry.Extensions.Hosting` SDK packages, official
`OpenTelemetry.Instrumentation.*` auto-instrumentation packages, and the built-in `OpenTelemetryProtocol`
and console exporters. Out of scope: the OpenTelemetry Collector's own configuration, any specific
observability backend's product-specific features, and sampling-strategy tuning beyond why
`StartActivity` can return `null`.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
1.19.1.
