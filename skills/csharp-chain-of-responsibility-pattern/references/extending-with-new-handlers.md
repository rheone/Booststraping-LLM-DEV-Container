# Extending With New Handlers

Adding a new handler to a chain should mean writing one new handler class and inserting it at the
right point during chain construction — never modifying an existing handler's logic, or the code
that submits requests to the chain.

## Adding a new handler

1. Confirm the handler interface (or base class) already expresses what the new handler needs: the
   same request/response types every other handler in the chain uses.
2. Write the new handler implementing `TryHandle` (or the chain's equivalent single-responsibility
   method) with only its own decision logic — it should never need to know what other handlers exist
   or what they've already tried.
3. Update wherever the chain is built (see
   [building-the-chain.md](building-the-chain.md)) to include the new handler at the correct
   position. Nothing else changes: the client submitting requests, and every other handler already
   in the chain, are untouched.

```csharp
// Existing.
public sealed class TeamLeadApproval : HandlerBase<PurchaseRequest, ApprovalResult> { /* ... */ }
public sealed class DirectorApproval : HandlerBase<PurchaseRequest, ApprovalResult> { /* ... */ }

// New handler, inserted between the two existing ones — neither existing handler changes.
public sealed class DepartmentHeadApproval : HandlerBase<PurchaseRequest, ApprovalResult>
{
    protected override ApprovalResult? TryHandle(PurchaseRequest request) =>
        request.Amount <= 10000m ? new ApprovalResult(Approved: true, "Department Head") : null;
}

teamLead.Next = departmentHead;
departmentHead.Next = director;
```

## Where a new handler belongs in the order

For a short-circuit chain, a new handler's position determines which requests it actually gets a
chance to see — a handler inserted after another one that already handles a broad range of requests
may never run for those cases. Decide the new handler's position by what range of requests it's
meant to catch, not by convenience of where it's easiest to insert in the construction code.

For an always-continue chain, position usually matters less to correctness (every handler runs
regardless), but still matters wherever earlier handlers' results are visible to later ones — order
by that dependency, not arbitrarily.

## When the request or response shape itself needs to grow

If the new handler needs data that the existing request type doesn't carry, or needs to communicate
something in the response type that existing handlers didn't need to express, that's a change to
the request/response shape itself — every handler in the chain sees the same request and response
types, so widening either one is visible to all of them, even handlers that don't care about the new
data. Add new data as an optional/nullable field, or as an additive property on the response, so
existing handlers keep compiling and behaving exactly as before; avoid narrowing or repurposing an
existing field to carry the new handler's data, since that changes what every other handler already
depends on that field meaning.

## Testing the extension in isolation

A new handler's own tests exercise `TryHandle` directly, without needing the rest of the chain built
at all. A test that the new handler is correctly *positioned* in the chain — that requests it should
catch actually reach it, and requests it should decline actually pass through to the next handler —
belongs at the chain level; see [testing-handlers.md](testing-handlers.md) for both kinds of test.
