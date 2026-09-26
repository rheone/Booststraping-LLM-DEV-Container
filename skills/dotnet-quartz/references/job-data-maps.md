# Job Data Maps

A `JobDataMap` carries parameters into a job — key/value data attached at the job level, the trigger
level, or both, that the job reads when it executes.

## Setting data when registering a job

```csharp
q.AddJob<SendReportJob>(j => j
    .WithIdentity("SendReport")
    .UsingJobData(x => x.RecipientEmail, "reports@example.com"));
```

The strongly-typed `UsingJobData(x => x.Property, value)` overload requires the job class to expose
a public settable property of a matching name and type — the DI-aware job factory copies each
matching entry from the merged data map onto the job instance's property before `Execute` runs.

## Setting data on a trigger

```csharp
q.AddTrigger(t => t
    .ForJob(jobKey)
    .WithIdentity("SendReport-weekly")
    .UsingJobData(x => x.RecipientEmail, "weekly-recipient@example.com")
    .WithCronSchedule("0 0 8 * * MON"));
```

Trigger-level data overrides job-level data of the same key when the two are merged — this is how
one job definition serves several triggers with different parameters (a shared `SendReportJob`, one
trigger per recipient).

## Reading data in the job

```csharp
public sealed class SendReportJob : IJob
{
    public string RecipientEmail { get; set; } = "";

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        await SendAsync(RecipientEmail, cancellationToken);
    }
}
```

Or read the map explicitly instead of relying on property injection:

```csharp
JobDataMap dataMap = context.MergedJobDataMap;
string recipient = dataMap.GetString("RecipientEmail") ?? "";
```

`MergedJobDataMap` combines the job's own data with the firing trigger's data, trigger values taking
precedence for any key present in both.

## What belongs in a job data map

Keep entries to small, serializable values — strings, numbers, booleans, GUIDs, small DTOs. A
persistent job store serializes the data map to storage, so a live object reference (a connection, a
service instance) does not survive there; resolve services through the job's constructor instead
(see [dependency-injection.md](dependency-injection.md)) and reserve the data map for the data a job
needs to know, not the services it needs to call.

## Job data vs. constructor injection

Use constructor injection for services a job depends on (a repository, an `ILogger<T>`, an HTTP
client) and the job data map for values specific to a particular scheduled instance of that job
(which recipient, which report ID, which threshold). Conflating the two — passing a service through
job data, or hardcoding a per-trigger value into the job class instead of parameterizing it — makes
the job harder to reuse across multiple triggers and harder to test in isolation.
