# C# Chain of Responsibility Pattern

Reference for the Chain of Responsibility design pattern in C#: a linked chain of handler objects,
each deciding whether to process a request itself or pass it along. The routing table (by
situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern, not per package or version — Chain of Responsibility is
a behavioral pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `philosophy-and-structure.md` | handler roles, the basic linked-handler shape, when to reach for the pattern |
| `generic-handler.md` | a reusable `IHandler<TRequest, TResponse>` interface |
| `building-the-chain.md` | assembling the chain via explicit composition or registration order |
| `short-circuit-vs-always-continue.md` | stopping at the first handler that acts vs. running every handler |
| `chain-vs-pipeline.md` | comparison with an always-continue, explicit-next-delegate processing pipeline, as a design concept |
| `extending-with-new-handlers.md` | adding a new handler without touching existing handlers or the chain's consumer |
| `testing-handlers.md` | testing chain routing behavior; testing one handler in isolation |

## Scope

A behavioral design pattern, not a package — there is no version or license to track. Guidance
applies to any C# codebase; the pattern has been expressible since C# 1.0, and the generic handler
form uses generics (C# 2.0 onward).

Out of scope: any specific framework's request-processing pipeline implementation, and any specific
dependency-injection container's registration API. See [SKILL.md](SKILL.md) for the full
out-of-scope list.
