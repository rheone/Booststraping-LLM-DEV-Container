# Streaming Requests

Alongside single-value request/response, MediatR supports streaming a sequence of values back to
the caller incrementally via `IAsyncEnumerable<T>`, instead of materializing a full collection
before returning. This remains part of the current (14.2.0) API surface.

## Shape

```csharp
public sealed record StreamOrdersForCustomer(string CustomerId) : IStreamRequest<OrderDto>;

public sealed class StreamOrdersForCustomerHandler : IStreamRequestHandler<StreamOrdersForCustomer, OrderDto>
{
    public async IAsyncEnumerable<OrderDto> Handle(
        StreamOrdersForCustomer request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var order in ordersRepository.StreamByCustomerAsync(request.CustomerId, cancellationToken))
        {
            yield return MapToDto(order);
        }
    }
}
```

Dispatch via `ISender.CreateStream`, not `Send`:

```csharp
await foreach (var order in sender.CreateStream(new StreamOrdersForCustomer(customerId), cancellationToken))
{
    // process each order as it arrives, without waiting for the full result set
}
```

## When it earns its keep

- Large result sets where holding the entire collection in memory before the caller can start
  processing is wasteful (e.g. exporting a large report, paging through a large table for a
  background job).
- Server-Sent Events / streaming HTTP responses in ASP.NET Core, where the handler's
  `IAsyncEnumerable<T>` maps naturally onto an incrementally-flushed response.
- Any case where "first result available" latency matters more than total completion time.

## Pipeline behaviors for streams

Streaming requests have their own parallel pipeline concept, `IStreamPipelineBehavior<TRequest,
TResponse>`, registered the same way as `IPipelineBehavior<,>` (via `AddOpenBehavior` or direct DI
registration against the open generic). A stream behavior wraps an `IAsyncEnumerable<TResponse>`
rather than a single awaited value — logging, for instance, would log around the whole
enumeration rather than around a single `await`.

## A known sharp edge: pre-processors and stream creation timing

`IRequestPreProcessor<TRequest>` still applies to stream requests, but it runs when enumeration of
the stream **begins** (i.e. when the consumer starts pulling from the `IAsyncEnumerable`), not at
the moment `CreateStream` is called and before the `IAsyncEnumerable` is even constructed. If a
pre-processor is being relied on for validation that must happen before *any* part of the handler
runs (including deferred/lazy setup inside the iterator method), verify empirically that the
validation actually executes early enough for your use case — the timing here is a documented
point of confusion, not something to assume matches non-streaming pre-processor timing exactly.

## When not to reach for streaming

Most CRUD-shaped queries returning a bounded, reasonably-sized page of results have no need for
`IStreamRequest` — a plain `IRequest<IReadOnlyList<T>>` is simpler to consume, simpler to unit
test (no async-enumerable iteration ceremony in test code), and easier to cache. Reach for
streaming only when there's a genuine reason (data volume or latency-to-first-result) to avoid
buffering the full result before returning.
