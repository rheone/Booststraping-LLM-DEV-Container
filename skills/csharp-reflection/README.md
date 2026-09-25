# C# Reflection

Reference for C# reflection: `System.Type`, `MethodInfo`/`PropertyInfo`/`FieldInfo`/
`ConstructorInfo`, and `Activator.CreateInstance` (C# 1.0 / .NET Framework 1.0, 2002), generic
type/method reflection via `MakeGenericType`/`MakeGenericMethod`/`IsGenericType` (C# 2.0 / .NET
Framework 2.0, Nov 2005), `dynamic` and DLR call sites as a reflection-adjacent alternative
(C# 4.0 / .NET Framework 4.0, Apr 2010), `nameof` as a refactor-safe alternative to reflecting by
string (C# 6.0 / .NET Framework 4.6, Jul 2015), and `NullabilityInfoContext` for reflecting
nullable reference type annotations (C# 10 / .NET 6, Nov 2021). The routing table is in
[SKILL.md](SKILL.md).

```text
references/                                  version-gated core syntax, oldest to newest
  csharp1-reflection-fundamentals.md           .NET Fx 1.0+ (C# 1.0+) — Type, MemberInfo family, Activator; the universal baseline
  csharp2-generics-reflection.md               .NET Fx 2.0+ (C# 2.0+) — MakeGenericType, MakeGenericMethod, IsGenericType
  csharp4-dynamic-and-callsites.md             .NET Fx 4.0+ (C# 4.0+) — dynamic, DLR call sites
  csharp6-nameof-reflection-safe-names.md      .NET Fx 4.6+ (C# 6.0+) — nameof instead of string-literal member lookups
  csharp10-nullabilityinfocontext.md           .NET 6+ (C# 10+) — NullabilityInfoContext

specialized/                                 cross-cutting patterns, applicable across versions
  reflection-performance-and-caching.md
  source-generators-vs-reflection.md
  detecting-compiler-lowered-member-shapes.md
  testing-with-reflection.md
```

## Version coverage

| .NET | C# | GA | Reflection-relevant additions |
| --- | --- | --- | --- |
| Framework 1.0 | 1.0 | 2002 | `System.Reflection`/`System.Type` ship as baseline BCL surface: `Type`, `MethodInfo`, `PropertyInfo`, `FieldInfo`, `ConstructorInfo`, `Activator.CreateInstance`, custom attribute inspection |
| Framework 2.0 | 2.0 | Nov 2005 | generics reflection: `MakeGenericType`, `MakeGenericMethod`, `IsGenericType`, `IsGenericTypeDefinition`, `GetGenericArguments` |
| Framework 4.0 | 4.0 | Apr 2010 | `dynamic` keyword and the Dynamic Language Runtime; call-site-cached member access as an alternative to hand-written `GetMethod`/`Invoke` |
| Framework 4.5 | 5.0 | Aug 2012 | no new reflection-specific language syntax (BCL addition in the same wave: `CustomAttributeExtensions.GetCustomAttribute<T>()`, `MethodImplOptions.AggressiveInlining`) |
| Framework 4.6 | 6.0 | Jul 2015 | `nameof` operator — refactor-safe alternative to string-literal member lookups |
| Core 1.0–3.1 / Fx 4.6.2+ | 7.0–7.3 | 2017–2018 | no new reflection-specific language syntax |
| Core 3.0 | 8.0 | Sep 2019 | nullable reference type annotations ship (the *source* of what C# 10's `NullabilityInfoContext` later reads); no reflection API to read them yet |
| 5 | 9.0 | Nov 2020 | no new `System.Reflection` API; ships `ISourceGenerator`, a reflection *alternative* — see [specialized/source-generators-vs-reflection.md](specialized/source-generators-vs-reflection.md); init-only properties change what reflection sees with no new reflection API — see [specialized/detecting-compiler-lowered-member-shapes.md](specialized/detecting-compiler-lowered-member-shapes.md) |
| 6 | 10.0 | Nov 2021 | `NullabilityInfoContext` ships (reflecting C# 8's nullable annotations, two versions later); `IIncrementalGenerator` and `System.Text.Json` source generation also ship this release |
| 7 | 11.0 | Nov 2022 | no new `System.Reflection` API; required members (`RequiredMemberAttribute`) change what reflection sees with no new reflection API; `[GeneratedRegex]` source generation ships |
| 8 | 12.0 | Nov 2023 | no new reflection-specific language syntax; `[UnsafeAccessor]` ships as a compile-time-bound alternative to private-member reflection; `System.Text.Json` source generation reaches functional parity with reflection-based serialization |
| 9 | 13.0 | Nov 2024 | no new reflection-specific language syntax |
| 10 | 14.0 | Nov 2025 | no new `System.Reflection` API; extension members change what `GetMethods()` sees on the extending class (never on the extended type) with no new reflection API |
| 11 | 15.0 | RC1 Sep 2026, GA expected Nov 2026 | no new reflection-specific language syntax as of RC1 |

Each reference file states its own fallback file, so a project pinned to an older `LangVersion`
than its target SDK supports can still find the right syntax tier.
