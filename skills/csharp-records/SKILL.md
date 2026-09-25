---
name: csharp-records
description: Reference for C# records and immutability — the `record` keyword, positional records vs. records with explicit members, `with`-expressions for non-destructive mutation, compiler-generated value equality (`Equals`/`GetHashCode`/`==`/`!=`) and `ToString`, `record class` vs. `record struct` vs. plain `class`/`struct`, record inheritance and hierarchies (`PrintMembers`, `EqualityContract`), init-only setters as they interact with records, `required` members on records, and generic record types — from the pre-C#9 hand-written-equality-class era through C# 9.0's `record` keyword, C# 10's `record struct`, C# 11's `required` members interacting with positional records, and C# 15's `closed` record hierarchies. Use when writing, reviewing, or porting a `record` declaration, choosing between a positional record and one with explicit members, deciding `record class` vs. `record struct` vs. a plain class, diagnosing unexpected equality or `ToString` results on a record, using `with`-expressions to build variants of an immutable value, working with record inheritance hierarchies, or writing record-based test fixtures. Covers the pre-C#9 workaround pattern through the current .NET 11 release candidate.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Records

One throughline: a `record` is a compiler-synthesized alternative to hand-writing value equality,
`ToString`, and a "copy with changes" method on a data-carrying type. C# 9.0 introduced the whole
idea — positional or explicit-member syntax, init-only properties, `with`-expressions, and record
inheritance — as a reference type (`record` and `record class` are identical). Every later tier
either extends records to a new type shape (`record struct` as a value type) or clarifies how an
unrelated general-purpose feature (`required` members, `closed` hierarchies) behaves specifically
when it meets a record. The C# 9.0 baseline in
[references/csharp9-record-fundamentals.md](references/csharp9-record-fundamentals.md) still compiles
unchanged on every later target.

## Quick start (works everywhere, C# 9.0+)

```csharp
public record Person(string FirstName, string LastName);

Person original = new("Nancy", "Davolio");
Person renamed = original with { LastName = "Fuller" };

Console.WriteLine(original == renamed); // False -- value equality
Console.WriteLine(renamed);             // Person { FirstName = Nancy, LastName = Fuller }
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets, so pick the
highest tier you need and it points you downward as required.

| Target | C# language version | Reference file |
| --- | --- | --- |
| Any pre-2020 target | C# 1.0 – 8.0 | [references/pre-csharp9-manual-value-types.md](references/pre-csharp9-manual-value-types.md) — no `record` keyword; hand-written `Equals`/`GetHashCode`/`ToString` and a hand-written "copy with changes" method |
| .NET 5+ | C# 9.0+ | [references/csharp9-record-fundamentals.md](references/csharp9-record-fundamentals.md) — `record`/`record class`, positional and explicit-member syntax, init-only properties, `with`-expressions, synthesized equality/`ToString`/`Deconstruct`, record inheritance, generic records; the universal baseline |
| .NET 6+ | C# 10+ | [references/csharp10-record-structs.md](references/csharp10-record-structs.md) — `record struct`, `readonly record struct`, explicit `record class` keyword |
| .NET 7+ | C# 11+ | [references/csharp11-required-members-in-records.md](references/csharp11-required-members-in-records.md) — `required` members on a record's explicit properties (not usable on positional parameters) |
| .NET 11 (RC1 as of Sept 2026; GA expected Nov 2026) | C# 15 | [references/csharp15-closed-record-hierarchies.md](references/csharp15-closed-record-hierarchies.md) — `closed record class` hierarchies make a `switch` over the hierarchy compiler-verified exhaustive |

**C# 12, 13, 14 note:** none of these releases changed anything about how records themselves are
declared or behave — there's no `references/csharp12-*.md` through `csharp14-*.md` file because
nothing in this domain changed between C# 11 and C# 15. C# 12 does add primary constructors to
*ordinary* classes and structs, which looks like positional-record syntax but means something
different; see
[specialized/primary-constructors-vs-positional-records.md](specialized/primary-constructors-vs-positional-records.md).
If you're targeting C# 12–14, use the C# 11 reference file; everything in it still applies unchanged.

## Specialized patterns

- [specialized/record-equality-semantics-in-depth.md](specialized/record-equality-semantics-in-depth.md) — `EqualityContract` and inheritance-aware equality, shallow-copy pitfalls, stale computed properties, boxing traps comparing `record struct` through a non-generic path
- [specialized/records-vs-classes-vs-structs.md](specialized/records-vs-classes-vs-structs.md) — decision list: when a record's built-in immutability/equality replaces a plain class or a staged-assembly helper, and when cross-field validation or a very large optional-parameter surface means it doesn't
- [specialized/primary-constructors-vs-positional-records.md](specialized/primary-constructors-vs-positional-records.md) — how C# 12's primary constructors on ordinary classes/structs differ from records' own, older positional syntax
- [specialized/testing-with-records.md](specialized/testing-with-records.md) — records as a test-authoring tool: structural-equality assertions with no custom comparer, `with`-built test-case variations from a shared fixture, immutable data that can't drift between arrange and assert
