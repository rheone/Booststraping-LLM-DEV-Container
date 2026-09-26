# Polly

Guidance on Polly, the .NET resilience and transient-fault-handling library — the routing table
(by task, not Polly version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Polly version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `ResiliencePipelineBuilder`, `ExecuteAsync`, building a pipeline once and reusing it |
| `resilience-strategies.md` | retry, circuit breaker, timeout, rate limiter, fallback, hedging |
| `combining-strategies.md` | chaining multiple strategies, ordering, `ResiliencePipeline<T>` |
| `httpclientfactory-integration.md` | `AddResilienceHandler`, per-named-client pipelines |
| `testing.md` | `TimeProvider`-based deterministic tests, avoiding real sleeps/timeouts |

## Scope

Polly only — in-process resilience and transient-fault handling around a single delegate
execution, via the current `ResiliencePipeline` API. Out of scope: the legacy v7 `Policy` API,
distributed/infrastructure-level resilience (service mesh, message broker redelivery), and chaos
engineering against a live system (see [SKILL.md](SKILL.md) for why).

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill. Polly carries a non-standard funding arrangement layered on top of its
license — research current terms independently before adopting it for a commercial project.
