# C# Mediator Pattern

Guidance on the Mediator design pattern in C# as a hand-rolled pattern — decoupling a set of
colleague objects from referencing each other directly by routing their communication through a
single mediating object. The routing table (by situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern/topic, not per package version — Mediator is a design
pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `core-concept-and-two-intents.md` | the coupling problem the pattern solves; the classic GoF intent (mediating UI/component colleagues) vs. the modern CQRS-style dispatch intent |
| `hand-rolled-dispatcher.md` | a from-scratch `IMediator`/`Send` implementation without a `dynamic` cast |
| `generic-mediator.md` | a reusable, type-parameterized `IRequest<TResponse>`/`IRequestHandler<TRequest,TResponse>` pair |
| `handler-resolution-strategies.md` | dictionary-based lookup vs. DI-container resolution for finding the right handler |
| `when-to-hand-roll-vs-framework.md` | the decision list for writing your own dispatcher vs. adopting a full framework for it |
| `testing.md` | testing a handler in isolation, testing code that sends through a mediator, testing the dispatcher itself |
| `extending.md` | adding a new request/handler pair without touching existing dispatch code |

## Scope

A design pattern, not a package — there is no version or license to pin, and no NuGet package this
skill tracks. Every example is a from-scratch implementation meant to be copied and adapted
directly into a project, not an API reference for any specific library.

Out of scope: cross-process or distributed messaging, and general CQRS architecture beyond the
dispatch mechanism itself. See [SKILL.md](SKILL.md) for the full out-of-scope list.
