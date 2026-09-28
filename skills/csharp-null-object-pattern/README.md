# Null Object Pattern

You give a "no behavior here" case a real implementation of an interface instead of a null
reference, so callers stop branching on null to find out whether anything is actually configured.
It covers the classic shape, a shared singleton versus a per-call instance, a reusable generic
base, and how the pattern sits next to nullable reference types.

## When to reach for it

- Every call site that uses an optional collaborator (a logger, a notifier, a cache) repeats the
  same `collaborator?.DoThing()` or `if (collaborator != null)` check.
- You're designing a fallback for an interface where "do nothing" is a legitimate, meaningful
  outcome, not a sign something went wrong.
- You're deciding whether a genuinely absent value belongs to a null object (absent *behavior*) or
  to nullable-reference-type handling (absent *data*).

## Using it

This skill is model-invoked: it fires automatically when your prompt matches its situation, such as
asking how to remove a scattered null check or design a no-op implementation. You can also invoke
it directly as `/csharp-null-object-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The problem and the pattern's basic shape | [references/core-concept-and-motivation.md](references/core-concept-and-motivation.md) |
| Shared singleton instance vs. a fresh instance per call | [references/singleton-vs-per-call-instances.md](references/singleton-vs-per-call-instances.md) |
| A reusable, type-parameterized null-object base | [references/generic-null-object-base.md](references/generic-null-object-base.md) |
| How the pattern relates to nullable reference types | [references/nullable-reference-types-interaction.md](references/nullable-reference-types-interaction.md) |
| Testing code that depends on a null-object implementation | [references/testing.md](references/testing.md) |
| Adding a new no-op implementation or interface member | [references/extending.md](references/extending.md) |

## Example prompts

- "This service does a null check on `_notifier` before every call. Can I get rid of that with a
  null object?"
- "Should my `NullNotifier` be a shared singleton or a new instance each time I need it?"
- "How do I write a generic null-object base I can reuse across a few different interfaces?"
