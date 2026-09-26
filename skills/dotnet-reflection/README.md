# Reflection

This skill covers C# reflection: inspecting types at runtime, invoking members dynamically,
constructing instances and generic types from a runtime `Type`, and reading custom attributes,
gated to the C# version where each capability first became available.

## When to reach for it

- Writing or reviewing code that inspects a type's members at runtime instead of at compile time.
- Constructing an instance or a closed generic type from a `Type` you only have at runtime.
- Deciding between reflection, `dynamic`, `nameof`, or a source-generated alternative for a given
  problem.
- Caching `MethodInfo`/`PropertyInfo` lookups because repeated reflection calls are showing up in a
  profile.
- Gating reflection-based syntax by the project's `LangVersion` or target framework.

## Using it

This skill fires automatically when your request involves runtime type inspection, dynamic member
invocation, or attribute reflection in C#. You can also invoke it directly with
`/dotnet-reflection`.

## What it covers

| Topic | Reference |
| --- | --- |
| `Type`, `MethodInfo`/`PropertyInfo`/`FieldInfo`/`ConstructorInfo`, `Activator.CreateInstance` (C# 1.0+) | [references/csharp1-reflection-fundamentals.md](references/csharp1-reflection-fundamentals.md) |
| `MakeGenericType`, `MakeGenericMethod`, `IsGenericType` (C# 2.0+) | [references/csharp2-generics-reflection.md](references/csharp2-generics-reflection.md) |
| `dynamic` and DLR call sites as a reflection-adjacent alternative (C# 4.0+) | [references/csharp4-dynamic-and-callsites.md](references/csharp4-dynamic-and-callsites.md) |
| `nameof` as a refactor-safe alternative to string-literal member lookups (C# 6.0+) | [references/csharp6-nameof-reflection-safe-names.md](references/csharp6-nameof-reflection-safe-names.md) |
| `NullabilityInfoContext` for reflecting nullable reference type annotations (C# 10+) | [references/csharp10-nullabilityinfocontext.md](references/csharp10-nullabilityinfocontext.md) |

## Example prompts

- "Write a helper that builds an instance of a type by name, using its parameterless constructor."
- "Should I use reflection or a source generator to read these custom attributes at startup?"
- "This code calls `GetType().GetMethod(...)` in a hot loop. How do I cache that safely?"
