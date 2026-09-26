---
name: dotnet-elk-integration
description: Guidance on integrating a .NET application with the Elastic Stack (Elasticsearch/Kibana) for centralized structured logging — the current Elastic-maintained shipping path via the Elastic.Serilog.Sinks package (verified current release 9.0.0, requires Elasticsearch 8.x+), the community-maintained Serilog.Sinks.Elasticsearch alternative, Elastic Common Schema (ECS) field mapping for structured log properties, data-stream index naming/rollover via Index Lifecycle Management (ILM), and correlating logs with distributed-trace/span IDs for observability. Also covers the shift toward OpenTelemetry-based log shipping and when to prefer it over a Serilog sink. Use when adding centralized logging from a .NET app to an ELK/Elastic Stack deployment, choosing between the Elastic sink and the community sink, mapping structured log properties into Elasticsearch correctly, configuring log index rollover, or correlating application logs with distributed traces.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# .NET-to-ELK Integration

Guidance on shipping structured logs from a .NET application into the Elastic Stack
(Elasticsearch/Kibana) for centralized logging and search. Organized by task, not by Elastic Stack
version — the current recommended shipping path has shifted more than once, so each reference file
states which approach is current and which is an older but still-functioning alternative, rather
than presenting both as equally current.

## Read this first: current recommended approach

This space has genuinely shifted, so don't default to whatever a training-data-era answer would
suggest:

- **The Elastic-maintained `Elastic.Serilog.Sinks` package (current release 9.0.0, verified via
  `nuget.org/packages/Elastic.Serilog.Sinks`) is Elastic's own recommended
  Serilog integration**, purpose-built around current best practices for Elasticsearch data
  streams and Index Lifecycle Management (ILM). It targets **Elasticsearch 8.x and later only** — it
  is not a drop-in replacement for older Elasticsearch major versions.
- **The community-maintained `Serilog.Sinks.Elasticsearch` package still works** and remains widely
  deployed, but is less prescriptive/opinionated about data-stream naming and ILM than the
  Elastic-maintained sink, and is not the vendor's own recommended path going forward.
- **Elastic's direction for the platform overall is shifting toward OpenTelemetry-based
  observability** rather than product-specific shipping libraries for every signal type (logs,
  metrics, traces). A Serilog sink remains a legitimate, currently supported way to ship
  *application logs* specifically; but if a project is standardizing all three signal types
  (logs/metrics/traces) around OpenTelemetry already, route logs through the OpenTelemetry .NET SDK
  and an OTel Collector rather than adding a separate sink dependency purely for logs.
- **Read [references/current-recommended-approach.md](references/current-recommended-approach.md)
  before choosing a shipping path** — the right choice depends on the target Elasticsearch version
  and whether the project already has an OpenTelemetry pipeline in place.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Deciding between `Elastic.Serilog.Sinks`, the community sink, and an OpenTelemetry-based pipeline | [references/current-recommended-approach.md](references/current-recommended-approach.md) |
| Wiring up `Elastic.Serilog.Sinks` in a .NET application | [references/serilog-elasticsearch-sink-setup.md](references/serilog-elasticsearch-sink-setup.md) |
| Mapping structured log properties into Elastic Common Schema (ECS) fields correctly | [references/ecs-structured-fields.md](references/ecs-structured-fields.md) |
| Configuring data-stream naming and Index Lifecycle Management (ILM) rollover for log indices | [references/index-naming-and-rollover.md](references/index-naming-and-rollover.md) |
| Correlating a log entry with the distributed trace/span it occurred within | [references/trace-correlation.md](references/trace-correlation.md) |
| Testing logging configuration and enrichment | [references/testing.md](references/testing.md) |

## Quick start

```csharp
using Elastic.Ingest.Elasticsearch.DataStreams;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service.name", "orders-api")
    .WriteTo.Elasticsearch(
        [new Uri("https://elasticsearch.internal:9200")],
        opts => opts.DataStream = new DataStreamName("logs", "orders-api", "production"))
    .CreateLogger();
```

`Elastic.Serilog.Sinks` writes directly to an Elasticsearch **data stream** rather than a manually
managed index — see
[index-naming-and-rollover.md](references/index-naming-and-rollover.md) for what the three
`DataStreamName` segments (`type`, `dataset`, `namespace`) mean and how they map to the resulting
index name and ILM policy.

## Out of scope

- Logstash-specific pipeline/filter configuration — this skill covers shipping logs directly from a
  .NET process to Elasticsearch, not routing them through a Logstash ingest pipeline first.
- Kibana dashboard/visualization authoring — out of scope beyond the field-mapping considerations in
  [ecs-structured-fields.md](references/ecs-structured-fields.md) that determine what's usefully
  queryable/visualizable once data lands in Elasticsearch.
- Elasticsearch cluster sizing, sharding strategy, and infrastructure operations — this skill covers
  the .NET application's shipping/mapping responsibilities, not cluster capacity planning.
