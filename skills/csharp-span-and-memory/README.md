# C# Span and Memory

Reference for C# `Span<T>`/`Memory<T>` and `ref`-based performance features: pre-`ref struct`
array/`ArraySegment<T>`/`unsafe`-pointer workarounds through `ref` returns and locals (C# 7.0),
`ref struct` and `Span<T>`/`Memory<T>` itself (C# 7.2 language feature; `System.Memory` BCL package,
stable May 2018), `ref` reassignment and safe `stackalloc` (C# 7.3), pattern-based disposal and
range/`Index` slicing (C# 8.0 / .NET Core 3.0), `ref` fields and `scoped` (C# 11 / .NET 7),
`ref readonly` parameters (C# 12 / .NET 8), `ref struct`s as generic type arguments via
`allows ref struct` (C# 13 / .NET 9), first-class implicit `Span<T>` conversions (C# 14 / .NET 10),
and the preview updated `unsafe` model (C# 15 / .NET 11). The routing table is in
[SKILL.md](SKILL.md).

```text
references/                                          version-gated core syntax, oldest to newest
  pre-csharp7-arrays-and-pointers.md                   C# 1.0 – 6.0 — arrays, ArraySegment<T>, unsafe pointers
  csharp7-ref-returns-and-locals.md                    C# 7.0+ — ref returns, ref locals
  csharp7.2-ref-struct-and-span.md                     C# 7.2+ — ref struct, in, ref readonly returns, stackalloc; language-vs-BCL split
  csharp7.3-ref-reassignment-and-stackalloc-init.md    C# 7.3+ — ref reassignment, stackalloc initializer syntax
  csharp8-span-foreach-and-ranges.md                   C# 8.0+ — pattern-based Dispose(), range/Index slicing
  csharp11-ref-fields-and-scoped.md                    C# 11+ — ref fields, scoped modifier
  csharp12-ref-readonly-parameters.md                  C# 12+ — ref readonly parameters
  csharp13-allows-ref-struct.md                        C# 13+ — allows ref struct anti-constraint
  csharp14-implicit-span-conversions.md                C# 14+ — implicit Span<T>/ReadOnlySpan<T>/T[] conversions
  csharp15-unsafe-model-preview.md                     C# 15 preview — updated unsafe model (RC caveat)

specialized/                                         cross-cutting patterns, applicable across versions
  span-vs-memory-vs-readonlymemory.md
  ref-struct-constraints-and-limitations.md
  arraypool-and-buffer-pooling.md
  memorymarshal-interop-patterns.md
  testing-with-span-and-memory.md
```

## Version coverage

| .NET | C# | GA | Span/Memory-relevant additions |
| --- | --- | --- | --- |
| Framework 1.0 – 4.6.1 / Core pre-1.0 | 1.0 – 6.0 | 2002 – 2015 | no `ref struct`, no `Span<T>`: `ArraySegment<T>` (Fx 2.0), raw `offset`/`count` parameters, or `unsafe` pointers were the only low-allocation options |
| Framework 4.6.2+ / Core 1.0+ | 7.0 | Mar 2017 | `ref` returns, `ref` locals |
| — | 7.2 | Dec 2017 (VS 2017 15.5) | `ref struct`, `readonly struct`/`readonly ref struct`, `in` parameters, `ref readonly` returns, `stackalloc` in nested expressions — language feature only; `Span<T>`/`Memory<T>` shipped as the `System.Memory` NuGet package, stable 4.5.0 May 29, 2018, then part of the shared framework from .NET Core 2.1, fully built in from .NET Core 3.0 |
| — | 7.3 | May 2018 (VS 2017 15.7) | `ref` local reassignment (`r = ref v`), `stackalloc` array-initializer syntax convertible to `Span<T>`/`ReadOnlySpan<T>` without `unsafe` |
| Core 3.0 | 8.0 | Sep 2019 | pattern-based `Dispose()` lets a `ref struct` participate in `using` without implementing `IDisposable`; range (`..`) and `Index` (`^`) operators slice `Span<T>`/`ReadOnlySpan<T>` with no allocation |
| 7 | 11 | Nov 2022 | `ref` fields declarable inside a `ref struct`; `scoped` modifier narrows a `ref`-like value's inferred escape scope |
| 8 | 12 | Nov 2023 | `ref readonly` parameters — like `in`, but rejects non-addressable call-site arguments instead of silently copying them |
| 9 | 13 | Nov 2024 | `allows ref struct` anti-constraint: `ref struct` types (including `Span<T>`) usable as generic type arguments for the first time |
| 10 | 14 | Nov 2025 | first-class implicit conversions among `T[]`, `Span<T>`, `ReadOnlySpan<T>`; span types as extension-method receivers and in generic type inference |
| 11 | 15 | RC1 Sep 2026, GA expected Nov 2026 | preview-only, opt-in updated `unsafe` model narrowing `unsafe` to pointer-indirection operations specifically, not pointer types generally |

Each reference file states its own fallback file, so a project pinned to an older `LangVersion`
than its target SDK supports can still find the right syntax tier.
