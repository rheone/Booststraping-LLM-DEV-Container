# Polly

Polly wraps an operation with retry, circuit-breaker, timeout, rate-limiting, fallback, or hedging
behavior through its `ResiliencePipeline` API, so transient failures get handled consistently
instead of scattered try/catch loops. This skill covers building and combining strategies,
wiring resilience into `HttpClientFactory`, and testing resilience behavior deterministically.

> [!NOTE]
> Polly carries a non-standard funding arrangement layered on top of its license. Research current terms independently before adopting it.

## When to reach for it

- You're adding retry, circuit-breaker, timeout, fallback, or hedging behavior around a call that can fail transiently.
- You need to combine more than one strategy into a single pipeline and get the ordering right.
- You're wiring resilience into a named `HttpClient` and want it applied automatically to every call.
- A resilience strategy isn't triggering when you expect it to (or is triggering when you don't).
- You want a test for retry or timeout behavior that doesn't actually sleep or wait out a real timeout.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches building or debugging
a Polly resilience pipeline. You can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| `ResiliencePipelineBuilder`, `ExecuteAsync`, building a pipeline once and reusing it | [references/core-concepts.md](references/core-concepts.md) |
| Retry, circuit breaker, timeout, rate limiter, fallback, hedging | [references/resilience-strategies.md](references/resilience-strategies.md) |
| Chaining multiple strategies into one pipeline and choosing their order | [references/combining-strategies.md](references/combining-strategies.md) |
| `AddResilienceHandler` and per-named-client pipelines | [references/httpclientfactory-integration.md](references/httpclientfactory-integration.md) |
| Deterministic tests for retry/timeout/circuit-breaker behavior | [references/testing.md](references/testing.md) |

## Example prompts

- "Add a retry pipeline around this HTTP call with exponential backoff."
- "Combine a circuit breaker and a timeout into one pipeline: which order should they run in?"
- "Write a test that proves my retry strategy waits before retrying, without the test actually sleeping."
