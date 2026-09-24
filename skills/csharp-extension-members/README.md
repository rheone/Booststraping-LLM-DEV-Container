# C# Extension Members

Reference for C# extension methods and extension members: from the classic `this`-parameter form
(C# 3.0 / .NET Framework 3.5) through extension properties, static extension members, and
operators (C# 14 / .NET 10), and extension indexers (C# 15 / .NET 11). Includes the pre-C#3
fallback pattern for .NET Framework 1.0–2.0, generic extension members, and a reference on writing
extension methods/members as test-authoring tools. The routing table is in [SKILL.md](SKILL.md).

```text
references/                       version-gated core syntax, oldest to newest
  pre-csharp3-no-extensions.md      .NET Framework 1.0–2.0 — no extension mechanism (fallback pattern)
  csharp3-extension-methods.md      .NET Framework 3.5 – .NET 9 (C# 3.0–13) — classic `this` syntax
  csharp8-nullable-extensions.md    .NET Core 3.0+ (C# 8.0+) — nullable annotations
  csharp14-extension-members.md     .NET 10 (C# 14) — extension blocks: properties, statics, operators
  csharp15-extension-indexers.md    .NET 11 RC1+ (C# 15) — extension indexers

specialized/                      cross-cutting patterns, applicable across versions
  generic-extension-members.md
  extension-properties.md
  static-extension-members.md
  extension-operators.md
  fluent-and-linq-style-patterns.md
  resolution-and-coexistence-rules.md
  testing-extension-members.md      extension methods/members as test-authoring tools
```

## Version coverage

| .NET | C# | GA | Extension-member support |
| --- | --- | --- | --- |
| Framework 1.0 – 2.0 | 1.0 – 2.0 | 2002 – 2005 | none |
| Framework 3.5 – .NET 9 | 3.0 – 13 | 2007 – Nov 2024 | classic extension methods |
| .NET 10 | 14 | Nov 2025 | + extension properties, static members, operators |
| .NET 11 | 15 | RC1 Sept 2026, GA expected Nov 2026 | + extension indexers |

Each reference file states its own fallback file, so a project pinned to an older `LangVersion`
than its target SDK supports can still find the right syntax tier.
