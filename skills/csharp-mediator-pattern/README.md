# C# Mediator Pattern

Mediator decouples a set of colleague objects from referencing each other directly by routing their
communication through a single mediating object. This skill documents the pattern hand-rolled from
scratch: an `IMediator`-shaped interface, an in-process dispatcher that resolves the right handler
for a request, the two distinct intents the pattern gets used for, and when writing this yourself
beats depending on a framework for it. Every example here is code you write directly into a
project, not an API reference for a specific library.

## When to reach for it

- A set of objects would otherwise need direct references to each other to communicate, and you
  want to decouple them behind a single dispatch point.
- You're implementing request/handler dispatch (a `Send` or `Publish`-style call that resolves to
  the right handler by request type) without taking on a framework dependency for it.
- You're reviewing or writing a dispatcher and want to check its handler-resolution strategy against
  the alternatives.
- You're deciding whether a project's dispatch needs justify a dependency, or a dozen lines of
  hand-rolled code cover it.

## Using it

This skill is model-invoked: it activates automatically when the conversation touches decoupled
request/handler dispatch, a `Send`/`Publish`-style mediator, or hand-rolling a small dispatcher. You
can also invoke it directly by asking for it or typing `/csharp-mediator-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The coupling problem the pattern solves; the classic mediating-colleagues intent versus modern CQRS-style dispatch | [references/core-concept-and-two-intents.md](references/core-concept-and-two-intents.md) |
| A from-scratch `IMediator`/`Send` implementation | [references/hand-rolled-dispatcher.md](references/hand-rolled-dispatcher.md) |
| A reusable, type-parameterized request/handler pair | [references/generic-mediator.md](references/generic-mediator.md) |
| Dictionary-based lookup versus DI-container resolution for finding the right handler | [references/handler-resolution-strategies.md](references/handler-resolution-strategies.md) |
| The decision list for hand-rolling a dispatcher versus adopting a framework | [references/when-to-hand-roll-vs-framework.md](references/when-to-hand-roll-vs-framework.md) |
| Testing a handler in isolation, code that sends through a mediator, and the dispatcher itself | [references/testing.md](references/testing.md) |
| Adding a new request/handler pair without touching existing dispatch code | [references/extending.md](references/extending.md) |

## Example prompts

- "I want to decouple these components so they don't all reference each other directly. Help me
  route their communication through a mediator."
- "Help me write a small `Send`-style dispatcher from scratch, without pulling in a framework for
  it."
- "How do I add a new request/handler pair to this dispatcher without touching the existing ones?"
