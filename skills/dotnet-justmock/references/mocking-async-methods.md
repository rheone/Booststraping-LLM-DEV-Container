# Mocking Async Methods

JustMock arranges `Task`/`Task<T>`-returning members with dedicated `ReturnsAsync`/`ThrowsAsync`
helpers rather than requiring you to hand-construct completed/faulted tasks with `.Returns(...)`.

## Arranging a `Task<T>`-returning method

```csharp
public interface IOrderRepository
{
    Task<Order?> FindAsync(Guid id);
}

var repository = Mock.Create<IOrderRepository>();

Mock.Arrange(() => repository.FindAsync(orderId)).ReturnsAsync(existingOrder);
```

`ReturnsAsync` wraps the given value in an already-completed `Task<T>` so `await` on the arranged
call returns synchronously in the test — there's no real asynchronous work happening, matching how
most mocking frameworks handle async arrangement.

## Arranging a `Task`-returning (non-generic) method

```csharp
public interface INotifier
{
    Task NotifyAsync(string message);
}

Mock.Arrange(() => notifier.NotifyAsync(Arg.IsAny<string>())).Returns(Task.CompletedTask);
```

A non-generic `Task`-returning member arranges via `.Returns(Task.CompletedTask)` (or
`.Returns(() => Task.CompletedTask)` for a lazily-evaluated version) since there's no return value
to wrap — `ReturnsAsync` is specifically for `Task<T>` members with a value to supply.

## Arranging a faulted task (simulating a failure)

```csharp
Mock.Arrange(() => repository.FindAsync(orderId)).ThrowsAsync(new TimeoutException("db timeout"));
```

`ThrowsAsync` returns a `Task`/`Task<T>` that is already faulted with the given exception — awaiting
the arranged call rethrows that exception at the `await` point, exactly like a real async method
that failed. This differs from `.Throws<TException>()` (the synchronous exception-arrangement API),
which would throw *before* a `Task` is even returned rather than producing a faulted task — using
the synchronous `.Throws` on an async member changes where in the calling code the exception
surfaces and can mask bugs in exception-handling code that specifically awaits before catching.

## Auto-mocked async members don't complete by themselves

An un-arranged member on a mock that returns `Task`/`Task<T>` does **not** automatically return a
completed task — unlike some other frameworks' auto-mocking behavior, an un-arranged async call on a
JustMock mock can leave the returned task in a state that never completes, hanging any code that
awaits it. Always explicitly arrange every async member the system under test actually calls,
including ones where the test "doesn't care about the return value" — arrange it to
`ReturnsAsync(default)` or `Task.CompletedTask` explicitly rather than leaving it un-arranged.

```csharp
// Explicit no-op arrangement — safe even when the test doesn't care about the return value.
Mock.Arrange(() => repository.FindAsync(Arg.IsAny<Guid>())).ReturnsAsync((Order?)null);
```

## `ConfigureAwait` in a mocked async chain

`ConfigureAwait(false)` inside the *production* code being tested (the code calling the mocked
dependency) behaves identically whether the awaited task came from a real implementation or a
JustMock-arranged mock — `ConfigureAwait` only affects which `SynchronizationContext`/`TaskScheduler`
the continuation resumes on, and JustMock's `ReturnsAsync`/`ThrowsAsync` produce ordinary
`Task`/`Task<T>` instances that behave like any other task with respect to `ConfigureAwait`. There is
no JustMock-specific configuration needed to make a mocked async call honor `ConfigureAwait`
correctly in the code under test — this is a non-issue in practice, but worth confirming explicitly
if a mocked-async test behaves unexpectedly around thread/context switching, since it rules out the
mock itself as the cause and points back at the test host's synchronization context (most unit test
runners have none, which is itself why `ConfigureAwait` differences rarely surface in unit tests
regardless of mocking framework).

## Common pitfall

Because `ReturnsAsync`/`ThrowsAsync` produce already-completed/already-faulted tasks, a test cannot
use a JustMock arrangement to simulate a slow, still-pending async dependency (e.g. to test a
timeout/cancellation path against a dependency that takes a long time to respond). For that
scenario, arrange the member to return a real, not-yet-completed `Task`/`Task<T>` built and
completed manually (e.g. via `TaskCompletionSource<T>`) rather than `ReturnsAsync`, and control its
completion timing explicitly from the test.
