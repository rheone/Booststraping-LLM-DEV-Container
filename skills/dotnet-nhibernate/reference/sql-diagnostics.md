# SQL Diagnostics & Logging

Everything else in this skill helps reason about NHibernate statically. Debugging a live N+1, an unexpected query shape, or a "why is this slow" report needs to *see* the SQL NHibernate actually generated — this file covers turning that visibility on.

## Turning on SQL logging

NHibernate logs through whatever logging framework the host app has wired up (`Microsoft.Extensions.Logging`, log4net, etc., via `NHibernate.Cfg.Environment` / `NHibernate.Bytecode` provider configuration). Two settings matter:

- `show_sql` (`Environment.ShowSql`) — writes generated SQL to whatever logger is configured. Verbose; turn on for a specific debugging session or a scoped log category, not globally in production.
- Log category `NHibernate.SQL` at `Debug`/`Trace` level — the more controllable option in an ASP.NET Core app already using `Microsoft.Extensions.Logging`, since it can be scoped per-environment via standard logging configuration rather than a compile-time/config-file flag:

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "NHibernate.SQL": "Debug",
      "NHibernate": "Warning"
    }
  }
}
```

Set `NHibernate` (the general category) to `Warning` while `NHibernate.SQL` is at `Debug` — the general category at `Debug` is extremely noisy (session lifecycle internals, not just SQL) and drowns out the SQL you actually wanted to see.

## Reading generated SQL for parameter values

By default, logged SQL shows parameter placeholders (`@p0`, `@p1`), not the bound values — set `Environment.FormatSql` alongside a parameter-logging setting, or use a `IInterceptor.OnPrepareStatement` hook to log the fully-bound statement, if you need to see actual values rather than placeholders. Be deliberate about this in anything other than local development — logging bound parameter values can leak sensitive data (PII, in particular) into logs that a debugging session doesn't need turned on by default.

## NHibernate Profiler (or equivalent APM/tracing)

For anything beyond a quick local check, a dedicated profiler (NHibernate Profiler, or generic APM tooling with ADO.NET instrumentation) gives you round-trip counts, duplicate-query detection, and timing per statement — this is the fastest way to actually confirm an N+1 suspicion (see `lazy-loading-and-fetching.md`) rather than counting log lines by eye. If the team has an APM tool already instrumented at the ADO.NET/`DbCommand` level (Application Insights, Datadog, etc.), NHibernate's generated SQL shows up there for free — check before reaching for a dedicated NHibernate-specific tool.

## Correlating SQL logs with a request

In a session-per-request setup (see `session-lifecycle.md`), tag the logging scope with the request's correlation/trace ID at the point the session opens, so a burst of SQL statements in the log can be attributed back to the request that caused them — without this, diagnosing "which request caused this N+1" in production logs under load is much harder than it needs to be.

## What to look for once you can see the SQL

- **Repeated identical query shape with different parameter values** — the signature of N+1; count matches the number of loop iterations, not the number of logical operations the code intended.
- **A single query far larger/more joined than expected** — often a `.Fetch()`/`.FetchMany()` stacking multiple collections and hitting the cartesian-product problem noted in `lazy-loading-and-fetching.md`.
- **A flush appearing mid-transaction where none was expected** — check `FlushMode` (see `session-lifecycle.md`) if a query's auto-flush is triggering writes earlier than the code's logical structure suggests.
