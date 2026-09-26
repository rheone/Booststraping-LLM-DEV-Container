# C# Nullable Reference Types

Reference for the nullable-reference-types (NRT) language feature and the compile-time static
flow analysis it's built on. The routing table is in [SKILL.md](SKILL.md).

**`references/`** — version-gated core syntax, oldest to newest

| File | Covers |
| --- | --- |
| `pre-csharp8-nullable-oblivious.md` | pre-2019 (C# 1.0-7.x) — no NRT; every reference type implicitly nullable, defensive checks by convention |
| `csharp8-nullable-reference-types.md` | C# 8.0 — `#nullable`/`<Nullable>`, `?` on reference types, `!`, flow-analysis narrowing, `where T : notnull` |
| `csharp9-unconstrained-generic-nullability.md` | C# 9.0 — unconstrained T?, [MemberNotNull]/[MemberNotNullWhen], null-conditional-chain ! |
| `csharp10-flow-analysis-and-defaults.md` | C# 10 — more accurate flow analysis; SDK templates default to Nullable enable |
| `csharp11-required-members.md` | C# 11 — required members satisfy definite assignment without a constructor |
| `csharp15-nullable-exhaustive-switch.md` | C# 15 (RC) — nullable-governed switch exhaustiveness requires a null arm |

**`specialized/`** — cross-cutting patterns, applicable across versions

| File | Covers |
| --- | --- |
| `generic-type-parameters-and-nullability.md` | `T?` as `Nullable<T>` vs. nullable reference, `notnull` vs. unconstrained `T`, oblivious type arguments |
| `migrating-to-nullable-reference-types.md` | incremental enabling, annotations/warnings staging, warning-as-error promotion |
| `null-forgiving-operator-pitfalls.md` | legitimate uses of ! vs. code smell / bug-masking uses |
| `testing-with-nullable-reference-types.md` | NRT as a test-authoring concern: assertion-library narrowing, non-null fixture builders |

## Version coverage

| .NET | C# | GA | NRT-relevant additions |
| --- | --- | --- | --- |
| Any pre-.NET-Core-3.0 target | 1.0 – 7.x | — | No nullable reference types; every reference type implicitly nullable, `?` reserved for `Nullable<T>` |
| .NET Core 3.0 | 8.0 | September 2019 | Nullable reference types introduced: `#nullable`/`<Nullable>`, `?` on reference types, `!`, flow-analysis narrowing, `where T : notnull`, the nullable-analysis attribute family (`[NotNull]`, `[MaybeNull]`, `[NotNullWhen]`, `[MaybeNullWhen]`, `[AllowNull]`, `[DisallowNull]`, `[NotNullIfNotNull]`, `[DoesNotReturn]`, `[DoesNotReturnIf]`) |
| .NET 5 | 9.0 | November 2020 | Unconstrained type parameter `T?` annotations, `[MemberNotNull]`/`[MemberNotNullWhen]` attributes, null-conditional-chain `!` support |
| .NET 6 | 10 | November 2021 | More accurate definite-assignment/null-state flow analysis; new SDK project templates default to `<Nullable>enable</Nullable>` |
| .NET 7 | 11 | November 2022 | `required` members satisfy definite assignment for non-nullable members without a constructor |
| .NET 8 | 12 | November 2023 | No NRT-specific change |
| .NET 9 | 13 | November 2024 | No NRT-specific change |
| .NET 10 | 14 | November 2025 | No NRT-specific change |
| .NET 11 | 15 | RC1 Sept 2026; GA expected Nov 2026 | Nullable-governed `switch` exhaustiveness requires an explicit `null` arm |

<!-- Keep this table's rows in sync with SKILL.md's routing table -- same tiers, same order.
     On a maintenance pass, re-check C# 15's RC/GA status before appending a new row. -->
