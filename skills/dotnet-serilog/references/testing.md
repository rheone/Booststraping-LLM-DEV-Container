# Testing

Testing Serilog-based logging means testing two different things, and conflating them produces
brittle tests: whether the *right event* was raised (level, and which properties it carries), and
whether the *pipeline configuration* itself behaves correctly (an override actually silences a
namespace, a filter actually excludes health-check noise). Test each at the layer that actually
exercises it.

## Don't assert on rendered message strings

Avoid asserting against the fully rendered text of a log message — it's the most brittle possible
check, breaking on any wording tweak, culture change, or output-template edit that has nothing to
do with whether the logging call itself is correct:

```csharp
// Fragile: breaks if anyone rewords the message template
sink.LogEvents.Should().Contain(e => e.RenderMessage() == "Order abc123 created for cust-1");
```

Assert against the event's structured properties instead — they're what a caller of the logging
API actually controls and what a structured sink actually queries on:

```csharp
sink.LogEvents.Should().Contain(e =>
    e.Level == LogEventLevel.Information &&
    e.Properties.TryGetValue("OrderId", out var orderId) &&
    orderId.ToString() == "\"abc123\"");
```

(Serilog's `LogEventPropertyValue.ToString()` includes surrounding quotes for string-typed scalar
values — account for that in the comparison, or use `((ScalarValue)orderId).Value` to compare the
unwrapped value directly.)

## In-memory sink for capturing events in a test

Configure a `LoggerConfiguration` that writes to an in-memory collection instead of a real sink,
scoped to the single test:

```csharp
public sealed class ListSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = [];
    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}

[Fact]
public void Create_LogsOrderId()
{
    var sink = new ListSink();
    var logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Sink(sink)
        .CreateLogger();

    var service = new OrderService(logger);
    service.Create(new Order { Id = "abc123", CustomerId = "cust-1" });

    sink.Events.Should().ContainSingle(e =>
        e.Properties.TryGetValue("OrderId", out var v) && ((ScalarValue)v).Value!.Equals("abc123"));
}
```

This avoids depending on a real sink's I/O (console, file) entirely, keeping the test fast and
deterministic; it requires the class under test to accept an injected `ILogger` rather than
reaching for the static `Log` class (see [core-concepts.md](core-concepts.md)).

## Testing that a service class logs, without over-specifying

For the common case — verifying a class emits *some* log event at a given level when a condition
occurs, without caring about every property — a mocked/substituted `ILogger<T>` (via
`Microsoft.Extensions.Logging.Abstractions`, since `ILogger<T>.Log` is the actual method invoked
under the hood regardless of the Serilog provider) is often simpler than a real Serilog pipeline:

```csharp
var logger = Substitute.For<ILogger<OrderService>>();
var service = new OrderService(logger);

service.Create(new Order { Id = "abc123" });

logger.Received(1).LogInformation(Arg.Any<string>(), Arg.Any<object?[]>());
```

Reserve the full in-memory-sink approach (above) for tests that specifically need to assert on
structured property values or enrichment behavior — for a plain "did it log at all" check, mocking
the abstraction is less setup and doesn't require constructing a real `LoggerConfiguration`.

## Testing pipeline configuration (overrides, filters) as integration tests

A `MinimumLevel.Override` or `Filter.ByExcluding` rule is configuration, not application logic —
test it by building the real configured pipeline (or the relevant fragment of it) against a
capturing sink and asserting on which events survive, rather than trying to unit-test the
configuration object's internals:

```csharp
[Fact]
public void Pipeline_ExcludesHealthCheckRequests()
{
    var sink = new ListSink();
    var logger = new LoggerConfiguration()
        .Filter.ByExcluding(e => e.Properties.TryGetValue("RequestPath", out var p) && p.ToString().Contains("/health"))
        .WriteTo.Sink(sink)
        .CreateLogger();

    logger.ForContext("RequestPath", "/health").Information("Request completed");
    logger.ForContext("RequestPath", "/orders").Information("Request completed");

    sink.Events.Should().ContainSingle();
}
```
