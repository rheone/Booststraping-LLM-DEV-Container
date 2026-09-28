# Enrichers

An enricher attaches additional properties to every log event that passes through it, without
each call site having to pass that data explicitly. Reach for an enricher whenever a piece of
context (which machine, which request, which correlation id) should appear on every event, not
just the ones a developer remembered to include manually.

## FromLogContext: the enricher almost every pipeline needs

`Enrich.FromLogContext()` makes `LogContext.PushProperty(...)` work — without it, properties
pushed onto the ambient log context are silently ignored:

```csharp
.Enrich.FromLogContext()
```

`LogContext.PushProperty` attaches a property to every log event emitted for the duration of an
`IDisposable` scope, which is how request-scoped or operation-scoped correlation data typically
gets onto every event without threading it through every method signature:

```csharp
using (LogContext.PushProperty("OrderId", order.Id))
{
    logger.LogInformation("Validating order");
    await ValidateAsync(order);
    logger.LogInformation("Order validated"); // both events carry OrderId
}
```

ASP.NET Core's request logging integration relies on this same mechanism to attach per-request
properties (see [aspnetcore-integration.md](aspnetcore-integration.md)).

## Built-in enrichers

The `Serilog.Enrichers.*` family of packages (each a separate NuGet install) covers the common
ambient-context cases:

| Enricher | Package | Adds |
| --- | --- | --- |
| `WithMachineName()` | Serilog.Enrichers.Environment | `MachineName` |
| `WithEnvironmentName()` | Serilog.Enrichers.Environment | `EnvironmentName` (from `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`) |
| `WithProcessId()` | Serilog.Enrichers.Process | `ProcessId` |
| `WithThreadId()` | Serilog.Enrichers.Thread | `ThreadId` |
| `WithCorrelationId()` | Serilog.Enrichers.CorrelationId | `CorrelationId` from `HttpContext` |

```csharp
.Enrich.WithMachineName()
.Enrich.WithEnvironmentName()
.Enrich.WithThreadId()
```

## A fixed property with WithProperty

For a value known at configuration time (not per-event), attach it directly rather than reaching
for a full enricher:

```csharp
.Enrich.WithProperty("ServiceName", "orders-api")
.Enrich.WithProperty("Version", typeof(Program).Assembly.GetName().Version?.ToString())
```

## Writing a custom ILogEventEnricher

When no built-in enricher covers the data you need, implement `ILogEventEnricher` directly:

```csharp
public sealed class TenantEnricher(ITenantAccessor tenantAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var tenantId = tenantAccessor.CurrentTenantId;
        if (tenantId is not null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TenantId", tenantId));
        }
    }
}

// registration
.Enrich.With(new TenantEnricher(tenantAccessor))
```

Use `AddPropertyIfAbsent` (not `AddOrUpdateProperty`) unless deliberately overwriting a
same-named property set earlier in the pipeline — `AddPropertyIfAbsent` respects whatever an
earlier enricher or an explicit `LogContext.PushProperty` call already set, which is almost always
the intended precedence order (more specific, closer-to-the-event context wins over a global
enricher).

## Enrichment order and cost

Enrichers run in the order they're added to the configuration, for every event that reaches them
— an enricher that does real work (a database lookup, a service call) on every single log event is
a correctness and performance hazard; keep enrichers reading from cheap, already-available ambient
state (thread-local data, a cached accessor, `HttpContext.Items`) rather than performing I/O.
