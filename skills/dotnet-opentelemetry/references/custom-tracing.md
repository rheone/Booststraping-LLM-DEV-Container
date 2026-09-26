# Custom spans and Activity

## `ActivitySource`: where your spans come from

`System.Diagnostics.ActivitySource` is the BCL type that creates `Activity` instances (OpenTelemetry
calls these "spans"; .NET calls the underlying type `Activity` — same concept, one type). Create one
`ActivitySource` per logical component, usually as a `static readonly` field, and register its name
via `AddSource(...)` in the SDK setup (`references/core-concepts.md`) so anything it starts is
actually collected:

```csharp
public class OrderProcessor
{
    private static readonly ActivitySource ActivitySource = new("OrdersApi.OrderProcessor");

    public async Task ProcessAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.itemCount", order.Items.Count);

        try
        {
            await ValidateAsync(order);
            await SaveAsync(order);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

- `StartActivity(name)` returns `null` if nothing is listening for that source (no `AddSource` call
  matches it, or no listener/exporter is configured at all) — the `?.` null-conditional calls
  throughout are load-bearing, not defensive style; they avoid tag/status calls on a `null`
  `Activity` when tracing isn't active.
- `using var activity = ...` stops (and, once stopped, ships to the SDK's processing pipeline) the
  `Activity` when it goes out of scope — an unstopped `Activity` never gets exported.
- `SetTag(key, value)` attaches a key/value attribute to the span; use tag names that are consistent
  across the codebase so a backend query can filter/group by them reliably.
- `SetStatus(ActivityStatusCode.Error, description)` marks the span as failed — set this in the
  `catch` block before rethrowing, since an uncaught exception alone doesn't mark a span's status by
  itself.

## Parent/child relationships happen automatically

A new `Activity` started while another one is "current" on the executing async flow (per
`Activity.Current`, which flows across `await` boundaries via `AsyncLocal` the same way
`ExecutionContext` does) automatically becomes that Activity's child — you don't pass a parent
reference explicitly in the common case. This is what makes `OrderProcessor`'s "ProcessOrder" span
above show up nested under the ASP.NET Core instrumentation library's request span
(`references/instrumentation-libraries.md`) without any code linking them together manually.

## Events: point-in-time detail within a span

`Activity.AddEvent(new ActivityEvent("name", tags: new ActivityTagsCollection { ["key"] = value }))`
records a timestamped point-in-time occurrence inside a span's duration — useful for a retry
attempt, a cache hit/miss, or any moment worth marking without ending the current span or starting a
new child span for it.

## Starting an `Activity` with an explicit kind

`StartActivity(name, ActivityKind.Client)` (or `.Server`, `.Producer`, `.Consumer`, `.Internal`)
marks the span's role in a distributed call — set `.Client` for an outbound call your code makes
directly (not already covered by an instrumentation library) and `.Producer`/`.Consumer` for
message-queue publish/receive spans, since these kinds affect how some backends visualize and link
spans across services.
