---
name: csharp-span-and-memory
description: Reference for C# Span<T>/Memory<T> and ref-based performance features — ref struct, readonly ref struct, in parameters, ref readonly returns and parameters, stackalloc, ref locals/returns and ref reassignment, ArrayPool<T> buffer pooling, MemoryMarshal — from pre-ref-struct array/ArraySegment<T>/unsafe-pointer workarounds through ref returns and locals (C# 7.0), ref struct and Span<T>/Memory<T> itself (C# 7.2 language feature / System.Memory BCL package, 2018), ref reassignment and safe stackalloc (C# 7.3), pattern-based disposal and range/Index slicing (C# 8.0), ref fields and scoped (C# 11), ref readonly parameters (C# 12), ref structs as generic type arguments via allows ref struct (C# 13), first-class implicit Span conversions (C# 14), and the preview updated unsafe model (C# 15). Use when writing, reviewing, or porting low-allocation/high-performance C# code, choosing between Span<T>/Memory<T>/ReadOnlyMemory<T>, pooling buffers with ArrayPool<T>, working with ref struct constraints, using ref/in/ref readonly parameters, or gating any of this syntax by C# language version.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Span and Memory

`Span<T>`/`Memory<T>` and the `ref`-based features around them exist to let code work directly with
contiguous memory — stack, heap array, or unmanaged — without copying it, while the compiler still
enforces that a stack-only view never outlives the stack frame that created it. The throughline
across every tier below is that guarantee: `ref struct` (C# 7.2) is the type-system mechanism that
makes it possible at all, and everything from C# 7.0's `ref` returns through C# 13's
`allows ref struct` either builds the foundation for it or removes a restriction that foundation
originally required. The baseline in
[references/csharp7.2-ref-struct-and-span.md](references/csharp7.2-ref-struct-and-span.md) still
compiles unchanged on C# 15 — nothing later removes or breaks any of it.

## Quick start (works everywhere, C# 7.2+ with `Span<T>` available)

```csharp
public static int Sum(ReadOnlySpan<int> values)
{
    int total = 0;
    foreach (int v in values)
    {
        total += v;
    }
    return total;
}

int[] numbers = { 1, 2, 3, 4, 5 };
int total = Sum(numbers.AsSpan()); // .AsSpan() needed pre-C#14; implicit from C# 14 on
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets.

| Target | C# language version | Reference file |
| --- | --- | --- |
| Any (no `ref struct`/`Span<T>` at all) | C# 1.0 – 6.0 | [references/pre-csharp7-arrays-and-pointers.md](references/pre-csharp7-arrays-and-pointers.md) — arrays, `ArraySegment<T>`, `unsafe` pointers as the only low-allocation options |
| .NET Fx 4.6.2+ / .NET Core 1.0+ | C# 7.0+ | [references/csharp7-ref-returns-and-locals.md](references/csharp7-ref-returns-and-locals.md) — `ref` returns and `ref` locals, the aliasing foundation `Span<T>` later builds on |
| VS 2017 15.5+; `Span<T>`/`Memory<T>` need `System.Memory` NuGet (stable May 2018) or .NET Core 2.1+/.NET Fx 4.7.2+ | C# 7.2+ | [references/csharp7.2-ref-struct-and-span.md](references/csharp7.2-ref-struct-and-span.md) — `ref struct`, `readonly ref struct`, `in` parameters, `ref readonly` returns, `stackalloc`; the language-version-vs-BCL-version split |
| VS 2017 15.7+ / .NET Core SDK 2.1.300+ | C# 7.3+ | [references/csharp7.3-ref-reassignment-and-stackalloc-init.md](references/csharp7.3-ref-reassignment-and-stackalloc-init.md) — `ref` local reassignment (`r = ref v`), `stackalloc` array-initializer syntax with no `unsafe` |
| .NET Core 3.0+ | C# 8.0+ | [references/csharp8-span-foreach-and-ranges.md](references/csharp8-span-foreach-and-ranges.md) — pattern-based `Dispose()` for `ref struct`s, range/`Index` (`^`, `..`) slicing over `Span<T>` |
| .NET 7+ | C# 11+ | [references/csharp11-ref-fields-and-scoped.md](references/csharp11-ref-fields-and-scoped.md) — `ref` fields inside a `ref struct`, the `scoped` modifier |
| .NET 8+ | C# 12+ | [references/csharp12-ref-readonly-parameters.md](references/csharp12-ref-readonly-parameters.md) — `ref readonly` parameters |
| .NET 9+ | C# 13+ | [references/csharp13-allows-ref-struct.md](references/csharp13-allows-ref-struct.md) — `allows ref struct` anti-constraint; `ref struct` types as generic type arguments for the first time |
| .NET 10+ | C# 14+ | [references/csharp14-implicit-span-conversions.md](references/csharp14-implicit-span-conversions.md) — first-class implicit conversions among `T[]`, `Span<T>`, `ReadOnlySpan<T>`; span types as extension-method receivers |
| .NET 11 — **preview**, RC1 as of Sept 2026, GA expected Nov 2026 | C# 15 preview | [references/csharp15-unsafe-model-preview.md](references/csharp15-unsafe-model-preview.md) — opt-in updated `unsafe` model narrowing where `unsafe` is required around pointer types |

## Specialized patterns

- [specialized/span-vs-memory-vs-readonlymemory.md](specialized/span-vs-memory-vs-readonlymemory.md) — when to reach for `Span<T>` vs. `Memory<T>`/`ReadOnlyMemory<T>`, and why the latter exist for async/heap-storable cases `Span<T>` structurally cannot cover
- [specialized/ref-struct-constraints-and-limitations.md](specialized/ref-struct-constraints-and-limitations.md) — the full `ref struct` restriction list: no boxing, no interface (pre-13), no field on a non-`ref struct` type, no lambda capture, no surviving an `await`
- [specialized/arraypool-and-buffer-pooling.md](specialized/arraypool-and-buffer-pooling.md) — `ArrayPool<T>` rent/return patterns, clearing sensitive buffers, avoiding double-return and use-after-return leaks
- [specialized/memorymarshal-interop-patterns.md](specialized/memorymarshal-interop-patterns.md) — `MemoryMarshal.Cast`/`AsBytes`/`GetReference`/`TryGetArray` for zero-copy reinterpretation and interop
- [specialized/testing-with-span-and-memory.md](specialized/testing-with-span-and-memory.md) — asserting on span contents without allocating, testing `Span<T>`-accepting APIs, why a `ref struct` can't be captured in a test framework's lambda-based assertion helpers or used across `await` in a test
