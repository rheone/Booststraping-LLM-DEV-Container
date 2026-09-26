# Source-generated serialization

By default, `System.Text.Json` builds its per-type serialization metadata (which properties
exist, their converters, constructors, etc.) via **reflection**, at run time, the first time each
type is used. Since .NET 6, a **Roslyn source generator** can instead emit that metadata as
compiled C# at build time. Both modes go through the same `JsonSerializer` entry points; source
generation only changes how the type metadata is produced and looked up.

## Why source generation exists

- **Native AOT / trimming compatibility.** Reflection-based serialization inspects types and
  invokes members dynamically at run time; a trimmer can't statically prove which members a
  reflection-based call will touch, so it either has to keep everything (defeating trimming) or
  risk removing something the serializer needs at run time (`NotSupportedException` at run time,
  or a trim-analysis warning at publish time). Source-generated metadata is ordinary compiled code
  that the trimmer/AOT compiler can see and analyze statically like any other call site.
- **Startup performance.** Reflection-based mode builds metadata lazily on first use per type,
  which costs real time on a cold path (a serverless cold start, a CLI tool's first request).
  Source-generated metadata is already compiled, removing that first-use cost.
- **Serialization throughput**, in `Serialization`-mode generation specifically (see below), by
  emitting direct, non-reflective read/write code for a fixed set of options.

Reflection-based mode remains the simpler default for typical server apps that aren't
trim/AOT-published and aren't startup-sensitive — source generation is an opt-in tradeoff (a build
step, a partial class to maintain) for when one of the above actually matters.

## Declaring a context

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Order>))]
[JsonSerializable(typeof(OrderStatus))]
public partial class AppJsonContext : JsonSerializerContext
{
}
```

- The class must be `partial` — the generator emits the other part.
- Each `[JsonSerializable(typeof(T))]` registers `T` (and, transitively, its own property types)
  for generated metadata. Collection/generic wrapper types (`List<Order>`, `Order[]`,
  `Dictionary<string, Order>`) need their **own** `[JsonSerializable]` entry if you serialize that
  exact shape directly — the generator does not automatically add every collection shape you might
  deserialize into, only the ones named.
- The generated context exposes a static `Default` instance (`AppJsonContext.Default`) plus a
  strongly-typed `JsonTypeInfo<T>` property per registered type (e.g. `AppJsonContext.Default.Order`).

## Using it

```csharp
string json = JsonSerializer.Serialize(order, AppJsonContext.Default.Order);
Order? order2 = JsonSerializer.Deserialize(json, AppJsonContext.Default.Order);

// Or wire it into JsonSerializerOptions so ordinary Serialize<T>/Deserialize<T> calls use it:
var options = new JsonSerializerOptions
{
    TypeInfoResolver = AppJsonContext.Default,
};
string json2 = JsonSerializer.Serialize(order, options);
```

To combine generated metadata with the reflection-based resolver for types you didn't register
(e.g. a third-party type you don't control and haven't listed), chain resolvers:

```csharp
var options = new JsonSerializerOptions
{
    TypeInfoResolver = JsonTypeInfoResolver.Combine(
        AppJsonContext.Default,
        new DefaultJsonTypeInfoResolver()), // falls back to reflection for unregistered types
};
```

Combining resolvers reintroduces reflection (and its AOT/trimming caveats) for whatever falls
through to the reflection-based fallback — for a fully AOT-safe app, every serialized type must be
covered by a generated context instead.

## `[JsonSourceGenerationOptions]`

Configures generation-wide behavior, applied to the context class itself:

```csharp
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Order))]
public partial class AppJsonContext : JsonSerializerContext
{
}
```

Mirrors most of `JsonSerializerOptions`' properties (naming policy, ignore conditions, number
handling, etc.) as attribute properties, since the generator needs these decisions available at
compile time rather than as a runtime `JsonSerializerOptions` instance. Note `PropertyNamingPolicy`
here takes a `JsonKnownNamingPolicy` enum value, not a `JsonNamingPolicy` instance (an attribute
argument must be a compile-time constant, so it can't reference a policy object) — a fully custom
`JsonNamingPolicy` subclass isn't usable from source-generation attributes for this reason.

`GenerationMode` (`JsonSourceGenerationMode`):

- `Metadata` — generates type metadata (property lists, converters, constructors) consumed by the
  normal `JsonSerializer` read/write engine. Supports the full serialization/deserialization
  feature set. This is the mode that makes an app AOT/trim-safe.
- `Serialization` — additionally generates specialized, non-reflective **serialize**-path code
  optimized for the exact options configured, trading generated-code size and some option
  flexibility for faster serialization. Not all options are supported in this mode, and it does
  not by itself provide optimized **deserialization** — combine with `Metadata` (the default,
  `Metadata | Serialization` when omitted) rather than using `Serialization` alone unless you're
  certain deserialization isn't needed through that context.
- Default (when `GenerationMode` isn't set) generates both — the safe, general-purpose choice.

## Multiple contexts

Large type graphs are sometimes split across multiple `JsonSerializerContext` classes (e.g. one
per feature area) to keep individual generated files manageable; combine them the same way as
combining with the reflection-based resolver, via `JsonTypeInfoResolver.Combine(...)`.

## Caveat: polymorphism and converters

`[JsonPolymorphic]`/`[JsonDerivedType]`-annotated hierarchies (see `polymorphism.md`) and custom
`[JsonConverter]`-attributed types are both supported in source-generated contexts — the base type
and every `[JsonDerivedType]` still each need their own `[JsonSerializable]` entry on the context
if serialized/deserialized directly (not only reachable as a nested property) for the generator to
emit metadata for them.
