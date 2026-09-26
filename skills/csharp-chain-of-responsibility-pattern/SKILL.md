---
name: csharp-chain-of-responsibility-pattern
description: Reference for the Chain of Responsibility design pattern in C# — a linked chain of handler objects, each deciding whether to process a request itself or pass it to the next handler in the chain. Covers the basic handler interface and linking a chain manually, a generic handler interface (IHandler<TRequest, TResponse>), building a chain via explicit composition or dependency-injection registration order, short-circuiting versus always-continue chain semantics, how this compares conceptually to a request-processing pipeline with an explicit next-delegate (described generically, as a design concept, not tied to any specific framework), extending the chain with a new handler without touching existing ones, and testing chain behavior. Use when writing a sequence of validators/approvers/filters where any one of them might handle a request and stop the rest from running, reviewing or building an approval/escalation workflow, or deciding how strict a chain's handler ordering and termination rules should be.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Chain of Responsibility Pattern

Chain of Responsibility links a series of handler objects together, and passes a request along the
chain until one of them handles it — or every handler has had a chance to look at it, depending on
the chain's semantics. Each handler only knows about the next one in line; none of them knows the
chain's full shape or length.

## Quick start

```csharp
public abstract class ApprovalHandler
{
    private ApprovalHandler? _next;

    public ApprovalHandler SetNext(ApprovalHandler next)
    {
        _next = next;
        return next;
    }

    public ApprovalResult? Handle(PurchaseRequest request) =>
        TryHandle(request) ?? _next?.Handle(request);

    protected abstract ApprovalResult? TryHandle(PurchaseRequest request);
}

public sealed class TeamLeadApproval : ApprovalHandler
{
    protected override ApprovalResult? TryHandle(PurchaseRequest request) =>
        request.Amount <= 1000m ? new ApprovalResult(Approved: true, "Team Lead") : null;
}

public sealed class DirectorApproval : ApprovalHandler
{
    protected override ApprovalResult? TryHandle(PurchaseRequest request) =>
        request.Amount <= 25000m ? new ApprovalResult(Approved: true, "Director") : null;
}

var chain = new TeamLeadApproval();
chain.SetNext(new DirectorApproval());

ApprovalResult? result = chain.Handle(new PurchaseRequest(Amount: 5000m));
```

`TeamLeadApproval` returns `null` from `TryHandle` for anything over its own limit, which sends the
request to `DirectorApproval` next. Neither handler knows whether a third handler exists after it.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Learning the pattern's roles and the basic linked-handler shape | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| Writing a reusable `IHandler<TRequest, TResponse>` abstraction | [references/generic-handler.md](references/generic-handler.md) |
| Deciding how to assemble the chain — explicit composition vs. registration order | [references/building-the-chain.md](references/building-the-chain.md) |
| Deciding whether a request stops at the first handler that acts on it, or flows through every handler | [references/short-circuit-vs-always-continue.md](references/short-circuit-vs-always-continue.md) |
| Comparing this pattern to an always-continue, explicit-next-delegate processing pipeline | [references/chain-vs-pipeline.md](references/chain-vs-pipeline.md) |
| Adding a new handler to the chain without touching existing handlers | [references/extending-with-new-handlers.md](references/extending-with-new-handlers.md) |
| Testing a chain's routing behavior, or testing one handler in isolation | [references/testing-handlers.md](references/testing-handlers.md) |

## Out of scope

- Any specific framework's request-processing pipeline implementation. The always-continue,
  explicit-next-delegate style of pipeline is described in
  [references/chain-vs-pipeline.md](references/chain-vs-pipeline.md) as a design concept, not as a
  named framework's feature.
- Any specific dependency-injection container's registration API. Ordering handlers "by
  registration order" is described generically — apply it with whatever container or manual
  composition a given project uses.
