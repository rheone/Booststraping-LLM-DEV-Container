# C# Pattern Matching

Helps you write, review, or port C# code that matches a value's run-time shape (its type,
constant value, properties, or elements) against a declarative pattern instead of a chain of
`if`/cast/index checks, in both `is`-expressions and `switch`.

## When to reach for it

- Choosing a switch expression over a switch statement (or vice versa)
- Diagnosing a non-exhaustive-switch warning (`CS8509`)
- Combining patterns with `and`/`or`/`not`/`when` without tripping over precedence
- Matching on a list's shape and elements with list and slice patterns
- Matching on a generic type or type parameter

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting
`is`/`switch` pattern code, or combining pattern kinds.

## What it covers

| Topic | Reference |
| --- | --- |
| No pattern matching: `as`-plus-null-check, constant-only switch | [references/pre-csharp7-manual-type-checks.md](references/pre-csharp7-manual-type-checks.md) |
| Declaration/constant/var patterns, pattern-capable switch, `when` guards | [references/csharp7-is-and-switch-patterns.md](references/csharp7-is-and-switch-patterns.md) |
| Pattern matching on a generic type parameter | [references/csharp7.1-generic-type-parameter-patterns.md](references/csharp7.1-generic-type-parameter-patterns.md) |
| Switch expressions, property/positional/tuple patterns | [references/csharp8-switch-expressions-and-recursive-patterns.md](references/csharp8-switch-expressions-and-recursive-patterns.md) |
| Relational patterns, `and`/`or`/`not` combinators | [references/csharp9-relational-and-logical-patterns.md](references/csharp9-relational-and-logical-patterns.md) |
| Extended (dot-notation) property patterns | [references/csharp10-extended-property-patterns.md](references/csharp10-extended-property-patterns.md) |
| List patterns and the slice pattern (`..`) | [references/csharp11-list-and-slice-patterns.md](references/csharp11-list-and-slice-patterns.md) |
| `closed` classes and compiler-verified switch exhaustiveness | [references/csharp15-closed-hierarchy-exhaustiveness.md](references/csharp15-closed-hierarchy-exhaustiveness.md) |

## Example prompts

- "Rewrite this if/else type-checking chain as a switch expression."
- "Why does the compiler say this switch expression isn't exhaustive?"
- "Match a list where the first two elements are known and the rest is a `Span<int>` slice."
