# Source Generators as a Reflection-Free Alternative

Reflection resolves a type's shape at *runtime*, every time, by walking metadata — flexible, but
never free, and hostile to trimming and Native AOT (a trimmer can't prove which members a
`GetMethod("Validate")` string-lookup will ask for at runtime, so it either keeps everything or
breaks). Roslyn source generators move that same shape-discovery to *compile time*: they inspect
your code during compilation and emit ordinary C# that a trimmer and AOT compiler can see and keep
exactly what's used, with none of reflection's per-call cost. This file is about when to reach for
a source generator instead of reflection, not about writing one from scratch — the examples below
use generators shipped in the BCL as the running illustration of the tradeoff.

## The generator lineage, verified by version

- **C# 9 / .NET 5, November 2020** — `ISourceGenerator`, the original source-generator API. A
  generator re-runs its full analysis on every keystroke-triggered compilation, which is fine for
  small generators but causes IDE lag on larger ones.
- **C# 10 / .NET 6, November 2021** — `IIncrementalGenerator`, a pipeline-based replacement that
  caches intermediate results and only re-executes the stages actually affected by an edit.
  Microsoft's current guidance is to write new generators against `IIncrementalGenerator`, not
  `ISourceGenerator`.
- **.NET 6, November 2021** — `System.Text.Json`'s source-generation mode debuts
  (`[JsonSerializable]`, a `JsonSerializerContext` partial class), generating serialization code at
  compile time instead of discovering a type's properties via reflection at runtime.
- **.NET 7, November 2022 / C# 11** — `[GeneratedRegex]` ships alongside .NET 7's `System.Text.Json`
  improvements: a source-generated `Regex` subclass emitted at build time, replacing
  `RegexOptions.Compiled`'s runtime IL-emit cost (and reflection-emit cost) with zero runtime
  compilation cost.
- **.NET 8, November 2023** — `System.Text.Json` source generation closes most of the remaining
  functional gap with reflection-based serialization, and .NET 8's own AOT-compiled components
  lean on it as their default serialization path rather than reflection.

## Basic: reflection-based vs. source-generated JSON serialization

```csharp
// Reflection-based (works on every tier in this skill; System.Text.Json's default mode):
Order order = JsonSerializer.Deserialize<Order>(json)!;
```

```csharp
// Source-generated (.NET 6+): a partial context class the generator fills in at compile time.
[JsonSerializable(typeof(Order))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

```csharp
Order order = JsonSerializer.Deserialize(json, AppJsonContext.Default.Order)!;
```

Both calls produce the same `Order`. The reflection-based overload discovers `Order`'s
properties via `GetProperties()`/`PropertyInfo.SetValue` the first time it serializes that type
(and caches the result internally); the source-generated overload has no discovery step at
runtime at all — `AppJsonContext.Default.Order` is a compile-time-known, AOT- and trim-safe
serializer the generator wrote for you.

## Advanced: when reflection is still the right tool

Source generation only helps when the *set of types* is known at compile time. It cannot replace
reflection for:

- **Plugin/extension loading**, where the concrete type isn't known until an assembly is loaded at
  runtime — there's nothing for a generator to see at compile time.
- **Generic code operating over an unbounded, caller-supplied `Type`** — `MakeGenericType`
  (see [csharp2-generics-reflection.md](../references/csharp2-generics-reflection.md)) constructing
  a closed generic from a `Type` obtained via configuration, user input, or dynamic loading.
- **One-off diagnostic/debugging tooling**, where compile-time codegen infrastructure is more setup
  cost than the problem justifies.

A related, narrower BCL alternative worth knowing about even though it's not a source generator:
**`[UnsafeAccessor]`, .NET 8, November 2023** — a zero-overhead, AOT-friendly way to call a private
member whose declaring type *and* member name are both known at compile time, generated as a
direct IL call instead of a reflection-emit trampoline. Like source generation, it only works when
the target is fixed at compile time; when the member to access is itself runtime-determined,
`MethodInfo`/`FieldInfo` reflection (from
[csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md)) is still
the only tool that applies.

```csharp
public partial class OrderAccessor
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_lineItems")]
    private static extern ref List<LineItem> GetLineItems(Order order);
}
```

```csharp
ref List<LineItem> lineItems = ref OrderAccessor.GetLineItems(order);
```

Compare this to the reflection form of the same access
(`typeof(Order).GetField("_lineItems", BindingFlags.NonPublic | BindingFlags.Instance)` then
`GetValue`/`SetValue`) — `[UnsafeAccessor]` needs the field's name as a compile-time string literal
(so it's just as rename-fragile as an un-`nameof`'d reflection call; see
[csharp6-nameof-reflection-safe-names.md](../references/csharp6-nameof-reflection-safe-names.md)
for the general string-literal-fragility problem), but pays none of reflection's per-call
invocation cost and works under full trimming/AOT, where field-name reflection may be trimmed
away entirely.

## Fallback

Source generators need the C# 9 compiler (.NET 5 SDK) at build time at minimum;
`IIncrementalGenerator` needs the C# 10 / .NET 6 SDK. Below either, or for any scenario in the
"reflection is still the right tool" list above, use the reflection forms from
[csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md) and
[csharp2-generics-reflection.md](../references/csharp2-generics-reflection.md) directly — every
source-generated example in this file has a working reflection-based equivalent shown alongside
it.
