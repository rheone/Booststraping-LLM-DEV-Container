---
name: dotnet-polly
description: Guidance on Polly (verified current release 8.8.0) for .NET resilience and transient-fault handling via the modern ResiliencePipeline API — resilience strategies (retry, circuit breaker, timeout, rate limiter, fallback, hedging), building and combining strategies into a single ResiliencePipeline, integrating with HttpClientFactory via AddResilienceHandler, and testing resilience behavior deterministically with a simulated time provider instead of real sleeps or timeouts. Use when adding retry/circuit-breaker/timeout/fallback/hedging behavior to an operation, wiring resilience into HttpClient, debugging why a resilience strategy did or didn't trigger, or writing a deterministic test for resilience behavior.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Polly

Guidance on Polly, the .NET resilience library for expressing retry, circuit-breaker, timeout,
rate-limiting, fallback, and hedging behavior around an operation. Organized by task, not by
version — the current `ResiliencePipeline` API (the v8 rewrite) is the API this skill documents
throughout; each reference file notes a version-introduced fact inline where it matters.

Polly carries a non-standard funding arrangement layered on top of its license — research current
terms independently before recommending it for a commercial project.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Building your first `ResiliencePipeline` and understanding the builder API | [references/core-concepts.md](references/core-concepts.md) |
| Adding retry, circuit breaker, timeout, rate limiter, fallback, or hedging behavior | [references/resilience-strategies.md](references/resilience-strategies.md) |
| Combining multiple strategies into one pipeline, and deciding their order | [references/combining-strategies.md](references/combining-strategies.md) |
| Wiring resilience into `HttpClient` via `HttpClientFactory` | [references/httpclientfactory-integration.md](references/httpclientfactory-integration.md) |
| Writing a fast, deterministic test for retry/timeout/circuit-breaker behavior | [references/testing.md](references/testing.md) |

## Quick start

A minimal retry pipeline, current API (v8.8.0):

```csharp
ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential
    })
    .Build();

await pipeline.ExecuteAsync(async ct =>
{
    await httpClient.GetAsync("https://api.example.com/orders", ct);
}, cancellationToken);
```

The single most common miss: building a new `ResiliencePipeline` on every call instead of
constructing it once (typically via `AddResiliencePipeline` in DI, or as a `static readonly` field)
and reusing it — pipelines are immutable and thread-safe by design and are meant to be built once,
not per-execution. See [core-concepts.md](references/core-concepts.md).

## Out of scope

- The legacy `Policy`/`Policy<T>` API from Polly v7 and earlier — this skill documents only the
  current `ResiliencePipeline` API; migrating an existing v7 `Policy`-based codebase is a
  version-upgrade exercise this skill does not walk through step by step.
- Distributed resilience infrastructure (service mesh retry/circuit-breaking, a message broker's
  own redelivery policy) — Polly operates entirely in-process, around a single delegate execution;
  nothing here applies to network-level or infrastructure-level fault handling.
- Chaos engineering / fault injection for testing production resilience under real failure
  conditions — out of scope beyond the deterministic unit-testing techniques in
  [testing.md](references/testing.md), which test the pipeline's own logic, not a live system's
  behavior under injected faults.
