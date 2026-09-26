# Core serialization

`System.Text.Json` is the built-in JSON serializer in the .NET base class library (namespace
`System.Text.Json`, serialization types under `System.Text.Json.Serialization`).

## Synchronous serialize/deserialize

```csharp
using System.Text.Json;

string json = JsonSerializer.Serialize(myObject);
string jsonTyped = JsonSerializer.Serialize<MyType>(myObject); // pins the static type used

MyType? obj = JsonSerializer.Deserialize<MyType>(json);
object? obj2 = JsonSerializer.Deserialize(json, typeof(MyType)); // when the type is only known at runtime
```

- `Serialize(value)` uses `value`'s **runtime type** via `value.GetType()` when called on an
  `object`-typed overload; the generic `Serialize<T>(value)` overload uses the **compile-time**
  type `T` instead. This matters for polymorphism: `Serialize<Base>(derivedInstance)` serializes
  only `Base`'s members unless the type is annotated for polymorphic serialization (see
  `polymorphism.md`) or you pass `derivedInstance.GetType()` explicitly.
- `Deserialize<T>` returns `T?` — a `null` return is legitimate (JSON literal `null`), distinct
  from a failed parse, which throws `JsonException`.

## Stream and async APIs

```csharp
await using FileStream stream = File.OpenRead("data.json");
MyType? obj = await JsonSerializer.DeserializeAsync<MyType>(stream, cancellationToken: ct);

await using FileStream outStream = File.Create("out.json");
await JsonSerializer.SerializeAsync(outStream, obj, cancellationToken: ct);
```

- Use the stream/async overloads (`SerializeAsync`/`DeserializeAsync`) for HTTP request/response
  bodies, files, or any I/O-backed source — they read/write incrementally against a `Stream`
  instead of materializing the entire JSON text as one `string` first, which matters for large
  payloads and for not blocking a thread on I/O.
- `JsonSerializer.DeserializeAsyncEnumerable<T>` deserializes a top-level JSON array as an
  `IAsyncEnumerable<T>`, yielding each element as it's read rather than waiting for the whole
  array — useful for streaming large arrays without buffering them all in memory.
- The synchronous `Serialize`/`Deserialize` overloads also accept a `Utf8JsonWriter`/
  `Utf8JsonReader`/`ReadOnlySpan<byte>`/`ReadOnlySpan<char>` directly, for callers who already have
  UTF-8 bytes and want to avoid a `string` allocation entirely.

## `JsonSerializerOptions`

A `JsonSerializerOptions` instance controls casing, formatting, null handling, converters, and
more. It is expensive to construct (the serializer caches metadata keyed by the options instance)
so **create one instance and reuse it** — a static `readonly` field or DI singleton — rather than
constructing a fresh one per call.

```csharp
private static readonly JsonSerializerOptions Options = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
};
```

Frequently used members:

| Member | Default | Effect |
| --- | --- | --- |
| `PropertyNameCaseInsensitive` | `false` | Whether property-name matching during deserialization ignores case. See `pitfalls.md`. |
| `PropertyNamingPolicy` | `null` (exact C# member name) | Naming policy applied to output/matched property names. See `naming-policies.md`. |
| `WriteIndented` | `false` | Pretty-prints output with newlines/indentation — for readability (logs, debugging), not for compact wire payloads. |
| `DefaultIgnoreCondition` | `Never` | When to omit a property from output (`WhenWritingNull`, `WhenWritingDefault`). Global counterpart to the per-property `[JsonIgnore]` attribute. |
| `NumberHandling` | `Strict` | Global counterpart to `[JsonNumberHandling]` — e.g. `AllowReadingFromString` accepts `"42"` for an `int` property. |
| `ReferenceHandler` | `null` (throw on cycles) | `ReferenceHandler.Preserve` emits `$id`/`$ref` for repeated/circular references. See `pitfalls.md`. |
| `Converters` | empty | Ordered list of custom `JsonConverter` instances; first matching converter wins. |
| `TypeInfoResolver` / `TypeInfoResolverChain` | reflection-based resolver | Set to a generated `JsonSerializerContext` (or chain one with the default resolver) to opt into source-generated metadata. See `source-generation.md`. |
| `MaxDepth` | 64 | Guards against stack overflow from deeply nested/malicious JSON; exceeding it throws `JsonException`. |
| `AllowTrailingCommas` | `false` | Whether a trailing comma before `}`/`]` is tolerated on read. |
| `ReadCommentHandling` | `Disallow` | Whether `//`/`/* */` comments in JSON are skipped or rejected on read (JSON itself has no comment syntax; this is a lenient-parsing accommodation). |

`JsonSerializerOptions` also has a small set of **built-in presets** —
`JsonSerializerOptions.Web` (camelCase naming, case-insensitive property matching — the same
defaults ASP.NET Core's minimal APIs use) is the one most worth knowing, so you don't hand-roll
those two settings for a typical web API surface.

## Exceptions to expect

- `JsonException` — malformed JSON, or a value that doesn't fit the target type (e.g. a JSON
  string where a number was expected, absent lenient `NumberHandling`).
- `NotSupportedException` — the type isn't serializable as configured (e.g. a type with no
  accessible constructor the serializer can use, or reflection-mode hitting a trimmed/AOT-removed
  member; see `source-generation.md`).
