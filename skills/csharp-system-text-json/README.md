# C# System.Text.Json

Guidance on `System.Text.Json` for C#/.NET — the routing table (by task, not by .NET version) is
in [SKILL.md](SKILL.md).

```text
references/                        one file per topic/concern
  core-serialization.md              JsonSerializer.Serialize/Deserialize, JsonSerializerOptions,
                                      sync vs. stream/async APIs
  attributes.md                      JsonPropertyName, JsonIgnore, JsonInclude, JsonConstructor,
                                      JsonPropertyOrder, JsonNumberHandling, [JsonConverter]
  naming-policies.md                 JsonNamingPolicy (CamelCase, KebabCase, SnakeCase), custom
                                      naming policies
  custom-converters.md               JsonConverter<T>, JsonConverterFactory for generic/open-
                                      generic types
  source-generation.md               JsonSerializerContext, [JsonSerializable],
                                      [JsonSourceGenerationOptions], reflection vs. source-gen
  polymorphism.md                    [JsonPolymorphic], [JsonDerivedType]
  type-handling.md                   nullable reference types, records/init-only, required
                                      members, DateTime/DateOnly/TimeOnly, enums, non-string-
                                      keyed dictionaries
  dom-and-low-level.md               JsonDocument/JsonElement vs. JsonNode, Utf8JsonWriter/
                                      Utf8JsonReader
  pitfalls.md                        case sensitivity, circular references, ReferenceHandler
                                      .Preserve, large-payload performance, Newtonsoft.Json
                                      behavioral deltas
  testing.md                         testing custom converters, snapshot/golden-file testing,
                                      testing source-generated contexts, round-trip patterns
```

## Scope

`System.Text.Json` itself: the serializer, its attributes, options, source-generation mode, the
DOM/low-level reader/writer types, and testing patterns for code built on top of it. Out of scope:
other serialization libraries (mentioned only for a concrete, factual behavioral contrast in
`pitfalls.md`) and ASP.NET Core's JSON configuration layer, which is framework glue rather than
part of the library.

Each reference file notes a feature's version-introduced fact inline; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).

## Verified version and licensing (as of 2026-09-25)

- **Latest `System.Text.Json` NuGet package: 10.0.12** (published 2026-09-08), matching the
  monthly-patch cadence of the **.NET 10** runtime it ships alongside.
- **.NET 10** is the current release; it shipped **2025-11-11** as a **Long Term Support (LTS)**
  release, supported until **2028-11-10**. .NET 10 includes C# 14.
- `System.Text.Json` is part of the shared framework in every .NET 10 app by default (no package
  reference needed when targeting `net10.0`+); the standalone NuGet package exists for consumers
  targeting older TFMs or .NET Standard/.NET Framework, or who want a newer minor version than
  their target framework ships.
- **License: MIT.** `System.Text.Json` is developed in the open-source
  [`dotnet/runtime`](https://github.com/dotnet/runtime) repository under the .NET Foundation /
  Microsoft, distributed under the MIT license — same license and repository as the rest of the
  modern .NET base class library.
