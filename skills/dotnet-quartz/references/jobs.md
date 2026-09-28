# Jobs

A job is the unit of work Quartz.NET schedules and executes — a class implementing `IJob`, resolved
fresh (or from your DI container) for each fire of a trigger pointing at it.

## Implementing IJob

```csharp
public sealed class SendDailyDigestJob : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        await Console.Out.WriteLineAsync("Sending daily digest...");
    }
}
```

As of v4.0, `Execute` returns `ValueTask` and takes a `CancellationToken` — every asynchronous
member across the library follows this shape. Pass the token into any awaited call inside `Execute`
so the job cooperates with a scheduler shutdown or job interruption instead of running to completion
regardless of it.

## IJobExecutionContext

`context` carries everything about the current firing: `context.JobDetail` (the registered job's
key, description, and durable job data), `context.Trigger` (the trigger that caused this fire),
`context.MergedJobDataMap` (job data merged with trigger-level overrides — see
[job-data-maps.md](job-data-maps.md)), `context.FireTimeUtc`/`NextFireTimeUtc`, and
`context.Result`, which you can set to a value the scheduler passes to a job listener.

## Jobs are stateless between fires

A job instance is discarded after `Execute` returns and a new one is created (or resolved from DI)
for the next fire — never store per-execution state in an instance field expecting it to persist to
the next run. Anything a job needs to remember across fires belongs in external storage (a
database, the job data map for values that don't change per fire), not in the job object itself.

## Jobs cannot take scheduler-related constructor dependencies

A registered job's constructor cannot resolve `IScheduler`, `ISchedulerFactory`, or other
scheduler-internal types — the job container that builds jobs does not expose them. If a job
genuinely needs to interact with the scheduler (rescheduling itself, triggering another job), obtain
`context.Scheduler` from the `IJobExecutionContext` passed into `Execute` instead of injecting it.

## Concurrent execution

By default, multiple triggers pointing at the same job type (or the same trigger firing again
before the previous fire finished, for a long-running job with a frequent schedule) can execute
concurrently. Decorate a job class that must not run concurrently with itself with
`[DisallowConcurrentExecution]` — the scheduler then holds a second fire until the first completes
rather than running both at once.

## PersistJobDataAfterExecution

By default, changes a job makes to its own `JobDataMap` during `Execute` do not persist to the next
fire. Add `[PersistJobDataAfterExecution]` when a job intentionally uses its data map as small
persisted state between fires (a counter, a last-processed watermark) — combine it with
`[DisallowConcurrentExecution]` so two concurrent fires can't race on updating the same persisted
value.
