# C# Records

Helps you write, review, or port C# `record` types: positional and explicit-member syntax,
`with`-expressions, compiler-generated value equality and `ToString`, `record class` vs.
`record struct`, and record inheritance hierarchies.

## When to reach for it

- Choosing between a positional record and one with explicit members
- Deciding `record class` vs. `record struct` vs. a plain class or struct
- Diagnosing unexpected equality or `ToString` output on a record
- Building a variant of an immutable value with a `with`-expression
- Working through a record inheritance hierarchy and its `PrintMembers`/`EqualityContract` mechanics

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting a
`record` declaration, or deciding between a record and a plain class or struct.

## What it covers

| Topic | Reference |
| --- | --- |
| No `record` keyword: hand-written equality and copy methods | [references/pre-csharp9-manual-value-types.md](references/pre-csharp9-manual-value-types.md) |
| `record`/`record class` fundamentals, `with`-expressions, inheritance | [references/csharp9-record-fundamentals.md](references/csharp9-record-fundamentals.md) |
| `record struct`, `readonly record struct` | [references/csharp10-record-structs.md](references/csharp10-record-structs.md) |
| `required` members on explicit record properties | [references/csharp11-required-members-in-records.md](references/csharp11-required-members-in-records.md) |
| `closed record class` hierarchies and exhaustive switch | [references/csharp15-closed-record-hierarchies.md](references/csharp15-closed-record-hierarchies.md) |

## Example prompts

- "Should this DTO be a positional record or a record with explicit properties?"
- "Why are two records with equal property values not comparing equal here?"
- "Convert this hand-written immutable class with a manual `Equals` into a record."
