# Resilience Strategies

Each strategy below is added to a `ResiliencePipelineBuilder` via its own `Add*` method and
configured through a dedicated options type. This file covers each strategy's own mechanics; see
[combining-strategies.md](combining-strategies.md) for how they interact when stacked together in
one pipeline.

## Retry

Re-executes the delegate a bounded number of times when `ShouldHandle` matches the outcome:

```csharp
.AddRetry(new RetryStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
    MaxRetryAttempts = 3,
    Delay = TimeSpan.FromMilliseconds(200),
    BackoffType = DelayBackoffType.Exponential,
    UseJitter = true
})
```

- **`BackoffType`** — `Constant`, `Linear`, or `Exponential`. Prefer `Exponential` with
  `UseJitter = true` for anything calling a shared external dependency — jitter spreads retries
  from many concurrent callers instead of having them all retry in lockstep, which is exactly the
  pattern that turns a brief blip into a retry storm against the dependency.
- **`OnRetry`** — an optional callback (`Func<OnRetryArguments<TResult>, ValueTask>`) for logging
  each retry attempt; use it to record the attempt number and delay rather than only logging the
  final outcome.
- Retry attempts count *in addition to* the first attempt — `MaxRetryAttempts = 3` means up to 4
  total executions (1 initial + 3 retries).

## Circuit Breaker

Stops calling a failing dependency for a cooldown period once failures cross a threshold, so a
struggling downstream service isn't hammered with more load while it recovers:

```csharp
.AddCircuitBreaker(new CircuitBreakerStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
    FailureRatio = 0.5,
    MinimumThroughput = 10,
    SamplingDuration = TimeSpan.FromSeconds(30),
    BreakDuration = TimeSpan.FromSeconds(15),
    OnOpened = args => { logger.LogWarning("Circuit opened: {Reason}", args.Outcome.Exception?.Message); return default; },
    OnClosed = _ => { logger.LogInformation("Circuit closed"); return default; }
})
```

- **`FailureRatio`** + **`MinimumThroughput`** — the circuit only evaluates failure ratio once at
  least `MinimumThroughput` calls have occurred within `SamplingDuration`; a low-traffic period
  won't trip the breaker off a handful of failures alone.
- States: **Closed** (normal, calls flow through) → **Open** (calls fail fast without invoking the
  delegate at all, for `BreakDuration`) → **Half-Open** (a single trial call is allowed through to
  test recovery) → back to **Closed** on success or **Open** again on failure.
- A circuit breaker is stateful across executions through the same pipeline instance — this is
  another reason to build and share one pipeline instance per logical dependency rather than a new
  one per call (see [core-concepts.md](core-concepts.md)); a fresh instance per call would never
  accumulate enough history to trip.

## Timeout

Bounds how long a single execution is allowed to run:

```csharp
.AddTimeout(TimeSpan.FromSeconds(5))

// or, with a callback:
.AddTimeout(new TimeoutStrategyOptions
{
    Timeout = TimeSpan.FromSeconds(5),
    OnTimeout = args => { logger.LogWarning("Operation timed out after {Timeout}", args.Timeout); return default; }
})
```

Throws `TimeoutRejectedException` when exceeded. The delegate must actually observe and honor the
`CancellationToken` it's given (see [core-concepts.md](core-concepts.md)) — a timeout strategy
cancels the token when the timeout elapses, but cannot forcibly abort code that ignores
cancellation and keeps running past the timeout regardless.

## Rate Limiter

Bounds the number of concurrent or per-window executions allowed through, rejecting (rather than
queuing or delaying) once the limit is hit:

```csharp
.AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
{
    PermitLimit = 100,
    Window = TimeSpan.FromSeconds(1),
    SegmentsPerWindow = 4
}))
```

Polly's rate limiter strategy wraps `System.Threading.RateLimiting` limiters directly (the same
limiter types ASP.NET Core's own rate-limiting middleware uses) — reach for this to protect your
own outbound call rate against a downstream dependency's rate limit, distinct from ASP.NET Core's
inbound rate limiting on requests your service receives.

## Fallback

Supplies a substitute result (or executes a substitute delegate) when the primary operation fails,
instead of letting the failure propagate:

```csharp
.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
{
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<HttpRequestException>(),
    FallbackAction = args => Outcome.FromResultAsValueTask(cachedResponse)
})
```

Use a fallback only where a degraded-but-usable response genuinely exists (a cached value, a
default, a "service temporarily unavailable" placeholder) — a fallback that silently swallows an
error and returns an empty/default value where the caller can't tell the difference from a real
empty result hides failures instead of handling them.

## Hedging

Starts one or more additional, parallel attempts if the primary attempt is slow to respond,
taking whichever attempt finishes first — trading extra load for reduced tail latency:

```csharp
.AddHedging(new HedgingStrategyOptions<HttpResponseMessage>
{
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<HttpRequestException>(),
    MaxHedgedAttempts = 2,
    Delay = TimeSpan.FromMilliseconds(500),
    ActionGenerator = args => () => httpClient.GetAsync(url, args.ActionContext.CancellationToken)
})
```

Hedging only makes sense for idempotent operations — issuing a second, concurrent attempt at a
non-idempotent write (a payment charge, a non-idempotent POST) risks executing the operation twice
if both attempts eventually succeed. Restrict hedging to reads and idempotent operations.

## Strategy summary

| Strategy | Reacts to | Effect |
| --- | --- | --- |
| Retry | Failure matching `ShouldHandle` | Re-executes, up to a bounded attempt count |
| Circuit Breaker | Failure rate over a sampling window | Fails fast without executing, for a cooldown period |
| Timeout | Execution duration | Cancels and throws `TimeoutRejectedException` |
| Rate Limiter | Concurrent/windowed call volume | Rejects excess calls immediately |
| Fallback | Failure matching `ShouldHandle` | Substitutes a fallback result/action |
| Hedging | Slow response (or failure) | Races additional parallel attempts |
