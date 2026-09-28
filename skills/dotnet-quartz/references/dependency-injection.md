# Dependency Injection Integration

As of Quartz.NET 4.0, DI and generic-host integration ship in the main `Quartz` package —
`AddQuartz` and `AddQuartzHostedService` extend `HostApplicationBuilder`/`WebApplicationBuilder`
directly. (An application still on Quartz.NET 3.x uses the separate
`Quartz.Extensions.Hosting`/`Quartz.Extensions.DependencyInjection` packages and extends
`IServiceCollection` instead — the configuration shape below is otherwise the same.)

## Registering the scheduler and jobs

```csharp
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

builder.AddQuartz(q =>
{
    q.UsePersistentStore(s => { /* configure a durable job store */ });

    var jobKey = new JobKey("SendDailyDigest");
    q.AddJob<SendDailyDigestJob>(j => j.WithIdentity(jobKey));
    q.AddTrigger(t => t.ForJob(jobKey).WithCronSchedule("0 0 8 * * ?"));
});

builder.AddQuartzHostedService(opts =>
{
    opts.WaitForJobsToComplete = true;
    opts.AwaitApplicationStarted = true;
});

var host = builder.Build();
await host.RunAsync();
```

`AddQuartzHostedService` can be called before or after `AddQuartz` — it registers the hosted service
that starts every scheduler configured through `AddQuartz` when the host starts, and stops them on
shutdown. `AwaitApplicationStarted` (default `true`) delays scheduler start until the host itself has
finished starting, so a job can't fire before the rest of the application's startup has completed.

## Injecting services into a job

```csharp
public sealed class SendDailyDigestJob : IJob
{
    private readonly IDigestService _digestService;
    private readonly ILogger<SendDailyDigestJob> _logger;

    public SendDailyDigestJob(IDigestService digestService, ILogger<SendDailyDigestJob> logger)
    {
        _digestService = digestService;
        _logger = logger;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        await _digestService.SendAsync(cancellationToken);
    }
}
```

The DI-aware job factory opens a new DI scope for each fire, resolves the job (and everything it
depends on) from that scope, and disposes the scope when `Execute` returns — a job can safely
constructor-inject a scoped service (a `DbContext`, a scoped repository) exactly as a controller
action would, without manually managing a scope itself.

## Registering job dependencies

Register a job's own dependencies with the same `IServiceCollection` your host already uses
(`builder.Services.AddScoped<IDigestService, DigestService>()`) — the job factory resolves from that
same container, so no separate registration mechanism exists specific to Quartz.

## What a job's constructor cannot resolve

A job cannot constructor-inject `IScheduler`, `ISchedulerFactory`, or other scheduler-internal
types — obtain those from `context.Scheduler` inside `Execute` instead (see
[scheduler.md](scheduler.md) and [jobs.md](jobs.md)).
