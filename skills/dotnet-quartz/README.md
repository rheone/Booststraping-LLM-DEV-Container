# Quartz.NET

Quartz.NET schedules jobs to run once, on a recurring cadence, or according to a cron expression,
inside an ASP.NET Core or generic host application. This skill covers implementing `IJob`,
choosing between cron and simple triggers, the scheduler lifecycle, passing data into a job, and
misfire handling.

## When to reach for it

- You're deciding between a cron trigger and a simple trigger for a recurring job.
- A job needs parameters passed in at schedule time and you're reaching for `JobDataMap`.
- The scheduler missed a fire time (the process was down, a job ran long) and you need to pick a misfire policy.
- You're wiring Quartz into a host app's DI container and want jobs to resolve scoped dependencies correctly.
- You want a test around a job's execution logic or a DI-registered schedule.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches scheduling a job,
triggers, the scheduler lifecycle, or Quartz's DI integration. You can also invoke it directly by
name.

## What it covers

| Topic | Reference |
| --- | --- |
| `IJob`, `Execute`, `IJobExecutionContext` | [references/jobs.md](references/jobs.md) |
| Cron triggers, simple triggers, and their scheduling semantics | [references/triggers.md](references/triggers.md) |
| `ISchedulerFactory`, `IScheduler`, starting/stopping/pausing | [references/scheduler.md](references/scheduler.md) |
| `JobDataMap`, passing parameters into a job at schedule and trigger level | [references/job-data-maps.md](references/job-data-maps.md) |
| Misfire instructions per trigger type and when to choose each | [references/misfire-handling.md](references/misfire-handling.md) |
| `AddQuartz`, `AddQuartzHostedService`, resolving scoped dependencies in a job | [references/dependency-injection.md](references/dependency-injection.md) |
| Testing `IJob` implementations and DI-registered schedules | [references/testing.md](references/testing.md) |

## Example prompts

- "Schedule a job that runs every morning at 8am using a cron trigger."
- "Pass a customer ID into this job through the JobDataMap."
- "The scheduler was down over the weekend and missed a fire: what misfire instruction stops it from running the job three times to catch up?"
