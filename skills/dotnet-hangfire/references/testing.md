# Testing

The goal in most tests is verifying that your code enqueued the *right* job with the *right*
arguments — not exercising Hangfire's own storage and worker pipeline, which is Hangfire's own
concern, not your application's.

## Depend on IBackgroundJobClient, not the static BackgroundJob class

The static `BackgroundJob.Enqueue`/`Schedule` methods resolve their client from a global
`JobStorage.Current` context, which is awkward to substitute in a unit test. Inject
`IBackgroundJobClient` into any class that enqueues jobs instead of calling the static methods
directly:

```csharp
public class OrderService(IBackgroundJobClient jobClient)
{
    public void SubmitOrder(Guid orderId)
    {
        jobClient.Enqueue<IEmailSender>(sender => sender.SendWelcomeEmail(orderId));
    }
}
```

`IBackgroundJobClient.Enqueue<T>` is the same extension method surface as the static
`BackgroundJob.Enqueue<T>`, so call sites read identically — the only change is where the client
instance comes from.

## Asserting the job was enqueued with the right method and arguments

```csharp
[Fact]
public void SubmitOrder_EnqueuesWelcomeEmail_ForTheGivenOrder()
{
    var jobClient = Substitute.For<IBackgroundJobClient>();
    var orderId = Guid.NewGuid();

    var service = new OrderService(jobClient);
    service.SubmitOrder(orderId);

    jobClient.Received(1).Create(
        Arg.Is<Job>(job =>
            job.Type == typeof(IEmailSender)
            && job.Method.Name == nameof(IEmailSender.SendWelcomeEmail)
            && (Guid)job.Args[0] == orderId),
        Arg.Any<EnqueuedState>());
}
```

`Enqueue<T>(expression)` is itself a thin wrapper around `IBackgroundJobClient.Create(Job, IState)`
— asserting on the underlying `Create` call lets you inspect the captured `Job` (its target type,
method, and argument values) and the `IState` it was created with (`EnqueuedState` for
`Enqueue`, `ScheduledState` for `Schedule`) without a real storage backend or worker ever running.

## Asserting a recurring job registration

Wrap `RecurringJob.AddOrUpdate` behind an injectable abstraction the same way, and assert the call
captured the expected job identifier and cron expression:

```csharp
public interface IRecurringJobScheduler
{
    void AddOrUpdate<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression);
}
```

Testing that a cron expression itself fires at the expected times is Hangfire's own responsibility,
not something your test suite needs to re-verify — assert only that your code registered the
expected identifier, target method, and cron string.

## What still deserves an integration test

Reserve a real storage-backed test (a real or disposable instance of your chosen backend) for
verifying end-to-end behavior no substitute can stand in for — that a job actually persists across
an application restart, or that your dashboard authorization filter behaves correctly against a real
`HttpContext`. Keep these few; the large majority of enqueue-time logic is fully covered by
asserting against `IBackgroundJobClient` as shown above.
