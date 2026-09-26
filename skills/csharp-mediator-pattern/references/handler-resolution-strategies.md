# Handler Resolution Strategies

The mediator's `Send` method needs some way to find the one handler registered for a given request
type. Two strategies cover almost every hand-rolled dispatcher: a plain dictionary the mediator
owns itself, or resolution through a dependency-injection container.

## Dictionary-based lookup

The mediator holds its own map from request type to a handler factory, populated explicitly at
startup:

```csharp
public sealed class Mediator : IMediator
{
    private readonly Dictionary<Type, Func<object, object>> _handlers = new();

    public void Register<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        _handlers[typeof(TRequest)] = request => handler.Handle((TRequest)request)!;
    }

    public TResponse Send<TResponse>(IRequest<TResponse> request)
    {
        if (!_handlers.TryGetValue(request.GetType(), out var invoke))
        {
            throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");
        }
        return (TResponse)invoke(request);
    }
}
```

This strategy needs no DI container at all — it's a plain object a small console app or library can
construct and populate directly. Its cost is that every handler instance registered this way lives
for as long as the mediator does; a handler with a shorter natural lifetime (a per-request database
context, for example) doesn't fit this shape without extra plumbing to construct it fresh on each
`Send` rather than once at registration time.

## DI-container resolution

The mediator asks the container for the handler on every `Send` call instead of holding instances
itself — shown in full in [hand-rolled-dispatcher.md](hand-rolled-dispatcher.md):

```csharp
public TResponse Send<TResponse>(IRequest<TResponse> request)
{
    Type requestType = request.GetType();
    // resolve IRequestHandler<requestType, TResponse> from the container, invoke it
}
```

This strategy fits a codebase that already has a DI container wired up for everything else, and it
gets handler lifetime management for free — a handler registered as scoped gets constructed fresh
per scope (typically per web request) the same way any other scoped service would, with no extra
code in the mediator to manage that.

## Choosing between them

Reach for the dictionary strategy in a project with no DI container already in place, or where the
handler set is small, fixed, and known entirely at startup. Reach for DI-container resolution
whenever a container is already wired up — piggybacking handler resolution onto infrastructure
that's already there avoids building a second, parallel registration mechanism next to the one the
rest of the project already uses.

## A resolution failure is a startup-time bug, not a runtime one

Whichever strategy is used, a request with no registered handler should fail loudly and
immediately — either at startup (a container that validates its registrations eagerly) or on first
`Send` with a clear exception naming the missing request type, as both examples above do. Silently
returning a default value for an unregistered request hides a wiring mistake behind wrong runtime
behavior instead of a clear failure at the point the mistake was made.
