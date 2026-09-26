# Custom metrics

## `Meter`: where your instruments come from

`System.Diagnostics.Metrics.Meter` is the BCL type that creates metric instruments, mirroring how
`ActivitySource` creates spans. Create one `Meter` per logical component, usually as a
`static readonly` field, and register its name via `AddMeter(...)` in the SDK setup
(`references/core-concepts.md`):

```csharp
public class OrderProcessor
{
    private static readonly Meter Meter = new("OrdersApi.OrderProcessor");

    private static readonly Counter<long> OrdersProcessed =
        Meter.CreateCounter<long>("orders.processed", unit: "{order}");

    private static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>("orders.processing.duration", unit: "ms");

    public async Task ProcessAsync(Order order)
    {
        var stopwatch = Stopwatch.StartNew();

        await DoProcessingAsync(order);

        OrdersProcessed.Add(1, new KeyValuePair<string, object?>("order.status", order.Status.ToString()));
        ProcessingDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
    }
}
```

## Instrument types

- **`Counter<T>`** — a monotonically increasing value (a count of events). `.Add(delta, tags...)`
  each time the event occurs; never decreases. Use for "how many times did X happen."
- **`Histogram<T>`** — a distribution of recorded values (durations, sizes). `.Record(value,
  tags...)` per observation; the SDK/backend buckets these into percentiles/aggregates rather than
  exposing every raw value. Use for "what does the distribution of X look like" (request latency,
  payload size).
- **`UpDownCounter<T>`** — like `Counter<T>` but can decrease (`.Add` with a negative delta) — for a
  value that goes up and down over time (an active-connection count, a queue depth), where a plain
  `Counter<T>` would misrepresent it as ever-increasing.
- **`ObservableGauge<T>`** — a callback-based instrument for a value read from some external state at
  export time rather than recorded imperatively (current memory usage, current queue length read
  from a data structure) — register a callback via `Meter.CreateObservableGauge(name, () =>
  currentValue)` instead of calling `.Record`/`.Add` from application code.

## Tags (dimensions)

Every recording accepts tags — key/value pairs that let a backend break a metric down by dimension
(`order.status`, `region`, `customer.tier`). Keep the *set* of distinct tag value combinations
bounded: a tag whose value is unique per request (an order ID, a user ID) turns a metric into
effectively one time series per event, which defeats aggregation and can overwhelm a backend's
cardinality limits — reserve high-cardinality identifiers for trace tags
(`references/custom-tracing.md`), not metric tags.

## Naming conventions

Favor a hierarchical, unit-suffixed name (`orders.processing.duration`) over a bare word, and pass
the actual unit (`ms`, `By` for bytes, `{order}` for a count of a specific thing) via the `unit:`
parameter — consistent naming and units make cross-service dashboards and alerts comparable instead
of needing a per-metric lookup to know what a number even represents.
