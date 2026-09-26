# Structured Logging

Serilog's entire value proposition rests on message templates: named placeholders that become
queryable properties on the emitted log event, not just text substituted into a string.

## Message templates, not string interpolation

Always write log calls with a message template and separate arguments:

```csharp
logger.LogInformation("Order {OrderId} created for {CustomerId} totaling {Total:C}", orderId, customerId, total);
```

Never pre-format the string yourself:

```csharp
// Wrong: destroys structure, defeats querying by OrderId/CustomerId/Total in a sink that supports it
logger.LogInformation($"Order {orderId} created for {customerId} totaling {total:C}");
```

With the template form, a structured sink (Seq, Elasticsearch, Application Insights, any JSON-based
sink) stores `OrderId`, `CustomerId`, and `Total` as separate, independently queryable fields on
the event — not just baked into an opaque rendered string. Even a plain-text console sink still
benefits: the same template renders to readable text while the properties remain available to any
enricher or filter running earlier in the pipeline.

## Property naming: PascalCase, matching the placeholder

Name placeholders the same way you'd name a C# property — `{OrderId}`, not `{orderid}` or
`{order_id}` — since these names surface directly as field names in whatever sink stores them
structured. Consistency here matters more once multiple log call sites emit the same
conceptual property (e.g. `OrderId` from ten different call sites) — a structured query across
all of them only works cleanly if the property name is spelled identically everywhere.

## Destructuring operators: @ and $

By default, a non-primitive argument logs via its `ToString()` (or, if it implements
`IEnumerable`, its ordinary formatting). Two operators change that:

- **`@` (destructure)** — captures the object's own properties as a structured sub-object instead
  of calling `ToString()`:

  ```csharp
  logger.LogInformation("Order placed: {@Order}", order);
  // structured sinks store Order.Id, Order.CustomerId, Order.Total, etc. as nested fields
  ```

  Use `@` for domain objects, DTOs, and anything else whose individual fields are worth querying
  independently later. Avoid destructuring an object with a large object graph or a cyclic
  reference (Serilog's destructurer has a depth limit and array/collection-count limits by
  default, but a very large graph still bloats every emitted event) — pass a smaller
  projection instead when the full graph isn't needed:

  ```csharp
  logger.LogInformation("Order placed: {@OrderSummary}", new { order.Id, order.CustomerId, order.Total });
  ```

- **`$` (stringify)** — forces `ToString()` even for a type that would otherwise destructure (a
  record, for instance) — the inverse of `@`:

  ```csharp
  logger.LogInformation("Raw payload: {$Payload}", payload);
  ```

## Positional vs. named placeholders

Named placeholders (`{OrderId}`) are strongly preferred over positional ones (`{0}`) — Serilog
supports `{0}`-style templates for compatibility with `string.Format`-style callers, but a
positional placeholder produces no meaningful property name in a structured sink (it becomes a
numeric-keyed field), losing most of the benefit of structured logging. Always use named
placeholders in code you write.

## Argument count must match placeholder count

Serilog does not throw when the argument list and template placeholders mismatch — it renders
what it can and silently drops or ignores the rest, which makes a copy-paste template/argument
mismatch a silent bug rather than a compile or runtime error. Reviewing the argument order against
the template's declared property order is a manual check with no compiler help behind it (outside
of running Serilog.Analyzer-style Roslyn analyzers on the codebase, if configured, which flag
exactly this class of mismatch at build time).
