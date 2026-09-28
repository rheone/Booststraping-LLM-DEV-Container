# Testing Handlers

Chain of Responsibility splits into two testable levels: each handler's own decision logic in
isolation, and the chain's overall routing behavior once handlers are linked together. Both need
coverage — one bug class lives entirely in an individual handler's condition, another lives entirely
in how handlers are ordered and linked.

## Testing one handler in isolation

Call `TryHandle` (or whatever the chain's per-handler method is named) directly, with no `Next` set
at all. This tests the handler's own decision logic without involving the rest of the chain.

```csharp
[Fact]
public void TeamLeadApproval_approves_requests_at_or_under_its_limit()
{
    var handler = new TeamLeadApproval();

    ApprovalResult? result = handler.Handle(new PurchaseRequest(Amount: 1000m));

    Assert.NotNull(result);
    Assert.True(result!.Approved);
}

[Fact]
public void TeamLeadApproval_declines_requests_over_its_limit()
{
    var handler = new TeamLeadApproval();

    ApprovalResult? result = handler.Handle(new PurchaseRequest(Amount: 1001m));

    Assert.Null(result);
}
```

Calling `Handle` with no `Next` configured and asserting `null` comes back for a request the
handler declines confirms the handler doesn't accidentally throw or produce a spurious result when
it has nothing further to forward to.

## Testing the chain's routing behavior

Build a small chain — using real handler instances or fakes standing in for each position — and
assert on which handler's result the caller actually receives for a given request.

```csharp
public sealed class FakeHandler : HandlerBase<PurchaseRequest, ApprovalResult>
{
    private readonly Func<PurchaseRequest, ApprovalResult?> _rule;
    public FakeHandler(Func<PurchaseRequest, ApprovalResult?> rule) => _rule = rule;
    protected override ApprovalResult? TryHandle(PurchaseRequest request) => _rule(request);
}

[Fact]
public void Chain_forwards_to_the_next_handler_when_the_first_declines()
{
    var first = new FakeHandler(_ => null);
    var second = new FakeHandler(_ => new ApprovalResult(Approved: true, "Second"));
    first.Next = second;

    ApprovalResult? result = first.Handle(new PurchaseRequest(Amount: 5000m));

    Assert.Equal("Second", result!.ApprovedBy);
}

[Fact]
public void Chain_stops_at_the_first_handler_that_accepts()
{
    var callCountOnSecond = 0;
    var first = new FakeHandler(_ => new ApprovalResult(Approved: true, "First"));
    var second = new FakeHandler(_ => { callCountOnSecond++; return null; });
    first.Next = second;

    first.Handle(new PurchaseRequest(Amount: 100m));

    Assert.Equal(0, callCountOnSecond);
}
```

The second test is specific to short-circuit semantics — it asserts a *negative* (the second handler
never ran). For an always-continue chain, write the mirror-image test instead: assert every handler
in the chain *did* run, and that their contributions were all aggregated into the final result. See
[short-circuit-vs-always-continue.md](short-circuit-vs-always-continue.md) for which assertion fits
a given chain.

## Testing chain construction

If the chain is assembled by a dedicated factory or from a resolved, ordered collection (see
[building-the-chain.md](building-the-chain.md)), give that construction logic its own test asserting
the resulting chain's handlers are linked in the expected order — walk the `Next` references from
the head and assert on each handler's type or identity in sequence. This catches an ordering mistake
at the point it's introduced, rather than only surfacing as a wrong-approver bug against a specific
input later.

## What doesn't need a test here

Don't duplicate a handler's own `TryHandle` test at the chain level for every possible input — the
chain-level tests exist to verify *routing* (forwarding, stopping, aggregating), not to re-verify
each handler's internal condition, which its own isolated test already covers.
