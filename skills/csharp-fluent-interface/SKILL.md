---
name: csharp-fluent-interface
description: Reference for designing and reviewing fluent interfaces in C# — method chains where each call returns a type that enables the next call, ending in a terminal operation. Covers the mutable self-returning chain (each call returns `this`), the immutable new-instance chain (each call returns a fresh instance, the shape records' `with`-expressions and LINQ's deferred operators both use), staged fluent APIs that return a different type per phase to enforce call order at compile time, adding fluent calls via extension methods without owning the chained type, naming and chain-readability conventions, and how to test a fluent API's final state without asserting on the chain's intermediate return values. Use when designing a chainable configuration, query, or assembly API; deciding whether a chain should mutate one shared instance or return a new instance per call; enforcing a required call order before a terminal operation; adding chain methods to a type you don't own; or reviewing an existing fluent surface for ambiguous naming or a chain that silently discards a call's result.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Fluent Interface

A fluent interface is a method-chaining API shape: each call in the chain returns a type — the same
instance, a new instance, or a different type entirely — that lets the next call attach directly to
it, with no intermediate variable needed. The chain ends in a **terminal operation**, a call whose
return type breaks the chain (a `Build()`, a `ToList()`, a `void`, or simply a type with no further
fluent members).

## Quick start

```csharp
StringBuilder message = new StringBuilder()
    .Append("Order ")
    .Append(orderId)
    .Append(" total: ")
    .Append(total.ToString("C"));

string result = message.ToString();
```

`Append` returns the same `StringBuilder` instance, so each call attaches directly to the previous
one's result; `ToString()` is the terminal operation — it returns `string`, which has no further
member to chain into. Every variant this skill covers is a different answer to one question: *what
does each intermediate call return, and why?*

## Pick your reference file

| Situation | Reference file |
| --- | --- |
| Understanding what makes an API "fluent" and how a chain terminates | [references/core-concepts-and-terminal-operations.md](references/core-concepts-and-terminal-operations.md) |
| Each call mutates and returns the same instance (`return this;`) | [references/mutable-self-returning-chains.md](references/mutable-self-returning-chains.md) |
| Each call returns a new instance, leaving the original untouched | [references/immutable-new-instance-chains.md](references/immutable-new-instance-chains.md) |
| Enforcing a required call order at compile time by returning a different type per phase | [references/staged-fluent-apis-and-call-order.md](references/staged-fluent-apis-and-call-order.md) |
| Adding fluent calls to a type you don't own, or splitting a large fluent surface across files | [references/extension-method-fluent-apis.md](references/extension-method-fluent-apis.md) |
| Naming each chain link and deciding when a chain has grown too long to stay fluent | [references/naming-and-chain-readability.md](references/naming-and-chain-readability.md) |
| Testing a fluent API's final state, a staged API's ordering, or an extension-method chain | [references/testing-fluent-interfaces.md](references/testing-fluent-interfaces.md) |
