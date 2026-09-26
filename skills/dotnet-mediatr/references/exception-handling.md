# Exception Handling in the Pipeline

MediatR provides two purpose-built interfaces for centralizing exception handling for requests,
as an alternative to (or complement of) a `try`/`catch` inside every individual handler or inside
a general-purpose pipeline behavior. Both remain current on 14.2.0.

## `IRequestExceptionAction<TRequest, TException>` — observe, don't suppress

Runs when a handler throws `TException` while processing `TRequest`. It can perform a side effect
(logging, alerting, incrementing a metric) but **cannot suppress the exception** — after every
matching action runs, the original exception is still rethrown.

```csharp
public sealed class OrderExceptionLogger<TRequest>(ILogger<OrderExceptionLogger<TRequest>> logger)
    : IRequestExceptionAction<TRequest, DbUpdateException>
    where TRequest : notnull
{
    public Task Execute(TRequest request, DbUpdateException exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Persistence failure handling {RequestType}", typeof(TRequest).Name);
        return Task.CompletedTask;
    }
}
```

## `IRequestExceptionHandler<TRequest, TResponse, TException>` — can suppress and substitute a response

Runs when a handler throws `TException`, and **can** short-circuit the exception by setting a
response on the provided state object — in which case the caller of `Send` receives that response
instead of the exception propagating.

```csharp
public sealed class NotFoundToDefaultHandler<TRequest, TResponse>
    : IRequestExceptionHandler<TRequest, TResponse, KeyNotFoundException>
    where TRequest : notnull
{
    public Task Handle(
        TRequest request,
        KeyNotFoundException exception,
        RequestExceptionHandlerState<TResponse> state,
        CancellationToken cancellationToken)
    {
        state.SetHandled(default!); // caller gets default(TResponse) instead of the exception
        return Task.CompletedTask;
    }
}
```

If `state.SetHandled(...)` is never called, the exception still propagates after this handler
runs — the handler has to opt in to suppression explicitly rather than suppression being implicit
just by existing.

## How they're wired in

Both interfaces are picked up by MediatR's assembly scan (they're closed or partially-closed
generics against a specific `TException`, similar to a regular handler) and are executed via a
built-in `RequestExceptionActionProcessorBehavior`/`RequestExceptionProcessorBehavior` that
MediatR automatically inserts into the pipeline when it detects any are registered — you do not
need to (and should not) also write a custom `IPipelineBehavior` to invoke them.

## Ordering relative to other pipeline behaviors and post-processors

A documented point of confusion: `IRequestPostProcessor` does **not** run after an
`IRequestExceptionHandler` has suppressed an exception and substituted a response — post-processors
are wired to run after a *successful* handler completion, and a suppressed-exception path does not
count as that for post-processor purposes on all versions. If a post-processor is depended on for
some invariant ("this always runs after a request completes, exception or not"), verify that
behavior for your specific version rather than assuming exception-handler-substituted responses
trigger it — a plain `IPipelineBehavior` wrapping `next()` in a `try`/`finally` is the more
reliable way to guarantee "runs no matter what happened."

## When to use exception handlers vs. a pipeline behavior's try/catch

- **`IRequestExceptionAction`/`IRequestExceptionHandler`**: when the concern is specific to one
  (or a few) exception **type**, independent of which request threw it — e.g. "translate every
  `DbUpdateException` anywhere in the app into a `ConflictException`." The per-exception-type
  targeting is the whole value proposition here.
- **A `try`/`catch` inside an `IPipelineBehavior`**: when the concern is about the pipeline
  position/flow itself regardless of exception type — e.g. "always roll back the transaction, no
  matter what was thrown" (see the transaction example in
  [pipeline-behaviors.md](pipeline-behaviors.md), which uses a bare `catch`/`finally` rather than
  a typed exception handler for exactly this reason).

These two mechanisms are not mutually exclusive — a typical pipeline runs both: a transaction
behavior guaranteeing rollback via `try`/`finally`, and a set of typed exception handlers
translating specific exception types into specific API-facing outcomes.
