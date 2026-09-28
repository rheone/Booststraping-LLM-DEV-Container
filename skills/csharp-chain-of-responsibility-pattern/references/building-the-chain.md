# Building the Chain

Assembling a chain means deciding, in one place, which handlers exist and in what order they run.
Keep that decision out of the handlers themselves — a handler that reaches into a container or a
static registry to find "the next one" couples it to how the chain happens to be built, instead of
letting it stay a plain object that just knows about whatever `Next` it was given.

## Explicit composition

The most direct approach: a single method constructs each handler and links them in order.

```csharp
public static class ApprovalChainFactory
{
    public static IHandler<PurchaseRequest, ApprovalResult> Build()
    {
        var teamLead = new TeamLeadApproval();
        var director = new DirectorApproval();
        var executive = new ExecutiveApproval();

        teamLead.Next = director;
        director.Next = executive;

        return teamLead;
    }
}
```

This keeps the chain's shape readable in one place, with the order stated explicitly rather than
inferred from anything else. It's the right default when the chain's membership and order are fixed
by the application's own logic, not by configuration or by what happens to be registered elsewhere.

## Building from an ordered collection

When handlers come from a source that already produces them in a meaningful order — a configured
list, a discovered set of implementations — link them in a loop instead of naming each one:

```csharp
public static IHandler<TRequest, TResponse>? BuildChain<TRequest, TResponse>(
    IReadOnlyList<IHandler<TRequest, TResponse>> handlersInOrder)
{
    for (int i = 0; i < handlersInOrder.Count - 1; i++)
    {
        handlersInOrder[i].Next = handlersInOrder[i + 1];
    }

    return handlersInOrder.Count > 0 ? handlersInOrder[0] : null;
}
```

## Ordering via dependency-injection registration order

When handler instances come from a dependency-injection container resolving all registrations of a
handler interface, the container typically hands back an ordered collection reflecting registration
order — the handler registered first comes first in the resolved sequence. Feed that resolved,
ordered collection straight into the same `BuildChain` helper used for any other ordered source:

```csharp
IReadOnlyList<IHandler<PurchaseRequest, ApprovalResult>> resolvedInOrder =
    /* container resolves all registered IHandler<PurchaseRequest, ApprovalResult> instances */;

IHandler<PurchaseRequest, ApprovalResult>? chain = BuildChain(resolvedInOrder);
```

This approach ties the chain's order to registration order, which is easy to get right when there
are few handlers and easy to get subtly wrong once there are many — a new registration inserted in
the wrong place silently changes chain order with no compiler or test failure to catch it unless the
order is covered by a test. Prefer explicit composition once a chain's correctness genuinely depends
on handler order, and reserve registration-order chaining for cases where handler order doesn't
actually matter to the outcome (each handler addresses a disjoint concern) or where the number of
handlers is small enough to review at a glance.

## Keeping the chain's construction separate from its use

Whichever approach builds the chain, expose only the resulting head handler (or an interface over
it) to the code that submits requests — that code should have no visibility into how many handlers
exist or how they were assembled, only that it can call `Handle` on the chain's start.
