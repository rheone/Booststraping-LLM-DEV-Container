# C# Adapter Pattern

The Adapter pattern wraps a type whose interface doesn't match what your code expects, translating
calls between the two without modifying either side. It covers object adapters, class adapters,
a generic adapter abstraction, and the testing and extension concerns that come with them.

## When to reach for it

- A third-party client, a legacy class, or a generated proxy exposes a shape your application code
  isn't written against, and you don't control that type.
- You're deciding between wrapping an instance (composition) and inheriting from it, and want to
  know which fits C#'s single-inheritance constraint.
- You're reviewing or writing a class named `*Adapter` or `*Wrapper` and want to check it against
  the pattern's actual shape.

## Using it

This skill is model-invoked: it activates automatically when the conversation touches adapting an
incompatible interface, wrapping a third-party API, or a class named `*Adapter`/`*Wrapper`. You can
also invoke it directly by asking for it or typing `/csharp-adapter-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| Target interface, adaptee, and adapter roles; when the pattern is warranted | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| The composition-based adapter (the idiomatic C# form) | [references/object-adapter.md](references/object-adapter.md) |
| The inheritance-based adapter, and why it's uncommon in C# | [references/class-adapter.md](references/class-adapter.md) |
| A reusable generic adapter interface | [references/generic-adapter.md](references/generic-adapter.md) |
| Wrapping a third-party library's API shape behind your own abstraction | [references/adapting-third-party-apis.md](references/adapting-third-party-apis.md) |
| Adding a new adapter without touching existing ones | [references/extending-with-new-adapters.md](references/extending-with-new-adapters.md) |
| Testing code written against an adapter's target interface | [references/testing-adapters.md](references/testing-adapters.md) |

## Example prompts

- "This third-party SDK's client doesn't match the interface my service layer expects. Help me
  wrap it."
- "Should this be a class adapter or an object adapter?"
- "I need a generic adapter interface so I'm not writing a one-off class for every legacy type we
  wrap."
