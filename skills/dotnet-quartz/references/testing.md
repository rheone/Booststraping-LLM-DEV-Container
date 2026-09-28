# Testing

Most value comes from testing a job's `Execute` logic directly and testing that your DI registration
wires up the jobs and triggers you expect — not from running a real scheduler against real time.

## Unit test a job's Execute method directly

```csharp
[Fact]
public async Task Execute_SendsDigestToConfiguredRecipient()
{
    var digestService = Substitute.For<IDigestService>();
    var logger = NullLogger<SendDailyDigestJob>.Instance;
    var job = new SendDailyDigestJob(digestService, logger);

    var context = Substitute.For<IJobExecutionContext>();

    await job.Execute(context, CancellationToken.None);

    await digestService.Received(1).SendAsync(Arg.Any<CancellationToken>());
}
```

Because a job is a plain class with constructor-injected dependencies, test it exactly like any
other class with injected dependencies — instantiate it directly, substitute its dependencies, and
call `Execute` with a fake or substituted `IJobExecutionContext`. No scheduler, trigger, or job store
needs to be involved to verify the job's own logic.

## Testing JobDataMap-driven behavior

```csharp
[Fact]
public async Task Execute_UsesRecipientFromJobDataMap()
{
    var job = new SendReportJob(digestService) { RecipientEmail = "test@example.com" };
    var context = Substitute.For<IJobExecutionContext>();

    await job.Execute(context, CancellationToken.None);

    await digestService.Received(1).SendAsync("test@example.com", Arg.Any<CancellationToken>());
}
```

Set the job's data-map-backed properties directly on the instance under test rather than routing
through a real `JobDataMap`/merge process — the merge behavior itself is Quartz.NET's own
responsibility, not something your test suite needs to re-verify.

## Verifying a cron expression's scheduling semantics

```csharp
[Fact]
public void CronExpression_FiresAt8AmDaily()
{
    var expression = new CronExpression("0 0 8 * * ?");
    var next = expression.GetNextValidTimeAfter(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    Assert.Equal(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero), next);
}
```

`CronExpression.GetNextValidTimeAfter` computes the next fire time for a given expression without
starting a scheduler at all — use it to assert a cron string means what you think it means,
especially for an expression complex enough that a small mistake (day-of-week vs. day-of-month, a
`?` in the wrong field) would otherwise only surface as a job never firing in production.

## Integration testing with an in-memory store

```csharp
IScheduler scheduler = await QuartzSchedulerBuilder
    .Create(q => q.UseInMemoryStore())
    .BuildScheduler();

await scheduler.Start();
await scheduler.ScheduleJob(job, trigger);
// advance a fake clock or poll scheduler.GetCurrentlyExecutingJobs() as appropriate
await scheduler.Shutdown();
```

Reserve a real scheduler run (even against the in-memory store) for verifying wiring — that a
specific job and trigger pair actually gets registered and fires — rather than for testing the
job's own business logic, which the direct unit test above already covers faster and more
deterministically. Avoid asserting on wall-clock timing in this kind of test; assert on
`GetCurrentlyExecutingJobs()` or a callback the job itself records having run, not on `Task.Delay`
guesses about when a trigger should have fired.
