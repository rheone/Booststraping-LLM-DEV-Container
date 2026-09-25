# C# Pattern Matching

Reference for C# pattern matching and switch expressions — `is`-expressions, switch-statement and
switch-expression patterns, `when` guards, property/positional/relational/logical/list patterns,
and pattern matching on generic types — from the pre-C#7 era with no pattern matching at all
through C# 15's closed-hierarchy switch exhaustiveness. The routing table is in
[SKILL.md](SKILL.md).

```text
references/                                        version-gated core syntax, oldest to newest
  pre-csharp7-manual-type-checks.md                   before C# 7.0 — no pattern matching; `as`-plus-null-check and constant-only `switch`
  csharp7-is-and-switch-patterns.md                   C# 7.0 — declaration/constant/var patterns, pattern-capable switch statement, `when` guards
  csharp7.1-generic-type-parameter-patterns.md        C# 7.1 — pattern matching on a generic type parameter
  csharp8-switch-expressions-and-recursive-patterns.md  C# 8.0 — switch expressions, property/positional/tuple patterns
  csharp9-relational-and-logical-patterns.md          C# 9.0 — relational patterns, `and`/`or`/`not` combinators, bare type patterns
  csharp10-extended-property-patterns.md              C# 10 — dot-notation nested property patterns
  csharp11-list-and-slice-patterns.md                 C# 11 — list patterns, slice pattern `..`
  csharp15-closed-hierarchy-exhaustiveness.md         C# 15 — `closed` classes, compiler-verified switch exhaustiveness over a hierarchy

specialized/                                       cross-cutting patterns, applicable across versions
  switch-expressions-vs-statements.md                 when to use which, exhaustiveness, the discard arm, CS8509
  list-and-slice-patterns-in-depth.md                 nested element patterns, slicing with a shape constraint, jagged list patterns
  pattern-combinators-nesting-and-when-clauses.md     `when` vs. combinators, precedence pitfalls, deep nesting, generic-type matching
  testing-with-pattern-matching.md                    pattern matching as a test-authoring tool
```

## Version coverage

| .NET | C# | GA | Pattern-matching-relevant additions |
| --- | --- | --- | --- |
| .NET Framework / .NET Core 1.x and earlier | 1.0 – 6.0 | — | none — no pattern matching exists |
| VS 2017 / .NET Framework 4.6.1+, .NET Core 1.x+ | 7.0 | March 2017 | declaration/constant/var patterns, pattern-capable `switch` statement, `when` guards |
| .NET Core 2.0+ | 7.1 | August 2017 | pattern matching on a generic type parameter |
| .NET Core 3.0+ | 8.0 | September 2019 | switch expressions, property/positional/tuple patterns |
| .NET 5+ | 9.0 | November 2020 | relational patterns, `and`/`or`/`not` logical combinators, bare type patterns |
| .NET 6+ | 10 | November 2021 | extended (dot-notation) property patterns |
| .NET 7+ | 11 | November 2022 | list patterns, slice patterns (needs .NET Standard 2.1+/.NET Core 3.0+ for `System.Index`/`Range`) |
| .NET 8 / .NET 9 / .NET 10 | 12 / 13 / 14 | Nov 2023 / Nov 2024 / Nov 2025 | none — no pattern-matching syntax added in any of these three releases |
| .NET 11 (RC1 as of Sept 2026; GA expected Nov 2026) | 15 | RC | `closed` classes: compiler-verified exhaustive `switch` over a class hierarchy, no discard arm required |
