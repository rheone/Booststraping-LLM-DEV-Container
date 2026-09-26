# Core Concepts

Polly's current API centers on `ResiliencePipeline` — an immutable, thread-safe object built once
via `ResiliencePipelineBuilder`, then reused to execute any number of delegates that need the same
resilience behavior wrapped around them.

## Building a pipeline

```csharp
ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
        MaxRetryAttempts = 3
    })
    .AddTimeout(TimeSpan.FromSeconds(5))
    .Build();
```

`ResiliencePipelineBuilder` accumulates strategies in the order you add them — order determines
execution order and matters for correctness (see
[combining-strategies.md](combining-strategies.md)). `Build()` produces the final
`ResiliencePipeline`, which has no further mutation surface after that point.

## Build once, execute many times

A `ResiliencePipeline` is meant to be constructed once and reused across every call site that needs
the same behavior — never build a new one per call:

```csharp
// Wrong: rebuilds the pipeline (and re-validates its options) on every call
public async Task<Order> GetOrderAsync(string id, CancellationToken ct)
{
    var pipeline = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions()).Build();
    return await pipeline.ExecuteAsync(async token => await FetchOrderAsync(id, token), ct);
}

// Right: built once, reused
private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions())
    .Build();

public Task<Order> GetOrderAsync(string id, CancellationToken ct) =>
    Pipeline.ExecuteAsync(async token => await FetchOrderAsync(id, token), ct).AsTask();
```

In a DI-based application, register pipelines through `AddResiliencePipeline` (from the
`Microsoft.Extensions.Resilience`/`Polly.Extensions` registration package) and inject
`ResiliencePipelineProvider<string>` or a keyed pipeline instead of hand-rolling a static field —
this keeps configuration (retry counts, timeouts) bindable from `IConfiguration` and testable via
the same DI container the rest of the application uses.

## ExecuteAsync and the callback signature

Every strategy wraps around a delegate passed to `ExecuteAsync` (or the synchronous `Execute` for
non-async work). The delegate receives a `CancellationToken` that Polly itself may create/link
internally (for timeout strategies) — always use the token the delegate receives, not an outer
token captured by closure, so a strategy like `AddTimeout` can actually cancel the operation it
wraps:

```csharp
await pipeline.ExecuteAsync(async cancellationToken =>
{
    // use `cancellationToken`, not an outer variable
    await httpClient.GetAsync(url, cancellationToken);
}, outerCancellationToken);
```

## ResiliencePipeline<TResult>: typed results

When every execution through a pipeline returns the same result type, build a
`ResiliencePipeline<TResult>` instead of the untyped `ResiliencePipeline` — this lets
result-based strategies (a retry that triggers on a specific HTTP status code, not just an
exception) inspect the actual return value:

```csharp
ResiliencePipeline<HttpResponseMessage> pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(response => response.StatusCode == HttpStatusCode.ServiceUnavailable)
    })
    .Build();
```

## ShouldHandle and PredicateBuilder

Every strategy that reacts to failure (retry, circuit breaker, fallback, hedging) takes a
`ShouldHandle` predicate built from `PredicateBuilder`, which composes exception-type and
result-value conditions:

```csharp
ShouldHandle = new PredicateBuilder()
    .Handle<HttpRequestException>()
    .Handle<TimeoutRejectedException>()
    .HandleInner<SocketException>()
```

Handle only the specific exception types (and, for a typed pipeline, result conditions) that
represent a transient, retryable/recoverable failure — handling `Exception` broadly risks retrying
or circuit-breaking on a bug (a `NullReferenceException`, an `ArgumentException`) that retrying
will never fix and that should instead surface immediately.
