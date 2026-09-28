# C# Chain of Responsibility Pattern

Chain of Responsibility links a series of handler objects together and passes a request along the
chain until one of them handles it, or every handler has had a chance to look at it. This skill
covers building and extending a handler chain, choosing between short-circuit and always-continue
semantics, and testing the chain's routing behavior.

## When to reach for it

- You're writing a sequence of validators, approvers, or filters where any one of them might handle
  a request and stop the rest from running.
- You're reviewing or building an approval or escalation workflow where a request moves up a chain
  until someone acts on it.
- You need to decide how strict a chain's handler ordering and termination rules should be, or
  whether it should always run every handler regardless of outcome.

## Using it

This skill is model-invoked: it activates automatically when the conversation touches a linked
chain of handlers, an approval/escalation flow, or deciding between short-circuiting and
always-continue dispatch. You can also invoke it directly by asking for it or typing
`/csharp-chain-of-responsibility-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| Handler roles, the basic linked-handler shape, and when the pattern fits | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| A reusable generic handler interface | [references/generic-handler.md](references/generic-handler.md) |
| Assembling the chain through explicit composition or registration order | [references/building-the-chain.md](references/building-the-chain.md) |
| Stopping at the first handler that acts versus running every handler | [references/short-circuit-vs-always-continue.md](references/short-circuit-vs-always-continue.md) |
| How the chain's semantics relate to an always-continue, explicit-next-delegate pipeline | [references/chain-vs-pipeline.md](references/chain-vs-pipeline.md) |
| Adding a new handler without touching existing ones | [references/extending-with-new-handlers.md](references/extending-with-new-handlers.md) |
| Testing chain routing behavior and testing one handler in isolation | [references/testing-handlers.md](references/testing-handlers.md) |

## Example prompts

- "I have three validators that each might reject a request. Help me chain them so the first
  rejection stops the rest."
- "Should this approval workflow short-circuit at the first approver, or run every approver and
  collect all their responses?"
- "I need to add a new handler to this chain without touching the existing ones or their
  registration order."
