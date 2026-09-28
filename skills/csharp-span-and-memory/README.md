# C# Span and Memory

Helps you write, review, or port low-allocation, high-performance C# code built on `Span<T>`,
`Memory<T>`, and the `ref`-based features around them (`ref struct`, `stackalloc`, `ref` returns
and locals, `ArrayPool<T>` pooling, and `MemoryMarshal`), while respecting the compiler's guarantee
that a stack-only view never outlives the frame that created it.

## When to reach for it

- Choosing between `Span<T>`, `Memory<T>`, and `ReadOnlyMemory<T>` for a method signature
- Pooling buffers with `ArrayPool<T>` instead of allocating a new array per call
- Working out why a `ref struct` can't be stored on the heap, boxed, or captured in a closure
- Deciding between `ref`, `in`, and `ref readonly` on a parameter
- Slicing a span with range/`Index` operators instead of manual offset/length math

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting
low-allocation code that touches `Span<T>`/`Memory<T>` or `ref`-based features.

## What it covers

| Topic | Reference |
| --- | --- |
| Arrays, `ArraySegment<T>`, unsafe pointers (the pre-`ref struct` era) | [references/pre-csharp7-arrays-and-pointers.md](references/pre-csharp7-arrays-and-pointers.md) |
| `ref` returns and locals | [references/csharp7-ref-returns-and-locals.md](references/csharp7-ref-returns-and-locals.md) |
| `ref struct`, `in`, `ref readonly` returns, `stackalloc` | [references/csharp7.2-ref-struct-and-span.md](references/csharp7.2-ref-struct-and-span.md) |
| `ref` reassignment, `stackalloc` initializer syntax | [references/csharp7.3-ref-reassignment-and-stackalloc-init.md](references/csharp7.3-ref-reassignment-and-stackalloc-init.md) |
| Pattern-based `Dispose()`, range/`Index` slicing | [references/csharp8-span-foreach-and-ranges.md](references/csharp8-span-foreach-and-ranges.md) |
| `ref` fields, `scoped` modifier | [references/csharp11-ref-fields-and-scoped.md](references/csharp11-ref-fields-and-scoped.md) |
| `ref readonly` parameters | [references/csharp12-ref-readonly-parameters.md](references/csharp12-ref-readonly-parameters.md) |
| `allows ref struct` anti-constraint | [references/csharp13-allows-ref-struct.md](references/csharp13-allows-ref-struct.md) |
| Implicit `Span<T>`/`ReadOnlySpan<T>`/`T[]` conversions | [references/csharp14-implicit-span-conversions.md](references/csharp14-implicit-span-conversions.md) |
| Updated `unsafe` model (preview) | [references/csharp15-unsafe-model-preview.md](references/csharp15-unsafe-model-preview.md) |

## Example prompts

- "Should this parsing method take a `ReadOnlySpan<char>` or a `string`?"
- "Rent a buffer from `ArrayPool<byte>` for this hot path instead of allocating a new array."
- "Why can't I store this `ref struct` as a field on my class?"
