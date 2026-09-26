# C# Fluent Interface

A fluent interface is a method-chaining API design — every call in the chain returns a type that
lets the next call attach directly to it, ending in a terminal operation that breaks the chain. This
skill helps you design, name, and test one, whatever's actually being chained: a mutable
configuration object, an immutable value, or a staged sequence that enforces call order.

## When to reach for it

- You're designing a chainable configuration, query, or assembly API and deciding what each method
  should return.
- You're not sure whether a chain should mutate one shared instance or hand back a new instance per
  call, and want to know which trade-offs come with each.
- You want a caller to be unable to call methods out of order — skipping a mandatory step should be
  a compile error, not a runtime check.
- You're adding chain methods to a type you don't own, via extension methods.
- A fluent chain in review reads ambiguously — an unclear boolean parameter, a chain stretched
  across an unpredictable number of repeated calls, or a call whose return value looks silently
  discarded.

## Using it

This skill is model-invoked: it activates automatically when you're designing, reviewing, or
naming a fluent/chained API. You can also invoke it directly by asking for it or typing
`/csharp-fluent-interface`.

## What it covers

| Topic | Reference |
| --- | --- |
| What makes a call chainable, and how a chain terminates | [references/core-concepts-and-terminal-operations.md](references/core-concepts-and-terminal-operations.md) |
| Mutable chains that return `this` | [references/mutable-self-returning-chains.md](references/mutable-self-returning-chains.md) |
| Immutable chains that return a new instance per call | [references/immutable-new-instance-chains.md](references/immutable-new-instance-chains.md) |
| Staged APIs that enforce call order at compile time | [references/staged-fluent-apis-and-call-order.md](references/staged-fluent-apis-and-call-order.md) |
| Adding fluent calls via extension methods | [references/extension-method-fluent-apis.md](references/extension-method-fluent-apis.md) |
| Naming each chain link and knowing when a chain has grown too long | [references/naming-and-chain-readability.md](references/naming-and-chain-readability.md) |
| Testing a fluent API's final state, mutation, or call-order guarantee | [references/testing-fluent-interfaces.md](references/testing-fluent-interfaces.md) |

## Example prompts

- "Should this configuration API mutate itself when chained, or return a new instance each time?"
- "I want callers to authenticate before they can call `Build()` — can I enforce that at compile
  time instead of throwing at runtime?"
- "This fluent chain has a `WithSorting(true)` call — how should I rename it so the call site reads
  clearly?"
