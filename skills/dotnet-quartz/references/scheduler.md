# Scheduler

The scheduler is the running engine that tracks registered jobs and triggers and fires jobs when
their triggers say to. In a DI-integrated application (see
[dependency-injection.md](dependency-injection.md)) the hosted service manages this for you; outside
DI, you build and control it directly.

## Building a scheduler without DI

```csharp
using Quartz;

IScheduler scheduler = await QuartzSchedulerBuilder
    .Create(q => q
        .ConfigureScheduler(options => options.InstanceName = "MyScheduler")
        .UseDefaultThreadPool(maxConcurrency: 5)
        .UseInMemoryStore())
    .BuildScheduler();

await scheduler.Start();
```

`QuartzSchedulerBuilder.Create(...).BuildScheduler()` is the current entry point for building a
scheduler outside a DI container — it replaces the older factory-based construction pattern with a
builder that assembles the scheduler's configuration directly.

## Scheduling a job and trigger

```csharp
IJobDetail job = JobBuilder.Create<SendDailyDigestJob>()
    .WithIdentity("SendDailyDigest")
    .Build();

ITrigger trigger = TriggerBuilder.Create()
    .WithIdentity("SendDailyDigest-trigger")
    .WithCronSchedule("0 0 8 * * ?")
    .Build();

await scheduler.ScheduleJob(job, trigger);
```

## Stopping a scheduler

```csharp
await scheduler.Shutdown(waitForJobsToComplete: true);
```

`waitForJobsToComplete: true` lets any currently executing job finish before `Shutdown` returns;
`false` returns as soon as the shutdown signal is issued, which can abandon an in-flight job
mid-execution. Prefer `true` for any job whose partial execution would leave inconsistent state.

## Pausing and resuming

```csharp
await scheduler.PauseJob(jobKey);
await scheduler.ResumeJob(jobKey);

await scheduler.PauseAll();
await scheduler.ResumeAll();
```

Pausing a job prevents its triggers from firing without removing the job or its trigger
registrations — use this for temporarily disabling a job (a maintenance window) where you intend to
resume it later, rather than deleting and re-creating its registration.

## One scheduler instance per logical scheduling concern

A single process can host more than one named scheduler (each with its own `InstanceName`) when
different job sets need genuinely independent configuration (different thread pools, different job
stores). Most applications need only one scheduler; introduce a second only when two sets of jobs
have configuration requirements that can't share a single scheduler's settings.
