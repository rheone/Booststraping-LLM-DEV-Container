# Short-Circuit vs. Always-Continue Semantics

A chain's most important design decision isn't how it's built — it's what happens once a handler
decides to act. Two semantics cover the common cases, and mixing them without deciding explicitly is
a frequent source of bugs.

## Short-circuit: the first handler that acts stops the chain

The handler that successfully processes the request returns its result, and no later handler runs
at all. This fits scenarios where handling is mutually exclusive — an approval chain where only one
approval level should apply, a lookup chain where the first cache or source that has the value wins.

```csharp
public TResponse? Handle(TRequest request) =>
    TryHandle(request) ?? Next?.Handle(request);
```

Returning a non-null result from `TryHandle` stops the chain implicitly, because `Handle` never
calls `Next` once `TryHandle` produced a value.

## Always-continue: every handler runs regardless of what earlier ones did

Every handler gets a chance to act, and the chain aggregates their effects instead of stopping at
the first one. This fits scenarios where handlers address independent, additive concerns — a
validation chain where every validator should report its own errors, not just the first one found.

```csharp
public sealed class ValidationHandler
{
    private ValidationHandler? _next;

    public ValidationHandler SetNext(ValidationHandler next)
    {
        _next = next;
        return next;
    }

    public void Handle(Order order, List<string> errors)
    {
        Validate(order, errors);
        _next?.Handle(order, errors);
    }

    protected abstract void Validate(Order order, List<string> errors);
}
```

Every handler in this chain runs unconditionally; the shared `errors` list accumulates every
handler's findings rather than any one handler's result ending the traversal.

## An explicit stop signal within an always-continue chain

Some chains are always-continue by default but need an escape hatch — a handler that detects a
condition severe enough that running the rest of the chain would be wrong (a validator that finds
the input isn't even parseable, making every later validator's check meaningless). Model this as an
explicit return value the aggregator checks, rather than silently reusing short-circuit's "return
non-null" convention for a chain that's always-continue everywhere else:

```csharp
public enum ChainSignal { Continue, Stop }

public ChainSignal Handle(Order order, List<string> errors)
{
    var signal = Validate(order, errors);
    return signal == ChainSignal.Stop ? ChainSignal.Stop : _next?.Handle(order, errors) ?? ChainSignal.Continue;
}
```

## Choosing between them

| Question | Short-circuit | Always-continue |
| --- | --- | --- |
| Can more than one handler legitimately apply to the same request? | No — handling is mutually exclusive | Yes — handlers address independent concerns |
| Does the caller need the *first* applicable result, or *every* handler's contribution? | First result | Every contribution |
| Does running a later handler after an earlier one already acted make sense? | No — it would be redundant or wrong | Yes — each one adds its own findings |

Decide this per chain, document it at the chain's entry point (the base handler class or the chain's
factory), and keep every handler in that chain consistent with the choice — a chain that mixes
handlers written for opposite semantics produces behavior that depends on registration order in
ways that are hard to predict from reading any single handler.
