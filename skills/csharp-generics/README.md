# C# Generics

Reference for C# generics: generic types, methods, and constraints (C# 2.0 / .NET Framework 2.0)
through variance, value tuples, `notnull`/`unmanaged` constraints, generic math and attributes
(C# 11 / .NET 7), ref struct type arguments (C# 13 / .NET 9), and generic extension blocks
(C# 14 / .NET 10, C# 15 / .NET 11). The routing table is in [SKILL.md](SKILL.md).

**`references/`** — version-gated core syntax, oldest to newest

| File | Covers |
| --- | --- |
| `csharp2-generics-fundamentals.md` | .NET Framework 2.0+ (C# 2.0+) — the universal baseline |
| `csharp4-variance.md` | .NET Framework 4.0+ (C# 4.0+) — out/in variance |
| `csharp7-tuples-and-constraints.md` | .NET Core 1.0 – .NET Fx 4.7.2 (C# 7.0–7.3) — value tuples, unmanaged/enum/delegate constraints |
| `csharp8-nullable-generics.md` | .NET Core 3.0+ (C# 8.0+) — notnull constraint, nullable type parameters |
| `csharp11-generic-math-and-attributes.md` | .NET 7 (C# 11) — generic attributes, generic math |
| `csharp13-ref-struct-generics.md` | .NET 9 (C# 13) — allows ref struct |
| `csharp14-generic-extension-blocks.md` | .NET 10 / .NET 11 RC1+ (C# 14 / C# 15) — generic extension blocks |

```text
specialized/                           cross-cutting patterns, applicable across versions
  generic-constraints-reference.md
  variance-in-depth.md
  generic-type-inference-and-overload-resolution.md
  generic-collections-and-linq.md
  generic-delegates-and-attributes.md
  generic-math-numeric-abstractions.md
  curiously-recurring-generic-pattern.md
  generics-for-testing.md
```

## Version coverage

| .NET | C# | GA | Generics-relevant additions |
| --- | --- | --- | --- |
| Framework 2.0 | 2.0 | 2005 | generics introduced: types, methods, `where` constraints |
| Framework 4.0 | 4.0 | 2010 | `out`/`in` variance on interfaces and delegates |
| Core 1.0 – Framework 4.7.2 | 7.0 – 7.3 | 2017 – 2018 | value tuples; `unmanaged`, `enum`, `delegate` constraints |
| Core 3.0 | 8.0 | 2019 | `notnull` constraint; nullable reference type parameters |
| 7 | 11 | Nov 2022 | generic attributes; static abstract interface members (generic math) |
| 9 | 13 | Nov 2024 | `allows ref struct` anti-constraint |
| 10 | 14 | Nov 2025 | generic `extension<T>(...)` blocks |
| 11 | 15 | RC1 Sept 2026, GA expected Nov 2026 | no new core-generics syntax; ships union types/closed hierarchies (generic case types supported — a union's case types and a closed hierarchy's derived types can each be closed generic types like any other, no special syntax beyond ordinary generic type arguments) |

Each reference file states its own fallback file, so a project pinned to an older `LangVersion`
than its target SDK supports can still find the right syntax tier.
