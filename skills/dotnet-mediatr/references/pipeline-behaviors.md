# Pipeline Behaviors

`IPipelineBehavior<TRequest, TResponse>` lets you wrap **every** request that flows through
`Send` with cross-cutting logic — logging, validation, caching, transactions, authorization —
without each handler implementing that logic itself. Behaviors compose like middleware: each one
decides whether/when to call the next delegate in the chain.

## Shape of a behavior

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next(cancellationToken);
        logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
```

`next` is the rest of the pipeline — calling it invokes the next behavior in the chain, and
ultimately the handler itself. A behavior can:

- Do work before calling `next` (logging entry, starting a stopwatch, opening a transaction).
- Do work after calling `next` (logging the result, committing/rolling back a transaction).
- **Skip calling `next` entirely** (e.g. return a cached response, or throw a validation
  exception before the handler ever runs).
- Wrap `next` in a `try`/`catch` to translate exceptions.

## Ordering is registration order, not attribute or type order

Behaviors execute in the order they were registered with `AddOpenBehavior` (or
`services.AddTransient(typeof(IPipelineBehavior<,>), ...)`), first-registered runs outermost:

```csharp
cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));     // runs first (outermost)
cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));  // runs second
cfg.AddOpenBehavior(typeof(TransactionBehavior<,>)); // runs third (innermost, closest to the handler)
```

For the three built-in wraps above, this ordering is deliberate: logging wraps the whole
operation including validation failures; validation runs before a transaction is opened, so an
invalid request never begins a transaction; the transaction is the innermost wrap so it covers
only the actual handler's database work. Getting the order backwards (e.g. opening a transaction
before validation) is a real, easy-to-make mistake with no compiler warning to catch it — it only
surfaces as a bug report or a code review comment.

## Common uses

### Logging

Shown above — records entry/exit and timing for every request uniformly, instead of each handler
remembering to log.

### Validation (e.g. with FluentValidation)

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
```

This keeps handlers free of validation boilerplate — a handler can assume the request it receives
already passed validation, which only holds true if every request actually flows through this
behavior (see the pitfall below).

### Transaction wrapping

```csharp
public sealed class TransactionBehavior<TRequest, TResponse>(AppDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

Scoping this behavior to only commands (not queries) usually means applying a marker interface
(e.g. `ITransactionalRequest`) and constraining the behavior's `TRequest` generic to it, or
checking `request is ICommand` at runtime — a plain unconstrained open generic wraps every
request, including read-only queries that don't need a transaction.

## Pre/post processors: a narrower alternative

`IRequestPreProcessor<TRequest>` and `IRequestPostProcessor<TRequest, TResponse>` are a lighter
alternative to a full pipeline behavior when you only need "run this before" or "run this after"
semantics without needing to control whether `next` is called at all:

```csharp
public sealed class AuditPreProcessor<TRequest>(ICurrentUser currentUser) : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        // audit log entry...
        return Task.CompletedTask;
    }
}
```

Under the hood, pre/post processors run inside a built-in `IPipelineBehavior` MediatR wires up
for you when it finds any registered — they're sugar over the same mechanism, useful when a
behavior would be overkill for a simple "always run this, in order, can't short-circuit" need.

## Constraining behaviors to a subset of requests

An unconstrained `IPipelineBehavior<TRequest, TResponse>` open generic applies to literally every
request in the assembly. To scope a behavior narrower, either:

- Constrain the generic's `where TRequest : ISomeMarkerInterface` clause, so DI simply won't match
  requests that don't implement the marker.
- Check `request is ISomeMarkerInterface` at the top of `Handle` and `return await next(...)`
  immediately (a no-op passthrough) when the marker isn't present.

The marker-interface-plus-generic-constraint approach is generally preferred: it fails to compile
(or rather, fails to register/match) rather than silently no-op-ing, which makes "why didn't my
behavior run" easier to diagnose than a runtime `if` check buried in every behavior.
