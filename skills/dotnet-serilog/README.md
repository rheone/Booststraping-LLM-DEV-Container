# Serilog

Guidance on Serilog, the structured logging library for .NET — the routing table (by task, not
Serilog version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Serilog version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `LoggerConfiguration`, `Log.Logger`, the static vs. injected logger choice |
| `structured-logging.md` | message templates, named properties, `@` destructuring, `$` stringification |
| `sinks.md` | `WriteTo.Console`, `WriteTo.File`, rolling files, general sink-configuration shape |
| `enrichers.md` | `Enrich.FromLogContext`, built-in enrichers, custom `ILogEventEnricher` |
| `levels-and-filtering.md` | minimum level, per-source overrides, sub-loggers, `Filter.ByExcluding` |
| `aspnetcore-integration.md` | `UseSerilog`, `UseSerilogRequestLogging`, two-stage initialization |
| `testing.md` | asserting on emitted log events, in-memory sinks, avoiding brittle message-string assertions |

## Scope

Serilog only — structured event logging within a .NET application. Out of scope: other logging
libraries, distributed-tracing pipeline setup beyond what an enricher attaches to an event, and
administering the backend service a sink writes to (see [SKILL.md](SKILL.md) for why).

Each reference file notes a version-introduced fact inline (e.g. Serilog.AspNetCore's current
10.0.0 release); version is not the file-splitting axis for this skill (see [SKILL.md](SKILL.md)).
