# Updated `unsafe` model preview (C# 15 / .NET 11, still in preview)

**Preview caveat:** as of this session (September 2026), this feature ships as an **opt-in preview**
in C# 15 / .NET 11 — .NET 11 itself was in Preview (Preview 1 shipped February 2026, with Preview 3
out by April 2026), with GA scheduled for **November 2026**. This tier's syntax may still shift
before GA; treat it as directional, not a stable target, and re-verify against current
`learn.microsoft.com` documentation before depending on it in shipping code.

C# 15 begins a multi-release effort to redefine what actually requires an `unsafe` context. Today
(C# 7.2 through 14), declaring a pointer type, taking an address with `&`, using `fixed`, or
converting a `stackalloc` expression to a pointer all require `unsafe` — even though none of those
operations alone touches unmanaged memory unsafely; only *dereferencing* the pointer does. The
preview model ties the `unsafe` requirement to the operation that actually reads or writes through
a pointer, not to the mere existence of a pointer-typed value.

## Syntax

```xml
<!-- opt-in required: project file -->
<PropertyGroup>
  <LangVersion>preview</LangVersion>
  <Features>$(Features);updated-memory-safety-rules</Features>
</PropertyGroup>
```

```csharp
// under the updated model: declaring the pointer type and taking its address need no `unsafe`
int value = 42;
int* pointer = &value; // legal in a safe context under the preview model

// dereferencing still requires unsafe — this is the operation the model actually gates
unsafe
{
    int read = *pointer;
}
```

## Basic use case: `stackalloc` to a pointer without an enclosing `unsafe` block

```csharp
// under the updated model: the stackalloc-to-pointer conversion itself doesn't dereference anything
byte* buffer = stackalloc byte[64]; // no `unsafe` needed for this line specifically

unsafe
{
    buffer[0] = 0xFF; // indexing through the pointer dereferences — still gated
}
```

This narrows the surface area an `unsafe` block has to cover in code that mixes pointer plumbing
(common in `Span<T>`/interop helper code) with the smaller number of lines that actually touch the
pointed-to memory — under today's (pre-C#15) rules, the entire surrounding method or block needs
`unsafe` the moment any pointer type appears anywhere in it.

## Requirements and restrictions

- Requires explicit opt-in via `<LangVersion>preview</LangVersion>` and the
  `updated-memory-safety-rules` feature flag — it is not part of the default C# 15 experience even
  once .NET 11 reaches GA on the currently-stated schedule, unless that defaults during the preview
  period; re-check before relying on this.
- Still gates the actual unsafe operation: pointer *indirection* (dereferencing to read or write
  through a pointer) requires `unsafe` under the new model exactly as under the old one — this is a
  narrowing of *where* `unsafe` is required, not a removal of the safety boundary itself.

## Fallback

Without opting into the preview feature (the default on any C# 15/.NET 11 project, and every
earlier tier back through C# 7.2), the existing rule applies: any pointer type, `&` address-of,
`fixed` statement, or `stackalloc`-to-pointer conversion anywhere in a method requires the entire
containing block to be marked `unsafe`, as shown throughout
[pre-csharp7-arrays-and-pointers.md](pre-csharp7-arrays-and-pointers.md) and
[csharp7.2-ref-struct-and-span.md](csharp7.2-ref-struct-and-span.md). This is the stable, current
behavior for all production code today — the updated model in this file is forward-looking only.
