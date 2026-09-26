# Testing instrumentation

## What's worth testing about telemetry code

The business logic a method performs is tested the same way regardless of whether that method also
starts an `Activity` or records a metric — telemetry calls are additive, not a behavior change, so
don't restructure ordinary unit tests around them. What *is* worth testing directly is whether your
own instrumentation code actually produces the spans/tags/metrics it claims to: a span that's
supposed to appear on every call, a tag that's supposed to carry a specific value, a status that's
supposed to flip to `Error` on a specific failure path.

## Capturing spans in a test with an `ActivityListener`

`System.Diagnostics.ActivityListener` (a BCL type, independent of any exporter) subscribes to
`Activity` creation directly — attach one before exercising the code under test to capture and
assert on whatever spans it starts, with no OTLP endpoint, collector, or exporter needed for the
test itself:

```csharp
[Fact]
public async Task ProcessAsync_StartsASpanTaggedWithOrderId()
{
    var recordedActivities = new List<Activity>();

    using var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "OrdersApi.OrderProcessor",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        ActivityStopped = activity => recordedActivities.Add(activity),
    };
    ActivitySource.AddActivityListener(listener);

    var sut = new OrderProcessor();
    await sut.ProcessAsync(new Order { Id = 42 });

    var span = Assert.Single(recordedActivities, a => a.OperationName == "ProcessOrder");
    Assert.Equal("42", span.GetTagItem("order.id")?.ToString());
}
```

- `ShouldListenTo` filters which `ActivitySource`s this listener cares about — matching by name
  avoids capturing unrelated activity from other sources active during the same test run.
  `Sample` returning `AllData` is required for the activity to actually record tags; a listener with
  no sampling decision configured silently drops detail.
- `ActivityStopped` fires once the `Activity` is fully populated (all tags/status set), so asserting
  inside that callback (or against a list captured there, as above) avoids racing the code under
  test.
- Register the listener before any code that might call `ActivitySource.StartActivity` — a listener
  added after a source has already started producing activities for this test won't retroactively
  see them, since `StartActivity` decides whether to actually create an `Activity` (versus returning
  `null`) based on whether a listener is present at call time.

## Capturing metrics in a test with `MeterListener`

`System.Diagnostics.Metrics.MeterListener` is the metrics-signal equivalent: subscribe to a specific
`Meter`, record measurements as they're published, and assert on the recorded values:

```csharp
[Fact]
public async Task ProcessAsync_IncrementsOrdersProcessedCounter()
{
    var recordedMeasurements = new List<long>();

    using var listener = new MeterListener();
    listener.InstrumentPublished = (instrument, l) =>
    {
        if (instrument.Meter.Name == "OrdersApi.OrderProcessor" && instrument.Name == "orders.processed")
        {
            l.EnableMeasurementEvents(instrument);
        }
    };
    listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        recordedMeasurements.Add(measurement));
    listener.Start();

    var sut = new OrderProcessor();
    await sut.ProcessAsync(new Order { Id = 42 });

    Assert.Equal(new long[] { 1 }, recordedMeasurements);
}
```

## Integration-level verification with the console or an in-memory exporter

For a broader check that the full SDK pipeline (processors, resource attributes, an exporter) is
wired up correctly end-to-end rather than testing one instrumentation call in isolation, configure
`AddOpenTelemetry()` with `AddConsoleExporter()` (`references/exporters.md`) in a small integration
test's host and assert against captured console output, or use an in-memory exporter type where the
installed SDK version provides one — reserve this for verifying the SDK wiring itself (does
`ConfigureResource`/`AddSource`/`AddOtlpExporter` actually get invoked and produce output), not as
the default way to test every individual span your code produces, which the `ActivityListener`/
`MeterListener` patterns above cover more directly and without a full host spin-up.

## What not to test

Don't write tests asserting on an instrumentation library's own behavior (that
`AddAspNetCoreInstrumentation()` creates a span per request, that `AddHttpClientInstrumentation()`
propagates a `traceparent` header) — that's the instrumentation package's own correctness. Scope
tests to the custom spans, tags, and metrics your own application code adds.
