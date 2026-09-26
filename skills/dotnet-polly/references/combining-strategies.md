# Combining Strategies

A single `ResiliencePipeline` commonly stacks more than one strategy — order is not cosmetic; it
determines which strategy wraps which, and getting it backwards produces behavior that looks
plausible but defeats the point of one of the strategies.

## Strategies execute outside-in, wrap inside-out

Strategies added earlier to the builder wrap strategies added later — the first `Add*` call is the
outermost layer, the last is the innermost, closest to the actual delegate:

```csharp
ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
    .AddRetry(retryOptions)      // outermost: retries the entire inner pipeline
    .AddCircuitBreaker(cbOptions) // middle
    .AddTimeout(timeoutOptions)   // innermost: bounds each individual attempt
    .Build();
```

Read this as: retry wraps circuit-breaker wraps timeout wraps the actual call. Each retry attempt
re-enters the circuit breaker and gets a fresh timeout-bounded attempt.

## The standard order: retry outermost, timeout innermost

The conventional and almost always correct order is **retry → circuit breaker → timeout**, from
outermost to innermost:

- **Timeout innermost** ensures each individual attempt is bounded, so a single slow call can't
  consume the entire retry budget's worth of wall-clock time by itself.
- **Circuit breaker in the middle** sees every attempt (including retried ones) and can trip based
  on the aggregate failure rate across all of them, short-circuiting further retries once it opens.
- **Retry outermost** re-runs the whole timeout-bounded, circuit-breaker-guarded operation on
  failure — including getting a fresh timeout window on each retry attempt.

Reversing timeout and retry (timeout outermost) is a common mistake: a single overall timeout
wrapping multiple retry attempts means the *first* slow attempt can consume the entire timeout
budget, leaving no time for any retry to happen at all — the opposite of the intended effect.

## Circuit breaker must be shared across calls to matter

Because a circuit breaker accumulates failure history across executions, it only works correctly
when the same `ResiliencePipeline` instance (with its circuit breaker in a fixed position) is
reused across every call to the same logical dependency — see
[core-concepts.md](core-concepts.md) on building a pipeline once. A circuit breaker inside a
freshly built pipeline per call never has enough history to open.

## Fallback and hedging: usually outermost or standalone

A fallback strategy is typically the outermost layer (or its own separate pipeline entirely) since
its job is to catch whatever the rest of the pipeline couldn't recover from and substitute a final
result — placing it inside a retry would mean the fallback's substituted result gets retried too,
which is rarely intended:

```csharp
.AddFallback(fallbackOptions)  // outermost: catches whatever nothing else could resolve
.AddRetry(retryOptions)
.AddTimeout(timeoutOptions)
```

Hedging normally replaces retry rather than combining with it in the same pipeline — both strategies
address "the first attempt didn't return an acceptable result fast enough," and combining them
multiplies concurrent load (hedged attempts, each individually retried) in a way that's rarely the
actual intent. Pick one or the other for a given operation based on whether the goal is reducing
tail latency (hedging) or recovering from outright failure (retry).

## Building the pipeline via configuration/DI instead of a fixed builder chain

`AddResiliencePipeline` (from the DI registration extensions) accepts the same builder shape inside
a delegate, letting the whole ordered chain live in one registration:

```csharp
services.AddResiliencePipeline("orders-api", builder => builder
    .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions())
    .AddTimeout(TimeSpan.FromSeconds(5)));
```

Resolve it later via `ResiliencePipelineProvider<string>.GetPipeline("orders-api")` — the ordering
rules above apply identically regardless of whether the pipeline is built directly or through this
registration path.
