# Wait strategies

## Why a wait strategy is mandatory, not optional

`StartAsync()` returns once Docker reports the container process as running — that is not the same
moment the service inside it is ready to do useful work. A database container's process can be
alive for several seconds while the engine itself is still initializing. Without an explicit wait
strategy, the first operation a test performs against the container races that startup window and
fails intermittently — flaky exactly in the way that's hardest to reproduce locally, because a
developer's machine is often fast enough to mask it.

Every `ContainerBuilder` call takes a `WithWaitStrategy(...)`; every prebuilt module supplies one by
default tuned to that engine (see `references/prebuilt-modules.md`) — but any container built from
the generic builder without a matching module needs one written by hand.

## Built-in strategies

`Wait.ForUnixContainer()` (or `Wait.ForWindowsContainer()` for Windows-based images) returns a
builder for composing conditions:

```csharp
var waitStrategy = Wait.ForUnixContainer()
    .UntilPortIsAvailable(5432);

var waitStrategy2 = Wait.ForUnixContainer()
    .UntilMessageIsLogged("database system is ready to accept connections");

var waitStrategy3 = Wait.ForUnixContainer()
    .UntilCommandIsCompleted("pg_isready", "-U", "postgres");

var waitStrategy4 = Wait.ForUnixContainer()
    .UntilHttpRequestIsSucceeded(request => request.ForPort(8080).ForPath("/health"));
```

- `UntilPortIsAvailable` only confirms a TCP socket is accepting connections — the weakest signal,
  since many services open their port before they're actually ready to serve requests correctly.
  Prefer it only when the technology genuinely has no better readiness signal.
- `UntilMessageIsLogged` matches a literal or regex pattern against the container's stdout/stderr —
  usually the most reliable signal, since most server software logs its own "ready" line.
- `UntilCommandIsCompleted` runs a command inside the container (e.g. the engine's own health-check
  CLI) and waits for a zero exit code.
- `UntilHttpRequestIsSucceeded` polls an HTTP endpoint until it returns a successful status —
  natural for anything exposing its own health-check route.

Chain multiple `.Until...` conditions on the same `Wait.ForUnixContainer()` builder when a single
signal isn't sufficient; all chained conditions must pass before `StartAsync()` returns.

## Custom wait strategies

For a readiness signal none of the built-ins express, implement `IWaitUntil` (or the current
wait-strategy interface the installed package version exposes) with your own polling logic, and
pass an instance to `WithWaitStrategy`. Keep a custom strategy's polling interval reasonable (avoid
a tight busy-loop) and give it a bounded total timeout — `WithWaitStrategy` composes with
`WithStartupCallback`/timeout configuration on the builder to cap how long a hung container can
block a test run before failing loudly instead of hanging the whole suite.

## Timeout behavior

If a wait strategy's condition never becomes true, `StartAsync()` eventually throws rather than
hanging forever — treat that failure as "the wait strategy doesn't match this image's actual
readiness signal" first, before assuming the container itself is broken; check the container's logs
(`await container.GetLogsAsync()`) to see what it actually printed during startup.
