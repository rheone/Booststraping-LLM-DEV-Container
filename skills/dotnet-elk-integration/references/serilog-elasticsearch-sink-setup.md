# Serilog Elasticsearch Sink Setup

Wiring up `Elastic.Serilog.Sinks` (the Elastic-maintained, current-recommended package — see
[current-recommended-approach.md](current-recommended-approach.md)) to ship structured logs from a
.NET application to Elasticsearch.

## Minimal setup

```csharp
using Elastic.Ingest.Elasticsearch.DataStreams;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Elasticsearch(
        [new Uri("https://elasticsearch.internal:9200")],
        opts => opts.DataStream = new DataStreamName("logs", "orders-api", "production"))
    .CreateLogger();
```

- `.Enrich.FromLogContext()` is what makes properties pushed via `LogContext.PushProperty(...)` (or
  structured properties on individual log calls, `Log.Information("Order {OrderId} created", id)`)
  actually reach the shipped document — without it, structured properties attached via
  `LogContext` are silently dropped from the final Elasticsearch document.
- `DataStreamName` takes three segments — `type`, `dataset`, `namespace` — that together form the
  actual Elasticsearch data stream name (`logs-orders-api-production` for the example above). See
  [index-naming-and-rollover.md](index-naming-and-rollover.md) for what each segment means and how
  it drives ILM policy selection.

## Authentication

```csharp
using Elastic.Transport;

.WriteTo.Elasticsearch(
    [new Uri("https://elasticsearch.internal:9200")],
    opts => opts.DataStream = new DataStreamName("logs", "orders-api", "production"),
    transport => transport.Authentication(new ApiKey(apiKeyBase64)))
```

- API-key authentication (`new ApiKey(...)`) is the preferred mechanism for a service shipping its
  own logs — scope the key to only the ingest privileges the shipping process needs, not a
  cluster-admin key.
- Basic authentication (`transport.Authentication(new BasicAuthentication(username, password))`) is
  also supported for clusters/deployments not using API keys.
- Never hard-code the API key or credentials in source — resolve them from configuration
  (`IConfiguration`, a secret manager, or an environment variable) the same way any other connection
  secret is handled in the application.

## Buffering and failure behavior

```csharp
opts.ConfigureChannel = channelOpts =>
{
    channelOpts.BufferOptions = new BufferOptions { ConcurrentConsumers = 4 };
};
opts.BootstrapMethod = BootstrapMethod.Failure;
```

- The sink buffers and batches events internally rather than making one HTTP request per log
  event — `ConfigureChannel`/`BufferOptions` tune batching concurrency and are worth adjusting under
  high log volume, but the defaults are reasonable for most services.
- `BootstrapMethod.Failure` (vs. `BootstrapMethod.Silent` or `.Ignore`) controls what happens if the
  sink can't establish the expected data stream/index template on startup — `Failure` surfaces the
  problem loudly (useful in development and staging so a misconfigured cluster connection is caught
  immediately) rather than silently dropping logs.

## Minimum log level and self-logging

Configure Serilog's own diagnostic output (`Serilog.Debugging.SelfLog`) during initial setup and
whenever the sink appears to silently drop events — the sink logs its own delivery failures
(connection errors, mapping conflicts, authentication failures) to `SelfLog` rather than to the
application's own log stream, since a sink can't reliably use itself to report its own failures:

```csharp
Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));
```

## Common pitfall

Registering `WriteTo.Elasticsearch` without `.Enrich.FromLogContext()` (or without explicitly
enriching with the structured properties a query later depends on) produces documents in
Elasticsearch that look sparse compared to what the application actually logged — the message
template renders fine, but the individual structured values used to filter/aggregate in Kibana
never made it into separate, queryable fields. Confirm enrichment is wired up whenever a query
against a specific structured field returns nothing despite matching log lines being visible in the
message text.
