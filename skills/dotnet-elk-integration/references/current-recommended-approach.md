# Current Recommended Approach

This is the file to read before writing any logging-pipeline code — the right shipping mechanism
depends on the target Elasticsearch version and on whether the project has already standardized on
OpenTelemetry for other signal types.

## Option 1: `Elastic.Serilog.Sinks` (Elastic's own recommended Serilog integration)

- **Current release: 9.0.0** (verified via `nuget.org/packages/Elastic.Serilog.Sinks`), published
  and maintained by Elastic directly (not a community contributor package).
- Purpose-built around current Elasticsearch best practices: writes to **data streams** rather than
  manually managed indices, integrates with **Index Lifecycle Management (ILM)** for rollover by
  default, and is deliberately **more prescriptive and less configurable** than the older community
  sink — fewer knobs, but the defaults already match Elastic's own recommended index/ILM setup.
- **Requires Elasticsearch 8.x or later.** It is not usable against a 7.x or earlier cluster —
  check the target cluster's version before choosing this package.
- This is the package to reach for on any new .NET project shipping logs to a current (8.x+)
  Elasticsearch/Elastic Cloud deployment, absent a specific reason to prefer one of the other two
  options below.

## Option 2: Community `Serilog.Sinks.Elasticsearch`

- Still functional and widely deployed in existing projects.
- More configurable than `Elastic.Serilog.Sinks` — exposes lower-level indexing/template options
  the Elastic-maintained sink deliberately doesn't surface.
- Not Elastic's own recommended path going forward, and its defaults are not automatically aligned
  with the current data-stream/ILM conventions the Elastic-maintained sink bakes in — a project
  choosing this sink needs to configure index templates and rollover policy explicitly rather than
  getting Elastic's current defaults out of the box.
- Reasonable choice when the target Elasticsearch cluster predates 8.x, or when an existing project
  already has this sink deeply configured and migrating carries more cost than benefit.

## Option 3: OpenTelemetry-based log shipping

- Elastic's broader platform direction is shifting toward OpenTelemetry as the unified mechanism
  for shipping all three observability signal types (logs, metrics, traces), rather than a separate
  product-specific library per signal.
- If a project already routes its traces and metrics through the OpenTelemetry .NET SDK and an OTel
  Collector, route logs through the same pipeline rather than adding a Serilog sink purely to cover
  logs — this avoids running two parallel shipping mechanisms with separate configuration,
  authentication, and failure modes.
- If a project has no existing OpenTelemetry pipeline and only needs to ship logs (not traces or
  metrics) to Elasticsearch, adding a full OTel Collector pipeline purely for logs is more
  infrastructure than a direct Serilog sink for a logs-only need — `Elastic.Serilog.Sinks` is the
  lighter-weight choice in that case.

## Decision guide

| Situation | Choice |
| --- | --- |
| New project, Elasticsearch 8.x+, no existing OpenTelemetry pipeline | `Elastic.Serilog.Sinks` |
| Existing project already on the community sink, Elasticsearch 8.x+, no urgent reason to migrate | Community `Serilog.Sinks.Elasticsearch` remains functional; migrate opportunistically |
| Target cluster is Elasticsearch 7.x or earlier | Community `Serilog.Sinks.Elasticsearch` (the Elastic-maintained sink does not support it) |
| Project already ships traces/metrics via OpenTelemetry | Route logs through the same OpenTelemetry .NET SDK / OTel Collector pipeline rather than adding a separate sink |

## Common pitfall

Assuming `Elastic.Serilog.Sinks` is a drop-in replacement for the community sink with the same
configuration surface — its options object (`ElasticsearchSinkOptions` in the Elastic-maintained
package) is a different, smaller type than the community sink's options class, and index-template/
mapping customizations expressed against the community sink's API don't carry over directly. Treat
a migration between the two as a genuine reconfiguration, not a namespace swap.
