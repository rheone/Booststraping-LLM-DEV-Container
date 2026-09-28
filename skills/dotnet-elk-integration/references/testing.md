# Testing

Testing a logging pipeline splits into two different concerns: verifying the *application code*
enriches/logs correctly (fast, no real Elasticsearch needed), and verifying the *shipped documents*
actually land with the expected shape in a real cluster (slower, needs a real or containerized
Elasticsearch).

## Unit testing enrichment logic without a real sink

Point the logger configuration at an in-memory sink instead of `Elastic.Serilog.Sinks` and assert on
the captured `LogEvent`'s properties — this is the fast, default layer for verifying enrichment and
field-mapping logic:

```csharp
[Fact]
public void ActivityEnricher_WithActiveActivity_AddsTraceAndTransactionIdProperties()
{
    using var activitySource = new ActivitySource("test");
    using var listener = new ActivityListener
    {
        ShouldListenTo = _ => true,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    };
    ActivitySource.AddActivityListener(listener);

    var events = new List<LogEvent>();
    var logger = new LoggerConfiguration()
        .Enrich.With<ActivityEnricher>()
        .WriteTo.Sink(new DelegatingSink(events.Add))
        .CreateLogger();

    using (activitySource.StartActivity("test-operation"))
    {
        logger.Information("test message");
    }

    var logged = events.Single();
    logged.Properties.Should().ContainKey("trace.id");
    logged.Properties.Should().ContainKey("transaction.id");
}
```

Assert on the *presence and shape* of the fields a downstream Elasticsearch query would depend on
(the ECS field names themselves), not on Serilog's internal `LogEvent` representation details —
the contract that matters is "does the shipped document have a `trace.id` field," not "does the
in-memory `LogEventPropertyValue` have a particular .NET type."

## Testing that a custom field stays consistently typed

Because Elasticsearch infers a field's type from the first document it sees (see
[ecs-structured-fields.md](ecs-structured-fields.md)), a regression where one code path
accidentally logs a field as a different type than another code path is a real production bug
(mapping conflicts causing dropped/rejected documents) worth a specific unit test:

```csharp
[Fact]
public void OrderLogging_AlwaysLogsOrderIdAsString()
{
    // Every call site that logs an order identifier should format it consistently
    // (e.g. Guid.ToString()) rather than letting the compiler's structured-logging
    // capture pick a different runtime type at a different call site.
}
```

## Integration-testing against a real Elasticsearch (or a container)

Reserve this for the smaller set of tests verifying the sink itself is configured correctly end to
end — connection settings, authentication, and that documents actually land in the expected data
stream with the expected mapping. Run against a disposable, containerized Elasticsearch instance
(a Testcontainers-managed Elasticsearch container is the common approach) rather than a shared
persistent cluster, so the test suite doesn't depend on external infrastructure being available or
clean between runs:

```csharp
[Fact]
public async Task Logger_WritesDocument_ToExpectedDataStream()
{
    // Arrange: containerized Elasticsearch, a logger configured with the real sink
    // pointed at the container's endpoint.

    logger.Information("integration test event {OrderId}", "order-123");
    await Task.Delay(TimeSpan.FromSeconds(1)); // sink batches/flushes asynchronously

    var response = await elasticsearchClient.SearchAsync<LogDocument>(s =>
        s.Index("logs-orders-api-test").Query(q => q.Match(m => m.Field("OrderId").Query("order-123"))));

    response.Documents.Should().ContainSingle();
}
```

Expect to need a short delay or a polling-with-timeout assertion rather than an immediate query
after logging — sinks batch and flush asynchronously, so a document is not guaranteed to be
searchable the instant `Log.Information` returns.

## Most likely scenarios

| Scenario | Approach |
| --- | --- |
| Verifying an enricher adds the right ECS fields | In-memory sink, assert on `LogEvent.Properties` |
| Verifying a field stays consistently typed across call sites | Targeted unit test on the logging call sites, or a static-analysis rule if the field is widely used |
| Verifying the sink actually ships and the document lands as expected | Containerized Elasticsearch integration test, reserved for a small, specific set of tests |
