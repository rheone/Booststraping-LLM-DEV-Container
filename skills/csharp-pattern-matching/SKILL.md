---
name: csharp-pattern-matching
description: Reference for C# pattern matching and switch expressions — `is`-expressions with type/declaration patterns, switch-statement and switch-expression pattern matching with `when` guards, constant/type/var/discard patterns, property and extended (dot-notation) property patterns, positional/tuple/deconstruction patterns, relational patterns (`> 0`, `<= 100`), logical combinators (`and`/`or`/`not`), list patterns (`[1, 2, .. var rest]`) and slice patterns, nested/recursive pattern composition, and pattern matching on generic types and type parameters — from the pre-C#7 `is`-plus-cast/constant-only-switch era (no pattern matching at all) through C# 7.0's original `is`/switch patterns, C# 7.1's generic-type-parameter matching, C# 8.0's switch expressions and recursive (property/positional/tuple) patterns, C# 9.0's relational/logical patterns, C# 10's extended property patterns, C# 11's list/slice patterns, and C# 15's closed-hierarchy switch exhaustiveness. Use when writing, reviewing, or porting `is`/`switch` pattern code, choosing switch expression vs. switch statement, diagnosing a non-exhaustive-switch warning (CS8509), combining patterns with `and`/`or`/`not`/`when`, matching on a generic type or type parameter, or writing pattern-based test assertions. Covers the pre-C#7 workaround pattern through the latest .NET 11 release candidate.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Pattern Matching

One throughline: matching a value's run-time shape (its type, its constant value, its properties,
its elements) against a declarative pattern instead of a chain of `if`/cast/index code. C# 7.0
introduced the whole idea (`is`-declaration patterns, pattern-capable `switch`); every later tier
adds either a new *pattern kind* (property, positional, relational, logical, list) or a new place
those kinds can be written (a switch *expression*, a generic type parameter, extended dot-notation,
a compiler-verified exhaustive class hierarchy). The C# 7.0 baseline in
[references/csharp7-is-and-switch-patterns.md](references/csharp7-is-and-switch-patterns.md) still
compiles unchanged on every later target.

## Quick start (works everywhere, C# 7.0+)

```csharp
public static decimal GetDiscount(object customer)
{
    switch (customer)
    {
        case null:
            throw new ArgumentNullException(nameof(customer));
        case PremiumCustomer p when p.YearsActive >= 5:
            return 0.20m;
        case PremiumCustomer _:
            return 0.10m;
        case RegularCustomer r when r.OrderCount > 50:
            return 0.05m;
        default:
            return 0m;
    }
}
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets, so pick the
highest tier you need and it points you downward as required.

| Target | C# language version | Reference file |
| --- | --- | --- |
| Any pre-2017 target | C# 1.0 – 6.0 | [references/pre-csharp7-manual-type-checks.md](references/pre-csharp7-manual-type-checks.md) — no pattern matching; `as`-plus-null-check and constant-only `switch` |
| VS 2017 / .NET Framework 4.6.1+, .NET Core 1.x+ | C# 7.0+ | [references/csharp7-is-and-switch-patterns.md](references/csharp7-is-and-switch-patterns.md) — declaration/constant/var patterns, pattern-capable `switch` statement, `when` guards; the universal baseline |
| .NET Core 2.0+ | C# 7.1+ | [references/csharp7.1-generic-type-parameter-patterns.md](references/csharp7.1-generic-type-parameter-patterns.md) — pattern matching on a generic type parameter |
| .NET Core 3.0+ | C# 8.0+ | [references/csharp8-switch-expressions-and-recursive-patterns.md](references/csharp8-switch-expressions-and-recursive-patterns.md) — switch **expressions**, property/positional/tuple patterns |
| .NET 5+ | C# 9.0+ | [references/csharp9-relational-and-logical-patterns.md](references/csharp9-relational-and-logical-patterns.md) — relational patterns, `and`/`or`/`not` combinators, bare type patterns |
| .NET 6+ | C# 10+ | [references/csharp10-extended-property-patterns.md](references/csharp10-extended-property-patterns.md) — dot-notation nested property patterns |
| .NET 7+ (and .NET Standard 2.1+/.NET Core 3.0+ for `System.Index`/`Range`) | C# 11+ | [references/csharp11-list-and-slice-patterns.md](references/csharp11-list-and-slice-patterns.md) — list patterns `[1, 2, 3]`, slice pattern `..` |
| .NET 11 (RC1 as of Sept 2026; GA expected Nov 2026) | C# 15 | [references/csharp15-closed-hierarchy-exhaustiveness.md](references/csharp15-closed-hierarchy-exhaustiveness.md) — `closed` classes make a switch over the hierarchy compiler-verified exhaustive |

**C# 12, 13, 14 note:** none of these releases added pattern-matching syntax — there's no
`references/csharp12-*.md` through `csharp14-*.md` file because nothing in this domain changed
between C# 11 and C# 15. If you're targeting C# 12–14, use the C# 11 reference file; everything in
it still applies unchanged.

## Specialized patterns

- [specialized/switch-expressions-vs-statements.md](specialized/switch-expressions-vs-statements.md) — when to use which, exhaustiveness, the discard arm, and CS8509
- [specialized/list-and-slice-patterns-in-depth.md](specialized/list-and-slice-patterns-in-depth.md) — nested element patterns, slicing with a shape constraint, jagged list patterns
- [specialized/pattern-combinators-nesting-and-when-clauses.md](specialized/pattern-combinators-nesting-and-when-clauses.md) — `when` vs. combinators, precedence pitfalls, deep nesting, matching on generic types and type parameters
- [specialized/testing-with-pattern-matching.md](specialized/testing-with-pattern-matching.md) — pattern matching as a test-authoring tool: one-expression shape assertions, list-pattern assertions on collection results, `is` replacing cast chains in test setup
