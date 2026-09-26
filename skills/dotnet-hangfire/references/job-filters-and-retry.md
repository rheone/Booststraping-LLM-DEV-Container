# Job Filters and Retry

Hangfire applies `AutomaticRetryAttribute` to every job by default and lets you write your own job
filters for cross-cutting behavior around job execution.

## Default retry behavior

A job that throws an unhandled exception is automatically retried, by default up to 10 times, with
an increasing delay between attempts. After the retry limit is exhausted, the job moves to the
`Failed` state and stops retrying on its own — it stays visible (and manually requeueable) in the
dashboard rather than disappearing.

## Configuring retry per job

```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 30, 60 })]
public class ReportGenerator : IReportGenerator
{
    public void GenerateDailySalesReport() { /* ... */ }
}
```

Apply `[AutomaticRetry]` at the class or method level to override the default attempt count and
delay sequence for that job. Set `Attempts = 0` to disable retry entirely for a job whose failure
should surface immediately rather than retry (an operation that isn't idempotent and can't safely
run twice).

## Configuring retry globally

```csharp
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 5 });
```

A global filter applies to every job that doesn't specify its own `[AutomaticRetry]` override — set
an organization-wide default here rather than repeating the same attribute on every job class.

## Writing a custom job filter

```csharp
public class LogJobExecutionFilter : JobFilterAttribute, IServerFilter
{
    public void OnPerforming(PerformingContext context) =>
        Log.Information("Starting job {JobId}", context.BackgroundJob.Id);

    public void OnPerformed(PerformedContext context) =>
        Log.Information("Finished job {JobId}", context.BackgroundJob.Id);
}
```

Implement `IServerFilter` for behavior around job execution (logging, metrics, a custom timeout), or
`IElectStateFilter`/`IApplyStateFilter` for behavior that reacts to or changes a job's state
transitions (e.g. custom failure notification). Register a custom filter globally the same way as
`AutomaticRetryAttribute`, or apply it as an attribute directly on a job class/method.

## Idempotency matters more than retry configuration

Because any job can run more than once — through automatic retry, a dashboard-triggered requeue, or
in rare cases a worker crash after starting but before Hangfire records completion — write every job
method to be safe to execute twice with the same arguments. A job that isn't idempotent (one that
sends an email or charges a payment with no de-duplication check) needs its own guard against
double-execution regardless of how retry is configured.
