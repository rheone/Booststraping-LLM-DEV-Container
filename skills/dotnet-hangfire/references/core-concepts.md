# Core Concepts

Hangfire captures a method call as a job, persists it to storage, and lets a worker process invoke
it later — the four job types below cover when that invocation happens.

## Fire-and-forget

```csharp
BackgroundJob.Enqueue<IEmailSender>(sender => sender.SendWelcomeEmail(userId));
```

Enqueues the job for immediate processing by the next available worker. Use this for work that
should happen as soon as possible but doesn't need to block the calling request — sending an email,
generating a thumbnail, invalidating a cache entry.

## Delayed

```csharp
BackgroundJob.Schedule<IEmailSender>(sender => sender.SendReminderEmail(userId), TimeSpan.FromDays(1));
```

Schedules the job to become eligible for processing no earlier than the given delay (or an absolute
`DateTimeOffset`) from now. Hangfire's scheduler polls for due delayed jobs and enqueues them when
their time arrives — the delay is a lower bound, not a guarantee of exact-time execution.

## Recurring

```csharp
RecurringJob.AddOrUpdate<IReportGenerator>(
    "daily-sales-report",
    generator => generator.GenerateDailySalesReport(),
    Cron.Daily);
```

Registers a job under a stable string identifier (`"daily-sales-report"`) that runs on the given
cron schedule indefinitely, until removed with `RecurringJob.RemoveIfExists`. Calling `AddOrUpdate`
again with the same identifier updates that job's schedule or target method rather than creating a
duplicate — this makes it safe to call from application startup on every deploy. `Cron` exposes
common schedules (`Cron.Daily()`, `Cron.Hourly()`, `Cron.Weekly()`) plus `Cron.Daily(hour, minute)`
overloads; anything more specific takes a raw five-field cron expression string.

## Continuations

```csharp
var jobId = BackgroundJob.Enqueue<IReportGenerator>(g => g.GenerateReport());
BackgroundJob.ContinueJobWith<IEmailSender>(jobId, sender => sender.EmailReport());
```

Chains a job to run only after a specific parent job completes successfully. By default a
continuation does not run if the parent job fails; pass `JobContinuationOptions.OnAnyFinishedState`
to run it regardless of the parent's outcome (useful for cleanup work that must happen either way).

## Why a job call is an expression, not an invocation

`BackgroundJob.Enqueue<T>(x => x.Method(args))` never calls `Method` on the spot — it inspects the
expression tree to capture the type, method, and argument values, serializes them into storage, and
resolves a fresh instance of `T` from your DI container when a worker actually processes the job.
Job method arguments must be simple, serializable values; passing an injected service, a
`DbContext`, or any other live/non-serializable object as an argument fails at enqueue time or
produces a stale/broken reference when the job later deserializes and runs.

## Passing state between an enqueue call and the eventual worker

Because the job body re-resolves its dependencies from DI at execution time rather than closing
over the state present when you called `Enqueue`, any per-request context the job needs (a user ID,
an order ID) must be passed as an explicit method argument — not read from ambient state (a
request-scoped service, `HttpContext`) that no longer exists by the time a worker picks the job up,
possibly seconds or days later.
