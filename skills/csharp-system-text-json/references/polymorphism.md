# Polymorphic serialization

By default, serializing a value through a base-type reference (`Serialize<Base>(derivedInstance)`,
or a property typed as `Base` on a containing object) emits **only the members declared on
`Base`** — derived-only members are silently dropped, and there is no type discriminator in the
JSON, so deserialization back into the correct derived type isn't possible without extra work.
`[JsonPolymorphic]`/`[JsonDerivedType]` (available since .NET 7) solve both problems by having the
serializer emit and read a type-discriminator property.

## Declaring a hierarchy

```csharp
using System.Text.Json.Serialization;

[JsonPolymorphic]
[JsonDerivedType(typeof(Circle), typeDiscriminator: "circle")]
[JsonDerivedType(typeof(Rectangle), typeDiscriminator: "rectangle")]
public abstract class Shape
{
    public string Color { get; set; } = "black";
}

public sealed class Circle : Shape
{
    public double Radius { get; set; }
}

public sealed class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }
}
```

```csharp
Shape shape = new Circle { Color = "red", Radius = 2.5 };
string json = JsonSerializer.Serialize(shape); // uses the Shape's declared/runtime resolution
// {"$type":"circle","Color":"red","Radius":2.5}

Shape back = JsonSerializer.Deserialize<Shape>(json)!; // back is a Circle instance
```

- Every type that should be reachable during deserialization needs its own
  `[JsonDerivedType(typeof(T), typeDiscriminator: "...")]` entry on the base type — the discriminator
  set is closed and explicit, not inferred from reflection over the assembly.
- Serializing/deserializing directly against a **derived** type (`Serialize<Circle>(circle)`, not
  through the `Shape`-typed reference) bypasses the discriminator machinery entirely and behaves
  like ordinary non-polymorphic serialization for that call.

## `[JsonPolymorphic]` options

```csharp
[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "$shapeType",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
    IgnoreUnrecognizedTypeDiscriminators = false)]
```

- `TypeDiscriminatorPropertyName` — default `"$type"`; override to avoid colliding with a real
  `type` property in your payload, or to match an existing wire format's discriminator field name.
- `UnknownDerivedTypeHandling` — behavior when serializing a runtime type not covered by any
  `[JsonDerivedType]`: `FailSerialization` (throw — the default) or `FallBackToNearestAncestor`
  (walk up the type hierarchy to the closest type that *is* registered, including the base type
  itself).
- `IgnoreUnrecognizedTypeDiscriminators` — when `true`, an incoming discriminator value with no
  matching `[JsonDerivedType]` deserializes as the base type instead of throwing `JsonException`.
  Default `false` (throw), which is usually the safer choice — a silent fallback can mask a
  producer/consumer contract drift.

## Discriminator type

The `typeDiscriminator` argument accepts either a `string` (as above) or an `int` — useful for
compact wire formats or when mirroring an existing integer-coded discriminator from another
system. All derived types under one base must use the same discriminator kind (all `string` or
all `int`).

## Interaction with source generation

Polymorphic hierarchies work with source-generated contexts (`source-generation.md`), but the base
type and each `[JsonDerivedType]` that's serialized/deserialized directly still need their own
`[JsonSerializable]` registration on the `JsonSerializerContext` for the generator to produce
metadata for them.

## When not to reach for this

For a simple "one of a small closed set of shapes" scenario where you control both producer and
consumer and don't need automatic type resolution, a plain `enum` discriminator property plus a
custom `JsonConverter<T>` (see `custom-converters.md`) that switches on it is sometimes more
explicit and easier to evolve than `[JsonPolymorphic]`/`[JsonDerivedType]`, particularly if the
discriminator needs to live in a specific position in the JSON or follow rules `[JsonPolymorphic]`
doesn't support (e.g. a value that also encodes a schema version).
