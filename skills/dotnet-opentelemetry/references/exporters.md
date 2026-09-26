# Exporters

## What an exporter does

An exporter takes the processed traces/metrics/logs the SDK has collected and ships them somewhere
— a local process for development, or a backend for storage, querying, and alerting. Configure one
or more per signal, inside that signal's `WithTracing`/`WithMetrics`/`WithLogging` builder.

## OTLP exporter

The OpenTelemetry Protocol (OTLP) exporter (package `OpenTelemetry.Exporter.OpenTelemetryProtocol`,
current stable release **1.19.1** as of this writing) ships telemetry to any backend or collector
that speaks OTLP — the vendor-neutral wire protocol most backends and the OpenTelemetry Collector
itself accept:

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");
    options.Protocol = OtlpExportProtocol.Grpc; // or OtlpExportProtocol.HttpProtobuf
})
```

- Defaults to `http://localhost:4317` (gRPC) if `Endpoint` isn't set — typically a local
  OpenTelemetry Collector instance in development, or a sidecar/agent in production that forwards
  to wherever telemetry ultimately needs to land.
- `OtlpExportProtocol.Grpc` and `OtlpExportProtocol.HttpProtobuf` are both valid; which one a given
  collector or backend expects depends on how it's configured to receive OTLP — check the receiving
  end's configuration rather than assuming one.
- Environment-variable configuration (`OTEL_EXPORTER_OTLP_ENDPOINT`,
  `OTEL_EXPORTER_OTLP_PROTOCOL`, and per-signal variants) is also supported and often preferred for
  container/Kubernetes deployments, since it lets the endpoint vary by environment without a code
  change.

## Console exporter

`.AddConsoleExporter()` writes traces/metrics/logs to stdout as they're processed — no backend, no
network call, nothing to configure. Useful for confirming instrumentation actually produces the
data you expect (a new custom span appears, a new metric shows the expected value) before wiring up
a real exporter, or for a minimal local development loop. Never use it as an application's only
production exporter — it produces high-volume unstructured console output with no aggregation,
querying, or retention.

## Vendor-specific backends, in general

Beyond OTLP and console output, a vendor's own dedicated exporter package (or, more commonly today,
that vendor accepting OTLP directly, or via an OpenTelemetry Collector configured to forward to it)
is how telemetry reaches a specific paid or self-hosted observability backend for dashboards,
alerting, and long-term storage. Which package or endpoint configuration applies depends entirely on
which backend a project has chosen — check that backend's own documentation for its current
recommended OpenTelemetry .NET integration path (a dedicated exporter package versus OTLP-with-a-
specific-endpoint-and-headers) rather than assuming one shape applies universally; backends have
converged heavily on "just point the OTLP exporter at us" over maintaining bespoke exporter
packages, but the exact current recommendation is backend-specific and worth confirming per vendor.

## Multiple exporters at once

Each signal builder accepts more than one exporter — e.g. `AddConsoleExporter()` alongside
`AddOtlpExporter()` during local development, so telemetry is both visible immediately in the
terminal and shipped to a real backend for comparison. Every registered exporter for a signal
receives every item the SDK processes for that signal; there's no built-in per-exporter filtering
beyond what each exporter's own options expose.
