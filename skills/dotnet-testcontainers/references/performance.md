# Performance: container scope and reuse

## The core tradeoff

Starting a container costs real wall-clock time — image pull (once, then cached), container
creation, and however long the wait strategy takes to confirm readiness. Every level of sharing a
container across more tests amortizes that cost further, at the price of tests no longer being
isolated from each other's side effects on that container's state.

## Per-test containers

Starting a fresh container in every test method (e.g. via a constructor plus `IAsyncLifetime` scoped
to each test class instance, when the test framework instantiates a new class instance per test)
gives maximum isolation — no test can observe another test's leftover data — at the highest total
startup cost, since a full container start-and-wait cycle happens once per test method.

Reserve this for tests where cross-test state leakage would be genuinely hard to reason about or
clean up deterministically, or where the number of tests against a given container is small enough
that the aggregate startup cost doesn't matter.

## Per-class or per-collection containers

Sharing one container across every test in a class (`IAsyncLifetime` on the test class itself) or
across every class in a collection (`ICollectionFixture`, shown in
`references/lifecycle-and-xunit.md`) amortizes the startup cost across many tests, at the cost of
needing each test to clean up its own state — a transaction rolled back per test, a per-test schema
or table prefix, explicit deletes in teardown — so tests don't see each other's data.

This is the default shape for most integration test suites: the startup cost of a database or
broker container is large relative to the cost of an individual test method, so spreading it across
a whole class or collection is usually the right default before reaching for per-test isolation.

## Reuse across an entire test run (or across runs)

Testcontainers supports marking a container `.WithReuse(true)` so, given identical configuration, a
subsequent run (or a subsequent test class in the same run) finds and reuses an already-running
container instead of starting a new one — the container survives past the end of any individual
test process and persists across separate `dotnet test` invocations until explicitly removed. This
requires Testcontainers' reuse feature to be enabled in the environment (an opt-in setting, since a
container that outlives a test run is a departure from the reaper-based cleanup story in
`references/cleanup-and-ryuk.md`) and demands the most careful state-cleanup discipline of any of
these options, since the container's lifetime is no longer tied to any single test run at all.

Reach for `.WithReuse(true)` only for a genuinely expensive-to-start container in a local
development inner loop where re-paying that cost on every test run is the actual bottleneck being
solved — it trades the reaper's automatic-cleanup guarantee for speed, and needs the test suite's
own cleanup logic to fully own correctness instead.

## Parallel test execution

Running test classes in parallel (a test framework's default in many setups) means multiple
containers may be starting concurrently, competing for the same host resources (CPU, memory, Docker
daemon throughput) — a wait strategy tuned against a quiet machine can become intermittently flaky
under heavy parallel load purely from resource contention, not from anything wrong with the
container or the strategy itself. If parallel container-heavy tests show occasional wait-strategy
timeouts under load but never in isolation, treat it as a parallelism/timeout-budget problem before
assuming the wait strategy is wrong.
