# C# Decorator Pattern

Decorator adds behavior to an object without modifying its class: a decorator implements the same
interface as the object it wraps, forwards calls to it, and adds its own behavior before, after, or
instead of forwarding. This skill covers the classic decorator shape, a generic base class that
forwards every member of a wide interface, chaining multiple decorators together, and testing the
result.

## When to reach for it

- You need to add cross-cutting behavior (logging, caching, retry, validation, metrics) around an
  existing implementation without changing its class.
- The interface you're decorating is wide, and hand-writing every forwarding member for each
  decorator would be repetitive.
- You're assembling several decorators together and need to reason about how their order changes
  observable behavior.

## Using it

This skill is model-invoked: it activates automatically when the conversation touches wrapping an
implementation to add behavior, a generic decorator base for a wide interface, or ordering a
decorator chain. You can also invoke it directly by asking for it or typing
`/csharp-decorator-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| Wrapping an interface to add behavior; decorator vs. inheritance | [references/classic-decorator.md](references/classic-decorator.md) |
| A generic base class forwarding every interface member | [references/generic-decorator-base.md](references/generic-decorator-base.md) |
| Building a chain, and why decorator order changes observable behavior | [references/chaining-decorators.md](references/chaining-decorators.md) |
| The shared intent and differences between hand-written chains and a DI container's own wrapping mechanism | [references/decorator-vs-di-pipelines.md](references/decorator-vs-di-pipelines.md) |
| Testing forwarding behavior and an assembled decorator chain | [references/testing-decorators.md](references/testing-decorators.md) |
| Adding a new decorator without touching existing ones | [references/extending-decorators.md](references/extending-decorators.md) |

## Example prompts

- "I need to add logging and caching around this service without touching its existing class."
- "This interface has fifteen members and I only want to override two of them in my decorator.
  Help me set up a forwarding base class."
- "Does the order I stack these decorators in actually matter here?"
