---
name: csharp-reflection
description: Reference for C# reflection — System.Type, Type.GetType, MethodInfo/PropertyInfo/FieldInfo/ConstructorInfo, Activator.CreateInstance, dynamic member invocation, custom attribute inspection, and generic type/method reflection (MakeGenericType, MakeGenericMethod, IsGenericType) — baseline since C# 1.0 / .NET Framework 1.0 (2002), generic reflection added in C# 2.0 (2005), the dynamic keyword and DLR call sites as a reflection-adjacent alternative in C# 4.0 (2010), nameof as a refactor-safe alternative to reflecting by string in C# 6.0 (2015), and NullabilityInfoContext for reflecting nullable reference type annotations in C# 10 / .NET 6 (2021). Use when writing or reviewing code that inspects types at runtime, invokes members dynamically, constructs instances or generic types from a runtime Type, reads custom attributes, decides between reflection and a source-generated alternative, caches MethodInfo/PropertyInfo for performance, writes reflection-based test helpers, or gates reflection syntax by C# language version.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Reflection

Reflection's core API — `System.Type`, `MethodInfo`/`PropertyInfo`/`FieldInfo`/`ConstructorInfo`,
`Activator.CreateInstance` — has existed since C# 1.0 / .NET Framework 1.0 and changed remarkably
little at the *language* level since: most of reflection's real evolution is BCL and runtime
additions layered on that unchanged foundation, not new C# syntax. The baseline in
[references/csharp1-reflection-fundamentals.md](references/csharp1-reflection-fundamentals.md)
still compiles unchanged on C# 15. Four later versions each add one genuinely new,
version-verified capability on top of it: generic type/method reflection (C# 2.0), `dynamic`
member access as a reflection alternative (C# 4.0), `nameof` as a refactor-safe substitute for
string-literal member lookups (C# 6.0), and reflecting nullable reference type annotations via
`NullabilityInfoContext` (C# 10 / .NET 6 — two full versions after nullable reference types
themselves shipped in C# 8, since the reflection API to read them lagged behind the language
feature). Most other C# versions add nothing to reflection's domain; that's a real, verified
finding, not a gap in this skill — see the routing table below.

## Quick start (works everywhere, C# 1.0+)

```csharp
Type orderType = typeof(Order);
object order = Activator.CreateInstance(orderType);

PropertyInfo statusProperty = orderType.GetProperty(nameof(Order.Status));
statusProperty.SetValue(order, OrderStatus.Pending);

MethodInfo validateMethod = orderType.GetMethod(nameof(Order.Validate));
object isValid = validateMethod.Invoke(order, parameters: null);
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets.

| Target | C# language version | Reference file |
| --- | --- | --- |
| .NET Framework 1.0+ | C# 1.0+ | [references/csharp1-reflection-fundamentals.md](references/csharp1-reflection-fundamentals.md) — `Type`, `MethodInfo`/`PropertyInfo`/`FieldInfo`/`ConstructorInfo`, `Activator.CreateInstance`, custom attribute inspection; the universal baseline |
| .NET Framework 2.0+ | C# 2.0+ | [references/csharp2-generics-reflection.md](references/csharp2-generics-reflection.md) — `MakeGenericType`, `MakeGenericMethod`, `IsGenericType`/`IsGenericTypeDefinition`, generic constraint reflection |
| .NET Framework 4.0+ | C# 4.0+ | [references/csharp4-dynamic-and-callsites.md](references/csharp4-dynamic-and-callsites.md) — `dynamic` and DLR call sites as a higher-level alternative to hand-written `GetMethod`/`Invoke` |
| .NET Framework 4.6+ | C# 6.0+ | [references/csharp6-nameof-reflection-safe-names.md](references/csharp6-nameof-reflection-safe-names.md) — `nameof` as a refactor-safe alternative to string-literal member lookups |
| .NET 6+ | C# 10+ | [references/csharp10-nullabilityinfocontext.md](references/csharp10-nullabilityinfocontext.md) — `NullabilityInfoContext`, reflecting nullable reference type annotations |
| .NET 5 (C# 9) through .NET 10 (C# 14); .NET 11 RC1+ (C# 15) as of Sept 2026, GA expected Nov 2026 | C# 3, 5, 7, 7.1–7.3, 8, 9, 11–15 | no reference file for any of these — none adds new `System.Reflection`/`System.Type` API surface. C# 9's source generators are a reflection *alternative*, not a reflection API change (see specialized/source-generators-vs-reflection.md); C# 9's init-only properties, C# 11's required members, and C# 14's extension members each change what reflection sees without adding reflection API (see specialized/detecting-compiler-lowered-member-shapes.md); C# 8's nullable reference types are the annotation source C# 10's NullabilityInfoContext reads, but shipped two versions before any reflection API existed to read them |

## Specialized patterns

- [specialized/reflection-performance-and-caching.md](specialized/reflection-performance-and-caching.md) — caching `MemberInfo` lookups, `MethodImplOptions`, `Delegate.CreateDelegate`/`DynamicMethod` as fast-invocation escape hatches
- [specialized/source-generators-vs-reflection.md](specialized/source-generators-vs-reflection.md) — `ISourceGenerator` (C# 9) vs. `IIncrementalGenerator` (C# 10), `System.Text.Json` and `[GeneratedRegex]` source generation, `[UnsafeAccessor]` (.NET 8), and when reflection is still the right tool
- [specialized/detecting-compiler-lowered-member-shapes.md](specialized/detecting-compiler-lowered-member-shapes.md) — detecting init-only properties, required members, and extension members through reflection, none of which have dedicated reflection API
- [specialized/testing-with-reflection.md](specialized/testing-with-reflection.md) — invoking private members under test, asserting on type shape, generating theory data from a type's declared members
