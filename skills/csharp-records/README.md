# C# Records

Reference for the `record` keyword and the immutability/value-equality patterns it replaces or
extends. The routing table is in [SKILL.md](SKILL.md).

**`references/`** — version-gated core syntax, oldest to newest

| File | Covers |
| --- | --- |
| `pre-csharp9-manual-value-types.md` | pre-2020 (C# 1.0-8.0) — hand-written Equals/GetHashCode/ToString and a hand-written copy method |
| `csharp9-record-fundamentals.md` | C# 9.0 — record/record class, positional syntax, with-expressions, synthesized equality, inheritance, generic records |
| `csharp10-record-structs.md` | C# 10 — record struct, readonly record struct, explicit record class |
| `csharp11-required-members-in-records.md` | C# 11 — required members on explicit record properties |
| `csharp15-closed-record-hierarchies.md` | C# 15 (RC) — closed record class hierarchies and exhaustive switch |

**`specialized/`** — cross-cutting patterns, applicable across versions

| File | Covers |
| --- | --- |
| `record-equality-semantics-in-depth.md` | EqualityContract, shallow copies, stale computed properties, record struct boxing |
| `records-vs-classes-vs-structs.md` | decision list: record vs. plain class/struct vs. staged assembly |
| `primary-constructors-vs-positional-records.md` | C# 12 primary constructors on ordinary types vs. records' own positional syntax |
| `testing-with-records.md` | records as a test-authoring tool |

## Version coverage

| .NET | C# | GA | Records-relevant additions |
| --- | --- | --- | --- |
| Any pre-.NET 5 target | 1.0 – 8.0 | — | No `record` keyword; manual `Equals`/`GetHashCode`/`ToString` plus a hand-written copy-with-changes method |
| .NET 5 | 9.0 | November 2020 | `record`/`record class`, positional and explicit-member syntax, init-only properties, `with`-expressions, synthesized equality/`ToString`/`Deconstruct`, record inheritance |
| .NET 6 | 10 | November 2021 | `record struct`, `readonly record struct`, explicit `record class` keyword |
| .NET 7 | 11 | November 2022 | `required` members usable on a record's explicit properties (not positional parameters) |
| .NET 8 | 12 | November 2023 | Primary constructors extended to ordinary classes/structs — related but not a records change; covered in `specialized/` |
| .NET 9 | 13 | November 2024 | No record-specific change |
| .NET 10 | 14 | November 2025 | No record-specific change |
| .NET 11 | 15 | RC1 Sept 2026; GA expected Nov 2026 | `closed record class` hierarchies for compiler-verified exhaustive `switch` |

<!-- Keep this table's rows in sync with SKILL.md's routing table -- same tiers, same order.
     On a maintenance pass, re-check C# 15's RC/GA status before appending a new row. -->
