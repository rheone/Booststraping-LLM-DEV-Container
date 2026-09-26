# C# Nullable Reference Types

Helps you write, review, or port code under C#'s nullable reference types (NRT) feature: the
`#nullable` directive, `?` annotations on reference types, the null-forgiving `!` operator, and the
compiler's flow analysis, as a purely compile-time, advisory static-analysis layer over ordinary
reference types.

## When to reach for it

- Deciding `?` vs. a plain reference type on a member, parameter, or generic type parameter
- Diagnosing or suppressing a nullable warning without masking a real bug
- Planning an incremental migration of an existing codebase onto NRT
- Reasoning about whether the compiler will narrow a value's nullability after a null check
- Writing NRT-aware test fixtures or assertions

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting
`#nullable`-aware code, or planning a migration to nullable reference types.

## What it covers

| Topic | Reference |
| --- | --- |
| No NRT: every reference type implicitly nullable | [references/pre-csharp8-nullable-oblivious.md](references/pre-csharp8-nullable-oblivious.md) |
| `#nullable`, `?`, `!`, flow-analysis narrowing (the baseline) | [references/csharp8-nullable-reference-types.md](references/csharp8-nullable-reference-types.md) |
| Unconstrained generic `T?`, `[MemberNotNull]`/`[MemberNotNullWhen]` | [references/csharp9-unconstrained-generic-nullability.md](references/csharp9-unconstrained-generic-nullability.md) |
| More accurate flow analysis, nullable-by-default templates | [references/csharp10-flow-analysis-and-defaults.md](references/csharp10-flow-analysis-and-defaults.md) |
| `required` members satisfying definite assignment | [references/csharp11-required-members.md](references/csharp11-required-members.md) |
| Nullable-governed exhaustive switch | [references/csharp15-nullable-exhaustive-switch.md](references/csharp15-nullable-exhaustive-switch.md) |

## Example prompts

- "Should this parameter be `string?` or `string` here?"
- "Why is the compiler still warning about a possible null after my null check?"
- "Help me plan an incremental rollout of nullable reference types across this project."
