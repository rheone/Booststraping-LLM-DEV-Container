---
name: dotnet-hangfire
description: Guidance on Hangfire (verified current release 1.8.25) for background job processing in .NET — fire-and-forget, delayed, recurring, and continuation jobs via BackgroundJob.Enqueue/Schedule/ContinueJobWith and RecurringJob.AddOrUpdate, the storage provider concept behind job persistence, the Hangfire Dashboard, job filters and retry behavior, and testing code that enqueues jobs without running the job pipeline. Use when scheduling a background job, wiring up recurring jobs, configuring job storage or the dashboard, customizing retry/filter behavior, or writing a unit test that verifies a job was enqueued with the right arguments.
license: LGPL-3.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Hangfire

Guidance on Hangfire, the background job library for .NET that lets an ASP.NET Core or any .NET
process enqueue, schedule, and process jobs against durable storage — no separate Windows Service
or worker process required. Organized by task, not by version.

Hangfire's core license carries a non-standard commercial tier alongside its open-source
license — research current terms independently before adopting it for a commercial project.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Enqueuing a fire-and-forget, delayed, recurring, or continuation job | [references/core-concepts.md](references/core-concepts.md) |
| Choosing and configuring where jobs and their state persist | [references/storage-providers.md](references/storage-providers.md) |
| Standing up and securing the Hangfire Dashboard | [references/dashboard.md](references/dashboard.md) |
| Customizing retry behavior or writing a job filter | [references/job-filters-and-retry.md](references/job-filters-and-retry.md) |
| Unit testing code that enqueues a job, without running the job pipeline | [references/testing.md](references/testing.md) |

## Quick start

Enqueuing and scheduling jobs (current API, v1.8.25):

```csharp
// Fire-and-forget: runs as soon as a worker is free
BackgroundJob.Enqueue<IEmailSender>(sender => sender.SendWelcomeEmail(userId));

// Delayed: runs no earlier than the given interval from now
BackgroundJob.Schedule<IEmailSender>(sender => sender.SendReminderEmail(userId), TimeSpan.FromDays(1));

// Recurring: runs on a cron schedule
RecurringJob.AddOrUpdate<IReportGenerator>(
    "daily-sales-report",
    generator => generator.GenerateDailySalesReport(),
    Cron.Daily);
```

Every job method call is captured as an expression tree, not invoked immediately — Hangfire
serializes the method call (type, method, arguments) into storage and invokes it later on a worker.
Keep job method arguments to simple, serializable values (IDs, primitives, small DTOs) rather than
passing a live object graph or an injected service instance as an argument.

## Out of scope

- Deep configuration of any one storage provider (SQL Server index tuning, Redis cluster topology,
  a specific provider's connection-string options) — [storage-providers.md](references/storage-providers.md)
  covers the storage-provider concept generically; a specific backend's own operational tuning is a
  separate concern.
- Building a custom dashboard UI or extending the dashboard's own metrics/authorization pipeline
  beyond the standard authorization filter shown in [dashboard.md](references/dashboard.md).
- Running the actual job pipeline end-to-end in a test (spinning up a real or in-memory storage
  backend and letting workers process jobs) — [testing.md](references/testing.md) covers verifying
  that a job was enqueued correctly, not exercising the job processing pipeline itself.
