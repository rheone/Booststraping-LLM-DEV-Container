# YamlDotNet

Guidance on YamlDotNet, the YAML parsing/emitting library for .NET — the routing table (by task,
not YamlDotNet version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per YamlDotNet version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `SerializerBuilder`/`DeserializerBuilder`, `Serialize`/`Deserialize<T>()` |
| `naming-conventions.md` | `WithNamingConvention`, `CamelCaseNamingConvention`, `PascalCaseNamingConvention`, `HyphenatedNamingConvention`, `UnderscoredNamingConvention` |
| `custom-type-converters.md` | `IYamlTypeConverter`, registering with `WithTypeConverter` |
| `anchors-aliases-and-multi-document.md` | `&anchor`/`*alias` reuse, merge keys, `Deserializer.Deserialize` over multiple documents in one stream |
| `typed-vs-dynamic-deserialization.md` | Strongly-typed POCOs vs. `YamlNode`/`YamlMappingNode`/`YamlStream`, and `dynamic` deserialization |
| `testing.md` | Testing round-trip serialization and custom converters |

## Scope

YamlDotNet's high-level `Serializer`/`Deserializer` object-model API. Out of scope: strict YAML
1.1/1.2 spec-version enforcement beyond default parser behavior, and programmatic YAML-to-JSON
object-model conversion.

Each reference file notes a version-sensitive fact inline where one exists; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).
