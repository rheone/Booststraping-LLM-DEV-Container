# System.Text.Json

This skill covers `System.Text.Json` for C#/.NET: serializing and deserializing JSON, shaping the
output with attributes and naming policies, writing custom converters, source-generated
serialization, and handling polymorphic and DOM-based JSON.

## When to reach for it

- Serializing or deserializing JSON and getting unexpected casing, missing properties, or an
  unexpected shape back.
- Choosing between reflection-based serialization and source-generated serialization for
  AOT/trimming or startup-performance reasons.
- Writing a custom `JsonConverter<T>` for a type the default serializer can't handle.
- Serializing or deserializing a polymorphic hierarchy with `[JsonPolymorphic]`/`[JsonDerivedType]`.
- Working with `JsonDocument`/`JsonElement`/`JsonNode` or the low-level `Utf8JsonReader`/
  `Utf8JsonWriter` APIs directly.

## Using it

This skill fires automatically when your request involves JSON serialization in C#. You can also
invoke it directly with `/dotnet-system-text-json`.

## What it covers

| Topic | Reference |
| --- | --- |
| `JsonSerializer.Serialize`/`Deserialize`, `JsonSerializerOptions`, sync vs. stream/async | [references/core-serialization.md](references/core-serialization.md) |
| `JsonPropertyName`, `JsonIgnore`, `JsonInclude`, `JsonConstructor`, `JsonNumberHandling` | [references/attributes.md](references/attributes.md) |
| Naming policies: camelCase, kebab-case, snake_case, custom | [references/naming-policies.md](references/naming-policies.md) |
| Writing `JsonConverter<T>`/`JsonConverterFactory` implementations | [references/custom-converters.md](references/custom-converters.md) |
| `JsonSerializerContext`, `[JsonSerializable]`, reflection vs. source-gen tradeoffs | [references/source-generation.md](references/source-generation.md) |
| `[JsonPolymorphic]`/`[JsonDerivedType]` polymorphic serialization | [references/polymorphism.md](references/polymorphism.md) |
| Records, init-only, required members, `DateOnly`/`TimeOnly`, enums, dictionaries | [references/type-handling.md](references/type-handling.md) |
| `JsonDocument`/`JsonElement`/`JsonNode`, `Utf8JsonReader`/`Utf8JsonWriter` | [references/dom-and-low-level.md](references/dom-and-low-level.md) |
| Case sensitivity, circular references, `ReferenceHandler.Preserve` | [references/pitfalls.md](references/pitfalls.md) |
| Testing custom converters and source-generated contexts | [references/testing.md](references/testing.md) |

## Example prompts

- "Deserialize this JSON into a record with `required` members and camelCase property names."
- "Should I switch this API's serialization to source-generated for AOT, and what do I lose?"
- "Write a custom converter for a type that serializes as a plain string instead of an object."
