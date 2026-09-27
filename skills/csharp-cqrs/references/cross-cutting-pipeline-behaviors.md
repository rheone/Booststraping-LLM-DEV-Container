# Cross-Cutting Pipeline Behaviors

Validation, logging, transaction management, and authorization apply to most command and query
handlers alike — writing each concern inline inside every handler both duplicates it everywhere and
buries the handler's actual business logic under boilerplate that has nothing to do with the
specific command or query. Wrapping handler dispatch in a pipeline of small, reusable behaviors keeps
each handler down to just its own logic.

## The shape: a chain of behaviors around a handler

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}

public sealed class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest> validator)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }

        return await next();
    }
}

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
```

Each behavior receives the request and a `next` delegate representing "the rest of the pipeline" —
it can inspect or reject the request before calling `next()`, inspect or transform the response
after, or both, without knowing anything about the handler at the end of the chain or the other
behaviors ahead of or behind it. This is the decorator pattern applied to handler dispatch: every
behavior wraps the next layer, and the outermost caller can't tell how many layers exist underneath.

## What belongs in a pipeline behavior vs. inside the handler

- **Pipeline behavior**: concerns that apply uniformly across many/most commands or queries and
  don't need domain-specific state to decide anything — input validation against a schema,
  request/response logging, wrapping the call in a database transaction, checking a broad
  authorization rule ("is this user authenticated at all," "does this user hold role X").
- **Inside the handler**: concerns that need the specific loaded state a generic behavior has no
  access to — "can *this* order, in *its current status*, be canceled," "does *this* user own *this*
  resource." A pipeline behavior runs before the handler has loaded anything domain-specific, so it
  can only ever check what's already present on the request itself (or globally available context
  like the current user), never state the handler alone knows how to load.

## Ordering matters

```csharp
services.AddPipelineBehavior(typeof(LoggingBehavior<,>));
services.AddPipelineBehavior(typeof(ValidationBehavior<,>));
services.AddPipelineBehavior(typeof(TransactionBehavior<,>));
```

Behaviors registered in this order wrap the handler from the outside in: logging wraps everything
(so it logs even a validation failure), validation runs before a transaction opens (so an invalid
request never even starts one), and the transaction behavior sits closest to the handler (so it
covers exactly the handler's own work and nothing else). Getting this order backwards — opening a
transaction before validating, for instance — means paying for a transaction on requests that are
about to be rejected anyway.

## Applying this without a dedicated pipeline abstraction

The same effect is reachable without any dispatch-pipeline machinery at all — a handler can simply
call a validator directly at its own top, or a thin wrapper method can compose
`Log(Validate(Transact(handler)))` manually for the handlers that need it. A generic pipeline is
worth the added indirection once enough handlers share enough behaviors that repeating the manual
composition everywhere becomes the bigger cost — for a small number of handlers, explicit composition
inside each one is often clearer than a generic behavior-chain abstraction.
