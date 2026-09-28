# Sinks

A sink is a destination a log event is written to. `LoggerConfiguration.WriteTo` accepts one or
more sinks, and every event that passes the pipeline's minimum level and any filters is delivered
to all of them independently.

## Console sink

```csharp
.WriteTo.Console()
```

The most common sink during local development. Accepts an `outputTemplate` to control the
rendered line's format:

```csharp
.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
```

`{Message:lj}` renders the message "literally" (without extra quoting applied to string
properties) — the conventional choice for human-readable console output; the default template
already uses it.

## File sink

```csharp
.WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
```

The `-` in the filename is where Serilog inserts the rolling suffix (a date, in this example),
producing files like `logs/app-20260925.log`. Key options:

- **`rollingInterval`** — `RollingInterval.Day` (most common), `Hour`, `Month`, `Year`, or
  `Infinite` (single file, no rolling).
- **`retainedFileCountLimit`** — caps how many rolled files stay on disk; older files are deleted
  automatically. Always set this for a long-running service — an unset limit means unbounded disk
  growth over the service's lifetime.
- **`fileSizeLimitBytes`** combined with `rollOnFileSizeLimit: true` — rolls to a new file once the
  current one exceeds the size limit, independent of (and combinable with) the time-based
  `rollingInterval`.
- **`shared: true`** — required when more than one process writes to the same file path
  concurrently (rare; most services own their own log directory).

```csharp
.WriteTo.File(
    "logs/app-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 14,
    fileSizeLimitBytes: 50 * 1024 * 1024,
    rollOnFileSizeLimit: true)
```

## The general sink-configuration shape

Every sink follows the same pattern regardless of its destination: an extension method on
`LoggerSinkConfiguration` (the object `WriteTo` exposes), taking sink-specific options plus two
options every sink accepts:

- **`restrictedToMinimumLevel`** — a per-sink minimum level, independent of the pipeline's overall
  `MinimumLevel`. Use this to send everything to one sink (a file, for audit/debug purposes) while
  sending only warnings and above to a noisier or costlier sink:

  ```csharp
  .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day) // everything
  .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)    // console: warnings+ only
  ```

- **`outputTemplate`** (text-based sinks) or an explicit `ITextFormatter`/structured formatter —
  controls exactly how an event renders for that destination; a JSON-based sink typically takes a
  formatter instance (e.g. `new CompactJsonFormatter()`) instead of an output template string.

## Buffering and flush-on-shutdown

Most non-console sinks batch writes for throughput rather than writing synchronously per event.
This is why `Log.CloseAndFlush()` (or the equivalent shutdown hook `UseSerilog` wires into the
ASP.NET Core host) matters — skipping it risks losing the last batch of buffered events on an
abrupt process exit. Never rely on process-exit behavior alone to flush a sink that batches.

## Choosing a sink beyond console/file

Selecting and provisioning a specific external logging backend (Seq, Elasticsearch, Application
Insights, a cloud logging service) is an infrastructure decision outside Serilog's own scope — from
Serilog's side, adding one is always the same shape: install that sink's NuGet package, add a
`.WriteTo.<SinkName>(...)` call with its connection details, and the rest of the pipeline
(enrichers, minimum level, structured templates) behaves identically regardless of which sink
receives the event.
