# Log Levels, Overrides, Sub-Loggers, and Filtering

Serilog controls verbosity at three layers: a global minimum level, per-source overrides, and
event-level filters — each solving a different shape of "which events actually get written."

## The level scale

From least to most severe: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`. An
event is written only if its level is at or above the effective minimum level for its source.

```csharp
.MinimumLevel.Information()
```

## Per-source-context overrides

Every logger created via `ILogger<T>` (or `Log.ForContext<T>()`) carries a `SourceContext`
property set to the type's full name. `MinimumLevel.Override` sets a different minimum for any
source context whose name starts with a given prefix — the standard way to silence a noisy
namespace (framework internals, a chatty third-party library) without lowering verbosity
everywhere:

```csharp
.MinimumLevel.Information()
.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
.MinimumLevel.Override("MyCompany.Orders", LogEventLevel.Debug) // more verbose for one namespace
```

Overrides can raise or lower verbosity relative to the default — the second example above
increases verbosity for a specific namespace of interest while everything else stays at the
default `Information`.

## Changing level at runtime with LoggingLevelSwitch

A `LoggingLevelSwitch` lets the effective minimum level change while the process is running,
without rebuilding the logger — useful for a diagnostic "turn on verbose logging for the next few
minutes" operation triggered by an admin endpoint or configuration reload:

```csharp
var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .WriteTo.Console()
    .CreateLogger();

// later, at runtime:
levelSwitch.MinimumLevel = LogEventLevel.Debug;
```

`ReadFrom.Configuration` supports binding a level switch to a configuration reload automatically
when combined with `IOptionsMonitor`-style config reload — check
`Serilog.Settings.Configuration`'s reload support before hand-rolling this.

## Sub-loggers: WriteTo.Logger for a separate pipeline

`WriteTo.Logger(...)` nests an entire independent `LoggerConfiguration` — its own minimum level,
its own filters, its own sinks — as one branch of the parent pipeline. Use it to route a subset of
events to a dedicated sink with rules the rest of the pipeline shouldn't share:

```csharp
.WriteTo.Logger(lc => lc
    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
    .WriteTo.File("logs/errors-.log", rollingInterval: RollingInterval.Day))
.WriteTo.Console()
```

Here, everything still goes to the console at the pipeline's overall minimum level, while only
`Error`/`Fatal` events additionally get written to a separate error-only file — the sub-logger's
filter doesn't affect what the console sink receives.

## Event filtering with Filter.ByExcluding / ByIncludingOnly

Filters operate on the whole event (not just its level), so they're the right tool for excluding
events by property value or message content rather than severity alone:

```csharp
.Filter.ByExcluding(e => Matching.FromSource("Microsoft.AspNetCore.StaticFiles").Invoke(e))
.Filter.ByExcluding(e => e.Properties.TryGetValue("RequestPath", out var path) && path.ToString().Contains("/health"))
```

The second example is the standard pattern for silencing health-check endpoint noise from request
logging — filtering on a property Serilog's ASP.NET Core integration already attaches to each
request-logging event, rather than trying to prevent the event from being raised in the first
place.

## Choosing the right mechanism

| Need | Mechanism |
| --- | --- |
| Globally raise/lower verbosity for the whole app | `MinimumLevel.<Level>()` |
| Silence or increase verbosity for one namespace | `MinimumLevel.Override(prefix, level)` |
| Change verbosity while the process keeps running | `LoggingLevelSwitch` + `MinimumLevel.ControlledBy` |
| Send a subset of events to a dedicated sink with its own rules | `WriteTo.Logger(...)` |
| Drop events matching an arbitrary condition regardless of level | `Filter.ByExcluding`/`ByIncludingOnly` |
