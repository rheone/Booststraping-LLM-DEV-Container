# Hangfire

Hangfire lets a .NET process enqueue, schedule, and process background jobs against durable
storage, without standing up a separate worker service. This skill covers fire-and-forget,
delayed, recurring, and continuation jobs, the storage-provider concept, the dashboard, and job
filters and retries.

> [!NOTE]
> Hangfire's core license carries a non-standard commercial tier alongside its open-source license. Research current terms independently before adopting it.

## When to reach for it

- You need a job to run once, later, on a recurring schedule, or after another job finishes.
- You're deciding where jobs and their state should persist (SQL Server, Redis, another provider).
- You're standing up the Hangfire Dashboard and need it locked down before it's reachable.
- A job keeps failing and you need custom retry behavior or a filter that runs around every job.
- You want to assert a job was enqueued with the right arguments, without running the job pipeline.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches scheduling, storage,
the dashboard, or retry/filter behavior for a Hangfire job. You can also invoke it directly by
name.

## What it covers

| Topic | Reference |
| --- | --- |
| `BackgroundJob.Enqueue`/`Schedule`/`ContinueJobWith`, `RecurringJob.AddOrUpdate` | [references/core-concepts.md](references/core-concepts.md) |
| The storage-provider concept behind job and state persistence | [references/storage-providers.md](references/storage-providers.md) |
| `UseHangfireDashboard` and dashboard authorization | [references/dashboard.md](references/dashboard.md) |
| `IJobFilter`, `AutomaticRetryAttribute`, retry/backoff behavior | [references/job-filters-and-retry.md](references/job-filters-and-retry.md) |
| Asserting a job was enqueued with the right method and arguments | [references/testing.md](references/testing.md) |

## Example prompts

- "Enqueue a fire-and-forget job that sends a welcome email after signup."
- "Lock down the Hangfire Dashboard so only admins can view it."
- "Write a job filter that retries a failed job with exponential backoff."
