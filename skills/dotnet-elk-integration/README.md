# .NET-to-ELK Integration

Guidance on shipping structured logs from a .NET application into the Elastic Stack
(Elasticsearch/Kibana) — the routing table (by task, not Elastic Stack version) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Elastic Stack version

| File | Covers |
| --- | --- |
| `current-recommended-approach.md` | `Elastic.Serilog.Sinks` vs. community `Serilog.Sinks.Elasticsearch` vs. OpenTelemetry-based shipping |
| `serilog-elasticsearch-sink-setup.md` | Wiring up `Elastic.Serilog.Sinks`, authentication, data streams |
| `ecs-structured-fields.md` | Elastic Common Schema (ECS) field mapping for structured log properties |
| `index-naming-and-rollover.md` | Data-stream naming (`type-dataset-namespace`), Index Lifecycle Management (ILM) rollover |
| `trace-correlation.md` | Correlating log entries with distributed-trace/span IDs |
| `testing.md` | Testing logging configuration, enrichment, and field mapping |

## Scope

Shipping and mapping structured logs from a .NET application directly into Elasticsearch/Kibana.
Out of scope: Logstash-specific pipeline/filter authoring, Kibana dashboard/visualization
authoring beyond field-mapping considerations, and Elasticsearch cluster infrastructure/capacity
planning.

Each reference file states which shipping approach is current and verified as of this writing
versus an older but still-functioning alternative — this space has shifted more than once, so
version/currency is called out explicitly rather than presented as settled (see
[SKILL.md](SKILL.md) for why).
