# Testing

A test that lets a retry strategy actually sleep between attempts, or lets a timeout strategy wait
out a real `TimeSpan`, is slow and flaky by construction — the whole point of testing resilience
behavior is to verify the *logic* (did it retry the right number of times, did the circuit open),
not to spend real wall-clock time proving a `Task.Delay` works.

## Inject a TimeProvider instead of using real time

Every timing-sensitive strategy option (`RetryStrategyOptions`, `CircuitBreakerStrategyOptions`,
`TimeoutStrategyOptions`, `HedgingStrategyOptions`) accepts a `TimeProvider` on the pipeline builder
via `ResiliencePipelineBuilder.TimeProvider`. Supply a fake/manually advanced `TimeProvider` (e.g.
`Microsoft.Extensions.Time.Testing.FakeTimeProvider`) in tests instead of the real
`TimeProvider.System`:

```csharp
[Fact]
public async Task Retry_ThreeFailures_RetriesThreeTimesThenSucceeds()
{
    var timeProvider = new FakeTimeProvider();
    var attempts = 0;

    ResiliencePipeline pipeline = new ResiliencePipelineBuilder { TimeProvider = timeProvider }
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        })
        .Build();

    var executeTask = pipeline.ExecuteAsync(async _ =>
    {
        attempts++;
        if (attempts < 3) throw new InvalidOperationException();
        await Task.CompletedTask;
    });

    // Advance the fake clock instead of waiting on real Task.Delay calls
    timeProvider.Advance(TimeSpan.FromSeconds(1));
    timeProvider.Advance(TimeSpan.FromSeconds(1));

    await executeTask;
    attempts.Should().Be(3);
}
```

This makes the test's wall-clock duration independent of the configured delays entirely — a retry
policy with a 30-second backoff tests in milliseconds, not 30+ seconds per test run.

## Testing a timeout strategy without waiting out the timeout

Pair a `FakeTimeProvider` with a delegate that awaits a `TaskCompletionSource` you control, so the
test can deterministically simulate "the operation is still running" without an actual long-running
task:

```csharp
[Fact]
public async Task Timeout_ExceedsLimit_ThrowsTimeoutRejectedException()
{
    var timeProvider = new FakeTimeProvider();
    ResiliencePipeline pipeline = new ResiliencePipelineBuilder { TimeProvider = timeProvider }
        .AddTimeout(TimeSpan.FromSeconds(5))
        .Build();

    var never = new TaskCompletionSource();

    var executeTask = pipeline.ExecuteAsync(async ct => await never.Task.WaitAsync(ct));

    timeProvider.Advance(TimeSpan.FromSeconds(5));

    var act = async () => await executeTask;
    await act.Should().ThrowAsync<TimeoutRejectedException>();
}
```

## Testing a circuit breaker's state transitions

Drive enough failing executions to cross `MinimumThroughput`/`FailureRatio`, assert the circuit is
open (further calls throw `BrokenCircuitException` without invoking the delegate at all — assert
this by counting delegate invocations), then advance the fake clock past `BreakDuration` and assert
a subsequent call is allowed through again (half-open trial):

```csharp
[Fact]
public async Task CircuitBreaker_OpensAfterThreshold_ThenHalfOpensAfterBreakDuration()
{
    var timeProvider = new FakeTimeProvider();
    var callCount = 0;

    ResiliencePipeline pipeline = new ResiliencePipelineBuilder { TimeProvider = timeProvider }
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
            FailureRatio = 0.5,
            MinimumThroughput = 2,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .Build();

    async ValueTask Fail(CancellationToken ct) { callCount++; throw new InvalidOperationException(); }

    await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(Fail).AsTask());
    await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(Fail).AsTask());

    // Circuit is now open: the delegate is not invoked again
    await Assert.ThrowsAsync<BrokenCircuitException>(() => pipeline.ExecuteAsync(Fail).AsTask());
    callCount.Should().Be(2); // third call never reached the delegate

    timeProvider.Advance(TimeSpan.FromSeconds(30));

    // Half-open: one trial call is allowed through again
    await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(Fail).AsTask());
    callCount.Should().Be(3);
}
```

## What still deserves a real integration test

Reserve tests against a real (or realistically faked, e.g. WireMock-style) HTTP endpoint for
verifying that `AddResilienceHandler` is actually wired onto the right named/typed client and that
the end-to-end request path behaves as configured — the fake-`TimeProvider` unit tests above verify
each strategy's own decision logic, not whether it's attached to the right `HttpClient` in DI.
Keep the fast, deterministic unit tests as the majority of the suite; reserve the slower
integration layer for a small number of wiring-focused tests.
