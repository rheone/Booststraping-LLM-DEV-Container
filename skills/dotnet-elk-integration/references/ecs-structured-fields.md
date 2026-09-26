# ECS Structured Fields

The Elastic Common Schema (ECS) is Elastic's field-naming and typing specification for log/event
data in Elasticsearch — mapping your application's structured log properties onto ECS field names
(rather than arbitrary ad hoc names) is what makes Kibana's built-in log views, correlation
features, and cross-service queries work without custom field-mapping configuration per service.

## Why ECS field names matter

A log event with an ad hoc property name (`OrderId`, `orderId`, `order_id` — inconsistent across
services) is fully searchable in Elasticsearch but doesn't benefit from any of Kibana's built-in
tooling that expects specific ECS field names (`service.name`, `trace.id`, `log.level`,
`error.message`). Two services logging the "same" concept under different field names can't be
correlated or filtered together in a single Kibana query without per-field mapping gymnastics.

## Common ECS fields relevant to application logging

| ECS field | Meaning | Typical source in a .NET app |
| --- | --- | --- |
| `service.name` | The logical service/application name | A fixed enrichment property set at logger configuration time |
| `service.version` | The deployed application version | Assembly version or a build-time injected value |
| `log.level` | The log severity | Serilog's own `LogEventLevel`, mapped automatically by the sink |
| `log.logger` | The originating logger/category name | The `SourceContext` Serilog property, mapped automatically |
| `error.type` / `error.message` / `error.stack_trace` | Exception details | The `Exception` passed to a Serilog log call, mapped automatically when present |
| `trace.id` / `transaction.id` | Distributed-trace correlation | See [trace-correlation.md](trace-correlation.md) |
| `http.request.method`, `url.path`, `http.response.status_code` | HTTP request context | Enrichment middleware capturing the current request, if logging in a web application |

`Elastic.Serilog.Sinks` maps a number of these automatically from Serilog's own conventions
(level, logger name, exception) without extra configuration — the fields worth deliberately
enriching yourself are the ones specific to your application's domain and the request/trace context
fields, which Serilog has no built-in concept of on its own.

## Enriching with ECS-aligned custom fields

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("service.name", "orders-api")
    .Enrich.WithProperty("service.version", ThisAssembly.AssemblyVersion)
    .Enrich.FromLogContext()
    // ...
    .CreateLogger();
```

For a custom domain field with no ECS equivalent (an application-specific identifier like an order
ID), namespacing it under a clearly application-owned prefix (`labels.orderId`, following ECS's own
`labels.*` extension convention for custom fields) avoids colliding with a future official ECS field
of the same bare name:

```csharp
Log.Information("Order {OrderId} shipped", orderId);
// becomes a `labels.OrderId` (or similarly namespaced) field once ECS-aware enrichment
// or an ECS-formatting library maps message-template properties onto the labels namespace.
```

## Mapping considerations for structured log fields

- **Keep a field's type consistent across every log call that emits it.** Elasticsearch infers a
  field's mapping from the first document it sees with that field; if one log call emits
  `OrderId` as a string and another emits it as a number, the second write either fails mapping
  validation or gets coerced/rejected depending on the index template's strictness — pick one type
  per field name and enforce it consistently across the codebase.
- **Avoid high-cardinality values as field names themselves.** A property whose *value* varies per
  event (a request ID, a timestamp) belongs as the *value* of a fixed-name field, never encoded into
  the field name itself (`request_12345: true`) — that pattern produces unbounded mapping growth
  ("mapping explosion") that degrades cluster performance and can hit the index's total field-count
  limit.
- **Prefer ECS's existing field for a concept before inventing a custom one.** Check the ECS
  reference for an existing field (`error.message`, `user.id`, `url.full`) before adding a custom
  field for something ECS already models — reusing the standard field is what makes Kibana's
  built-in views and cross-service correlation work without per-service configuration.

## Common pitfall

Logging an entire exception object's `ToString()` output as a single unstructured string field
loses the ability to query on `error.type` or search for a specific exception type across services
— pass the actual `Exception` instance to the Serilog log call (`Log.Error(exception, "message")`)
so the sink can populate the structured `error.*` ECS fields, rather than interpolating
`exception.ToString()` into the message text.
