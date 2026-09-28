# .NET-to-ELK Integration

This skill covers shipping structured logs from a .NET application into the Elastic Stack
(Elasticsearch/Kibana) for centralized search and analysis: choosing a shipping path, mapping
fields correctly, and correlating logs with distributed traces.

## When to reach for it

- Adding centralized logging from a .NET app to an Elastic Stack deployment for the first time.
- Deciding between the `Elastic.Serilog.Sinks` package, the community `Serilog.Sinks.Elasticsearch`
  sink, or shipping logs via OpenTelemetry instead.
- Mapping structured log properties onto Elastic Common Schema (ECS) fields correctly.
- Configuring data-stream index naming and rollover with Index Lifecycle Management (ILM).
- Correlating an application log entry with the distributed trace/span it happened inside.

## Using it

This skill fires automatically when your request involves shipping .NET logs to Elasticsearch or
Kibana, or choosing/configuring an Elastic sink. You can also invoke it directly with
`/dotnet-elk-integration`.

## What it covers

| Topic | Reference |
| --- | --- |
| Choosing the current shipping path vs. an older alternative | [references/current-recommended-approach.md](references/current-recommended-approach.md) |
| Wiring up `Elastic.Serilog.Sinks`, authentication, data streams | [references/serilog-elasticsearch-sink-setup.md](references/serilog-elasticsearch-sink-setup.md) |
| Elastic Common Schema (ECS) field mapping for log properties | [references/ecs-structured-fields.md](references/ecs-structured-fields.md) |
| Data-stream naming and Index Lifecycle Management (ILM) rollover | [references/index-naming-and-rollover.md](references/index-naming-and-rollover.md) |
| Correlating log entries with trace/span IDs | [references/trace-correlation.md](references/trace-correlation.md) |
| Testing logging configuration, enrichment, and field mapping | [references/testing.md](references/testing.md) |

## Example prompts

- "What's the current recommended way to ship logs from a .NET app into Elasticsearch?"
- "My log properties aren't showing up under the ECS field names Kibana expects. How do I fix the
  mapping?"
- "Set up index rollover so our application logs don't grow into one giant index."
