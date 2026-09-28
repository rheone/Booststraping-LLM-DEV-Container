---
name: dotnet-quartz
description: Guidance on Quartz.NET (verified current Apache-2.0 release 4.1.1, targeting .NET 10) for job scheduling in .NET — implementing IJob, cron and simple triggers and their scheduling semantics, the ISchedulerFactory/IScheduler lifecycle, JobDataMap for passing parameters, misfire handling policies, and dependency injection integration via AddQuartz/AddQuartzHostedService. Use when scheduling a recurring or one-off job, choosing between a cron and a simple trigger, starting or stopping a scheduler, passing data into a job, reasoning about misfire behavior, or wiring Quartz into an ASP.NET Core / generic host application.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Quartz.NET

Guidance on Quartz.NET, the job scheduling library for .NET. Organized by task, not by version —
the current `AddQuartz`/`AddQuartzHostedService` DI-registration API (folded into the main `Quartz`
package as of v4.0) is the API this skill documents throughout.

Quartz.NET 4.x requires .NET 10 or later and has no `netstandard2.0` build — an application still
targeting an earlier .NET version stays on the Quartz.NET 3.x line, whose DI integration lived in
separate `Quartz.Extensions.Hosting`/`Quartz.Extensions.DependencyInjection` packages instead of the
main package.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Implementing `IJob` and understanding `IJobExecutionContext` | [references/jobs.md](references/jobs.md) |
| Choosing and configuring a cron trigger or a simple trigger | [references/triggers.md](references/triggers.md) |
| Starting, stopping, and working with `ISchedulerFactory`/`IScheduler` | [references/scheduler.md](references/scheduler.md) |
| Passing parameters into a job via `JobDataMap` | [references/job-data-maps.md](references/job-data-maps.md) |
| Choosing a misfire instruction for a trigger | [references/misfire-handling.md](references/misfire-handling.md) |
| Wiring Quartz into an ASP.NET Core or generic host app | [references/dependency-injection.md](references/dependency-injection.md) |
| Testing jobs, triggers, and DI-registered schedules | [references/testing.md](references/testing.md) |

## Quick start

Registering a job on a cron schedule through the hosted-service integration (current API, v4.1.1 —
`AddQuartz`/`AddQuartzHostedService` extend `HostApplicationBuilder`/`WebApplicationBuilder`
directly, not `IServiceCollection`):

```csharp
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

builder.AddQuartz(q =>
{
    var jobKey = new JobKey("SendDailyDigest");

    q.AddJob<SendDailyDigestJob>(j => j.WithIdentity(jobKey));

    q.AddTrigger(t => t
        .ForJob(jobKey)
        .WithIdentity("SendDailyDigest-trigger")
        .WithCronSchedule("0 0 8 * * ?"));
});

builder.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

var host = builder.Build();
await host.RunAsync();

public sealed class SendDailyDigestJob : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // send the digest
    }
}
```

`AddQuartzHostedService` registers an `IHostedService` that starts every scheduler configured
through `AddQuartz` when the host starts, and stops it gracefully on shutdown —
`WaitForJobsToComplete = true` lets an in-flight job finish before the host completes shutdown
instead of aborting it mid-execution. `IJob.Execute` takes a `CancellationToken` and returns
`ValueTask` as of v4.0 — honor the token so a job cooperates with host shutdown instead of running
past it.

## Out of scope

- Clustering a scheduler across multiple nodes with a shared job store (AdoJobStore clustering,
  node failover) — a deep operational domain of its own beyond the single-process scheduling this
  skill covers.
- Building a custom `IJobStore` or persistence provider from scratch.
- Quartz.NET 3.x's separate `Quartz.Extensions.Hosting`/`Quartz.Extensions.DependencyInjection`
  package APIs — this skill documents the 4.x package layout where that surface lives in the main
  `Quartz` package.
