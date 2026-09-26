# HttpClientFactory Integration

`AddResilienceHandler` (from `Microsoft.Extensions.Http.Resilience`) attaches a Polly
`ResiliencePipeline` to a named or typed `HttpClient` registered through `HttpClientFactory`,
wrapping every outgoing request through that client with the configured strategies automatically.

## Attaching a pipeline to a named client

```csharp
services.AddHttpClient("orders-api", client =>
{
    client.BaseAddress = new Uri("https://orders.internal/");
})
.AddResilienceHandler("orders-api-resilience", builder =>
{
    builder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions());
    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

The ordering rules from [combining-strategies.md](combining-strategies.md) apply identically here
— retry outermost, timeout innermost, in the order the strategies are added inside the builder
delegate.

## HTTP-specific option types already know what's transient

`HttpRetryStrategyOptions` and `HttpCircuitBreakerStrategyOptions` (as opposed to the generic
`RetryStrategyOptions`/`CircuitBreakerStrategyOptions`) come with a `ShouldHandle` predicate
pre-configured for standard HTTP transient conditions — network failures, request timeouts, and
5xx/408 status codes — so most `HttpClient` resilience configuration doesn't need to hand-write a
`PredicateBuilder` from scratch. Override `ShouldHandle` only when the default set of transient
conditions doesn't match your actual failure semantics (e.g. treating a specific 4xx as retryable
for a particular API's quirks).

## Typed clients

The same `AddResilienceHandler` call attaches identically when using a typed client instead of a
named one:

```csharp
services.AddHttpClient<OrdersApiClient>(client =>
{
    client.BaseAddress = new Uri("https://orders.internal/");
})
.AddResilienceHandler("orders-api-resilience", builder =>
{
    builder.AddRetry(new HttpRetryStrategyOptions());
    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

Every `HttpClient` instance the factory creates for `OrdersApiClient` picks up the same resilience
handler transparently — application code calling `httpClient.GetAsync(...)` through the typed
client doesn't need to know a `ResiliencePipeline` is involved at all.

## Per-endpoint or per-route resilience within one client

For finer-grained control than one pipeline per named/typed client, resolve
`ResiliencePipelineProvider<string>` directly and select a pipeline by key inside the calling code,
rather than trying to vary the `AddResilienceHandler` configuration per individual request:

```csharp
public sealed class OrdersApiClient(HttpClient httpClient, ResiliencePipelineProvider<string> pipelines)
{
    public Task<Order?> GetOrderAsync(string id, CancellationToken ct)
    {
        var pipeline = pipelines.GetPipeline("orders-api-read");
        return pipeline.ExecuteAsync(
            async token => await httpClient.GetFromJsonAsync<Order>($"/orders/{id}", token),
            ct).AsTask();
    }
}
```

## Avoid double-wrapping the same failure mode

`AddResilienceHandler` already wraps every request through the named/typed client — adding a
second, separately-built `ResiliencePipeline` around the same `httpClient.GetAsync(...)` call in
application code (rather than through `AddResilienceHandler` or a resolved pipeline for a distinct
purpose) means retries and timeouts compound across both layers, producing effective retry counts
and timeout windows that are the product of both configurations rather than either one's intended
values. Configure resilience for an `HttpClient` in exactly one place.
