---
name: csharp-nullable-reference-types
description: Reference for C# nullable reference types (NRT) — the `#nullable enable`/`disable`/`restore` directive and project-level `<Nullable>` setting, annotation vs. warning context, `?` on reference types, the null-forgiving operator `!`, compiler flow analysis (definite assignment, narrowing after null checks), nullable generic type parameters (`where T : notnull`, unconstrained `T?`), and how nullable-governed switch exhaustiveness treats `null` as a case — from the pre-C#8 nullable-oblivious era through C# 8.0's introduction of the feature, C# 9.0's unconstrained-generic and flow-analysis refinements, C# 10's more accurate flow analysis and nullable-by-default project templates, C# 11's `required` members satisfying definite assignment, and the current C# 15 release candidate's nullable-governed exhaustive switch. Use when writing, reviewing, or porting `#nullable`-aware code, deciding `?` vs. plain reference types on a member or generic type parameter, diagnosing or suppressing a nullable warning, planning an incremental migration of an existing codebase to NRT, or writing NRT-aware test fixtures and assertions.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Nullable Reference Types

One throughline: nullable reference types are a purely compile-time static-analysis feature — a
`string?` and a `string` are the identical CLR type, and every warning the compiler emits is
advisory unless a project explicitly promotes it to an error. C# 8.0 introduced the whole
mechanism — `#nullable` contexts, `?` annotations, `!` suppression, flow-analysis narrowing, the
`notnull` constraint — and every later tier either extends what the analyzer can express (generics,
`required` members, nullable-governed switch exhaustiveness) or makes the existing analysis more
accurate, never changing runtime behavior. The C# 8.0 baseline in
[references/csharp8-nullable-reference-types.md](references/csharp8-nullable-reference-types.md)
still compiles unchanged on every later target.

## Quick start (works everywhere, C# 8.0+)

```csharp
#nullable enable

public class Order
{
    public required string OrderId { get; init; } // C# 11+; see the required-members tier below
    public string? Notes { get; init; }
}

public string Describe(Order? order)
{
    if (order is null)
    {
        return "(no order)";
    }

    return order.Notes ?? "(no notes)"; // order is narrowed non-null past the check above
}
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets, so pick the
highest tier you need and it points you downward as required.

| Target | C# language version | Reference file |
| --- | --- | --- |
| Any pre-2019 target | C# 1.0 – 7.x | [references/pre-csharp8-nullable-oblivious.md](references/pre-csharp8-nullable-oblivious.md) — no NRT; every reference type is implicitly nullable, `?` only exists for value types, defensive null checks by convention |
| .NET Core 3.0+ | C# 8.0+ | [references/csharp8-nullable-reference-types.md](references/csharp8-nullable-reference-types.md) — `#nullable`/`<Nullable>`, `?` on reference types, `!`, flow-analysis narrowing, `where T : notnull`; the universal NRT baseline |
| .NET 5+ | C# 9.0+ | [references/csharp9-unconstrained-generic-nullability.md](references/csharp9-unconstrained-generic-nullability.md) — `T?` on a fully unconstrained type parameter, `[MemberNotNull]`/`[MemberNotNullWhen]`, null-conditional-chain `!` |
| .NET 6+ | C# 10+ | [references/csharp10-flow-analysis-and-defaults.md](references/csharp10-flow-analysis-and-defaults.md) — more accurate definite-assignment/null-state analysis; new SDK project templates default to `<Nullable>enable</Nullable>` |
| .NET 7+ | C# 11+ | [references/csharp11-required-members.md](references/csharp11-required-members.md) — `required` members satisfy definite assignment without a constructor |
| .NET 11 (RC1 as of Sept 2026; GA expected Nov 2026) | C# 15 | [references/csharp15-nullable-exhaustive-switch.md](references/csharp15-nullable-exhaustive-switch.md) — a nullable-governed `switch` must handle `null` explicitly to be considered exhaustive |

**C# 12, 13, 14 note:** none of these releases changed anything about the nullable-reference-types
feature itself — there's no `references/csharp12-*.md` through `csharp14-*.md` file because nothing
in this domain changed between C# 11 and C# 15. If you're targeting C# 12–14, use the C# 11
reference file; everything in it still applies unchanged.

## Specialized patterns

- [specialized/generic-type-parameters-and-nullability.md](specialized/generic-type-parameters-and-nullability.md) — why `T?` means `Nullable<T>` or a nullable reference depending on the type argument, `notnull` vs. an unconstrained `T`, and how an oblivious type argument interacts with a nullable-enabled generic type
- [specialized/migrating-to-nullable-reference-types.md](specialized/migrating-to-nullable-reference-types.md) — enabling incrementally: per-file `#nullable enable` before a project-wide flip, `<Nullable>annotations</Nullable>`/`warnings` as staging steps, warning-as-error promotion by diagnostic code
- [specialized/null-forgiving-operator-pitfalls.md](specialized/null-forgiving-operator-pitfalls.md) — when `!` is a legitimate assertion of knowledge the compiler can't see versus a code smell that silently suppresses a real bug
- [specialized/testing-with-nullable-reference-types.md](specialized/testing-with-nullable-reference-types.md) — NRT as a concern when writing tests: which assertion-library `NotNull` helpers actually narrow the compiler's flow state, and builder patterns for constructing valid non-null test fixtures without excessive `!`
