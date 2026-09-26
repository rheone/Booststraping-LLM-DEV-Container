# Quartz.NET

Guidance on Quartz.NET, the job scheduling library for .NET — the routing table (by task, not
Quartz.NET version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Quartz.NET version

| File | Covers |
| --- | --- |
| `jobs.md` | `IJob`, `Execute`, `IJobExecutionContext` |
| `triggers.md` | Cron triggers, simple triggers, and their scheduling semantics |
| `scheduler.md` | `ISchedulerFactory`, `IScheduler`, starting/stopping/pausing |
| `job-data-maps.md` | `JobDataMap`, passing parameters into a job at schedule and trigger level |
| `misfire-handling.md` | Misfire instructions per trigger type and when to choose each |
| `dependency-injection.md` | `AddQuartz`, `AddQuartzHostedService`, resolving DI-scoped dependencies in a job |
| `testing.md` | Testing `IJob` implementations and DI-registered schedules |

## Scope

Quartz.NET only — in-process job scheduling: jobs, triggers, the scheduler lifecycle, and DI
integration. Out of scope: multi-node scheduler clustering and building a custom job store (see
[SKILL.md](SKILL.md) for why).

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill. Quartz.NET 4.x requires .NET 10 and folds `Quartz.Extensions.Hosting`/
`Quartz.Extensions.DependencyInjection` into the main `Quartz` package — an application on an
earlier .NET version uses the 3.x package layout instead.
