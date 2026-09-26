# Index Naming and Rollover

Current Elasticsearch/Elastic Stack conventions ship logs into **data streams**, not manually
created and manually rolled indices — the naming scheme and rollover policy (Index Lifecycle
Management, ILM) are both driven by the data stream's name rather than application-side index
naming logic.

## The data stream naming scheme

A data stream name is composed of three segments joined by hyphens:

```
{type}-{dataset}-{namespace}
```

- **`type`** — the category of data; for application logs this is always `logs`.
- **`dataset`** — identifies the specific source/format of the data, typically the service or
  application name (`orders-api`, `payments-worker`).
- **`namespace`** — a user-defined segment for further separation, commonly the deployment
  environment (`production`, `staging`) or a tenant identifier in a multi-tenant deployment.

`Elastic.Serilog.Sinks` takes these three segments directly via `DataStreamName("logs",
"orders-api", "production")` (see
[serilog-elasticsearch-sink-setup.md](serilog-elasticsearch-sink-setup.md)), producing the actual
data stream `logs-orders-api-production`.

## Built-in index templates for `logs-*-*`

Elasticsearch (from 7.9 onward) ships built-in index templates matching the `logs-*-*` and
`metrics-*-*` wildcard patterns, which automatically ensure the `data_stream.*` metadata fields are
correctly mapped for any data stream whose name matches that pattern. As long as the naming scheme
above is followed, a new data stream created by the sink on first write picks up the correct
built-in template without a manual index-template setup step for the basic data-stream mechanics.

## Rollover via Index Lifecycle Management (ILM)

- A data stream is backed by a sequence of hidden backing indices; ILM is the policy engine that
  decides when to **roll over** to a new backing index (based on age, size, or document count) and
  when to eventually delete old backing indices.
- `Elastic.Serilog.Sinks`'s data-stream-first design means rollover is handled by the cluster's ILM
  policy attached to the matching index template, not by anything the .NET application controls at
  write time — the application only ever writes to "the current data stream," and ILM transparently
  manages which backing index actually receives the write.
- The default `logs` ILM policy that ships with Elasticsearch provides reasonable rollover behavior
  out of the box; a project with specific retention requirements (e.g. "keep 90 days of logs, then
  delete") customizes the ILM policy attached to the data stream's index template in Elasticsearch/
  Kibana rather than in application code.

## Choosing the `dataset` and `namespace` segments deliberately

- Use a **stable, unique `dataset` value per logical service** — changing it later effectively
  starts a brand-new data stream (with its own backing indices and rollover history) rather than
  continuing the old one, so treat it as a naming decision made once at a service's creation, not
  something adjusted casually later.
- Use `namespace` for the axis you actually want to separate/query independently — environment is
  the most common choice (so a `production` incident investigation never accidentally includes
  `staging` noise), but a multi-tenant system might use it for tenant isolation instead, depending
  on which axis needs independent retention/ILM policy.

## Common pitfall

Treating the data stream name as an arbitrary string and picking segments that don't follow the
`type-dataset-namespace` convention (e.g. putting the environment first) still technically writes
data, but forfeits the automatic `logs-*-*` index-template matching described above — a
non-conforming name means manually creating and maintaining an index template with the correct ECS/
data-stream mappings yourself, which is exactly the manual work the built-in templates exist to
avoid.
