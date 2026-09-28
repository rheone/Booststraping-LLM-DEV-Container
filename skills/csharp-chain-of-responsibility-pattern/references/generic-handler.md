# Generic Handler

When a codebase has several chains, each handling a different request/response shape, a generic
`IHandler<TRequest, TResponse>` interface gives every chain the same shape without a bespoke
interface per chain.

## The interface

```csharp
public interface IHandler<TRequest, TResponse>
{
    IHandler<TRequest, TResponse>? Next { get; set; }

    TResponse? Handle(TRequest request);
}
```

## A reusable base that wires forwarding automatically

As with the non-generic form, a base class that implements `Handle` in terms of an abstract
`TryHandle` keeps concrete handlers focused purely on their own decision logic:

```csharp
public abstract class HandlerBase<TRequest, TResponse> : IHandler<TRequest, TResponse>
    where TResponse : class
{
    public IHandler<TRequest, TResponse>? Next { get; set; }

    public TResponse? Handle(TRequest request) =>
        TryHandle(request) ?? Next?.Handle(request);

    protected abstract TResponse? TryHandle(TRequest request);
}
```

## Implementing a chain

```csharp
public sealed class TeamLeadApproval : HandlerBase<PurchaseRequest, ApprovalResult>
{
    protected override ApprovalResult? TryHandle(PurchaseRequest request) =>
        request.Amount <= 1000m ? new ApprovalResult(Approved: true, "Team Lead") : null;
}

public sealed class DirectorApproval : HandlerBase<PurchaseRequest, ApprovalResult>
{
    protected override ApprovalResult? TryHandle(PurchaseRequest request) =>
        request.Amount <= 25000m ? new ApprovalResult(Approved: true, "Director") : null;
}

IHandler<PurchaseRequest, ApprovalResult> chain = new TeamLeadApproval
{
    Next = new DirectorApproval(),
};

ApprovalResult? result = chain.Handle(new PurchaseRequest(Amount: 5000m));
```

## Reusing the same generic interface for an always-continue chain

`IHandler<TRequest, TResponse>` also fits a chain where every handler runs regardless of what prior
handlers did — have `Handle` accumulate onto a mutable response, or aggregate a sequence of
responses, instead of short-circuiting on the first non-null result. See
[short-circuit-vs-always-continue.md](short-circuit-vs-always-continue.md) for both variants of the
`Handle` implementation and when each fits.

## Why the generic form earns its keep

Reach for `IHandler<TRequest, TResponse>` once a codebase has, or is expected to grow, more than one
chain built on the same request/response-per-handler shape — a validation chain, an approval chain,
a filter chain — so they share one interface, one base class, and one chain-building helper instead
of each reinventing forwarding logic against its own request/response types. For a single, one-off
chain that will never need a second instance of the pattern elsewhere, a concrete non-generic
handler interface named for that specific request is simpler to read at the call site and avoids
generic type arguments with no second use case to justify them.
