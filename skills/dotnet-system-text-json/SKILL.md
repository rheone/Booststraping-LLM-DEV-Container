---
name: dotnet-system-text-json
description: Guidance on System.Text.Json for C#/.NET — JsonSerializer.Serialize/Deserialize and their stream/async forms, JsonSerializerOptions, property-shaping attributes (JsonPropertyName, JsonIgnore, JsonInclude, JsonConstructor, JsonPropertyOrder, JsonNumberHandling, JsonConverter), naming policies (camelCase, kebab-case, snake_case, custom), writing custom JsonConverter<T>/JsonConverterFactory implementations, source-generated serialization (JsonSerializerContext, [JsonSerializable], AOT/trimming/startup-perf tradeoffs vs reflection mode), polymorphic serialization ([JsonPolymorphic]/[JsonDerivedType]), type-specific handling (records, init-only, required members, DateOnly/TimeOnly, enums, non-string-keyed dictionaries), JsonDocument/JsonElement/JsonNode DOM manipulation, low-level Utf8JsonReader/Utf8JsonWriter, common pitfalls (case sensitivity, circular references, ReferenceHandler.Preserve), and testing patterns for converters and source-gen contexts. Use when serializing/deserializing JSON in C#, choosing between reflection-based and source-generated System.Text.Json, writing a custom converter, debugging unexpected JSON shape/casing, or handling polymorphic/derived-type JSON payloads.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# System.Text.Json

Guidance on `System.Text.Json` (verified current release: **10.0.12**, shipping in lockstep with
**.NET 10** — see [README.md](README.md) for full version detail). Organized by task,
not by .NET version: this library's surface has been stable in shape since .NET 6, with additive
features layered on in later releases (each noted inline with its version-introduced fact).

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Basic serialize/deserialize, sync vs. stream/async, `JsonSerializerOptions` | `JsonSerializer.Serialize`/`Deserialize`, `SerializeAsync`/`DeserializeAsync` | [references/core-serialization.md](references/core-serialization.md) |
| Shaping how one type's properties serialize | `JsonPropertyName`, `JsonIgnore`, `JsonInclude`, `JsonConstructor`, `JsonPropertyOrder`, `JsonNumberHandling`, `[JsonConverter]` | [references/attributes.md](references/attributes.md) |
| Changing property-name casing project-wide or per-type | `JsonNamingPolicy` (CamelCase, KebabCase, SnakeCase), custom naming policies | [references/naming-policies.md](references/naming-policies.md) |
| A type needs bespoke read/write logic the attributes can't express | `JsonConverter<T>`, `JsonConverterFactory` for open-generic types | [references/custom-converters.md](references/custom-converters.md) |
| AOT/trimming, faster startup, or avoiding reflection | `JsonSerializerContext`, `[JsonSerializable]`, `[JsonSourceGenerationOptions]` | [references/source-generation.md](references/source-generation.md) |
| Serializing a type hierarchy (base + derived types) | `[JsonPolymorphic]`, `[JsonDerivedType]` | [references/polymorphism.md](references/polymorphism.md) |
| Records, `required` members, `DateOnly`/`TimeOnly`, enums, non-`string`-keyed dictionaries | type-specific serialization behavior | [references/type-handling.md](references/type-handling.md) |
| No static type available — dynamic/DOM-style JSON work | `JsonDocument`/`JsonElement` (readonly, pooled) vs. `JsonNode` (mutable, LINQ-to-JSON) | [references/dom-and-low-level.md](references/dom-and-low-level.md) |
| Same file: hand-rolled high-performance read/write | `Utf8JsonReader`, `Utf8JsonWriter` | [references/dom-and-low-level.md](references/dom-and-low-level.md) |
| Debugging an unexpected shape, a `PascalCase` leak, a circular-reference exception | case sensitivity, `ReferenceHandler.Preserve`, large-payload perf, Newtonsoft behavioral deltas | [references/pitfalls.md](references/pitfalls.md) |
| Testing a converter, snapshotting JSON output, verifying a source-gen context | isolated converter tests, golden-file/snapshot testing, round-trip patterns | [references/testing.md](references/testing.md) |

## Quick start

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

// Reflection-based (default): fine for most apps, not for Native AOT.
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

string json = JsonSerializer.Serialize(new Person("Ada", 36), options);
Person? person = JsonSerializer.Deserialize<Person>(json, options);

// Stream/async: use for HTTP bodies, files — avoids buffering the whole string.
await using var stream = File.Create("person.json");
await JsonSerializer.SerializeAsync(stream, person, options);

record Person(string Name, int Age);
```

For AOT/trimming-safe or startup-sensitive apps, pair a `[JsonSerializable]`-decorated
`JsonSerializerContext` with `options.TypeInfoResolver` instead of relying on reflection — see
[references/source-generation.md](references/source-generation.md).

## Out of scope

- General C# language features unrelated to JSON (generics, records, source generators as a
  language mechanism) — this skill covers only how `System.Text.Json` uses them, inlined where
  relevant to a specific behavior.
- Other serialization formats (XML, protobuf, MessagePack) and other JSON libraries
  (Newtonsoft.Json) — mentioned only in [references/pitfalls.md](references/pitfalls.md) where a
  concrete behavioral difference from `System.Text.Json` matters, never as a migration guide.
- ASP.NET Core's JSON integration (`AddJsonOptions`, `[FromBody]` binding conventions) — that's
  framework configuration layered on top of `System.Text.Json`, not the library itself.
