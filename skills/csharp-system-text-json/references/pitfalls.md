# Common pitfalls

## Deserialization property matching is case-sensitive by default

`JsonSerializerOptions.PropertyNameCaseInsensitive` defaults to `false`. If your JSON uses
`camelCase` (typical for JS-produced payloads) but your C# type's members are plain `PascalCase`
with no naming policy configured, deserialization silently leaves those properties at their
default values instead of throwing — there is no error, just quietly-unpopulated data, which makes
this a common source of "why is everything null/zero" bugs.

```csharp
// JSON: {"name":"Ada","age":36}
public class Person { public string Name { get; set; } = ""; public int Age { get; set; } }

var p = JsonSerializer.Deserialize<Person>(json); // Name == "", Age == 0 — NOT an exception
```

Fix by setting `PropertyNameCaseInsensitive = true`, or by using a matching
`PropertyNamingPolicy` (see `naming-policies.md`), or by using the `JsonSerializerOptions.Web`
preset which sets both camelCase naming and case-insensitive matching together.

## Enums serialize as numbers by default

`ProductStatus.Active` (value `1`) serializes to JSON as `1`, not `"Active"`, unless
`JsonStringEnumConverter`/`JsonStringEnumConverter<TEnum>` is registered (see
`type-handling.md`). This differs from some other ecosystems' defaults and is a frequent surprise
when a consumer expects a human-readable string.

## Circular references throw by default

```csharp
public class Node { public Node? Parent { get; set; } public List<Node> Children { get; } = new(); }
```

Serializing a graph with a cycle (a child pointing back to its parent, etc.) throws
`JsonException: A possible object cycle was detected` by default — `System.Text.Json` does not
silently truncate or infinitely recurse. Two ways to handle it:

- `ReferenceHandler.Preserve` (`JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve`)
  emits `$id`/`$ref` metadata properties so the graph (including the cycle) round-trips faithfully.
  This changes the JSON shape — every object gains an `$id`, and repeated references become
  `{"$ref":"..."}` — so it's only appropriate when the consumer of the JSON also understands this
  convention (typically: another `System.Text.Json` consumer, or a same-team API where you control
  both ends).
- For a payload consumed by an arbitrary/external client that doesn't know about `$id`/`$ref`,
  restructure instead: don't serialize the back-reference at all (`[JsonIgnore]` the `Parent`
  property), or serialize a DTO/projection that doesn't carry the cycle.

## Large payload performance

- Prefer the `Stream`-based `SerializeAsync`/`DeserializeAsync` overloads over
  `Serialize`/`Deserialize` against a `string` for large payloads — the string-based overloads
  must materialize the entire JSON text as one allocation before/after processing it, while the
  stream overloads process incrementally.
- Reuse a single `JsonSerializerOptions` instance (see `core-serialization.md`) — constructing a
  new one per call defeats the serializer's per-options metadata cache and adds real overhead at
  volume.
- For genuinely hot paths (high request volume, large objects, measured via profiling — not
  speculatively), source-generated contexts (`source-generation.md`) remove reflection-based
  metadata construction and, in `Serialization` generation mode, generate direct non-reflective
  write code.
- `JsonDocument`'s pooled-buffer model (`dom-and-low-level.md`) makes repeated parse/dispose
  cycles cheaper than repeatedly allocating fresh structures — but only if every `JsonDocument` is
  actually disposed; a leaked one still holds its rented buffer.

## Comparison notes vs. Newtonsoft.Json (only where behavior actually differs)

- **Fields**: Newtonsoft.Json serializes public fields by default; `System.Text.Json` does not
  (needs `[JsonInclude]` — see `attributes.md`).
- **Case sensitivity**: Newtonsoft.Json matches property names case-insensitively by default;
  `System.Text.Json` is case-sensitive by default (see above).
- **Enums**: Newtonsoft.Json's default enum handling is also numeric, so this one is *not* a
  behavioral difference — a common misconception is that Newtonsoft "does the right thing" here
  by default; it requires its own `StringEnumConverter` opt-in too.
- **Circular references**: Newtonsoft.Json's default `ReferenceLoopHandling.Error` also throws by
  default (matching `System.Text.Json`'s default), but Newtonsoft additionally offers
  `Ignore`/`Serialize` loop-handling modes with different tradeoffs than `ReferenceHandler.Preserve`'s
  `$id`/`$ref` metadata approach.
- **Async**: Newtonsoft.Json's "async" APIs wrap synchronous work in `Task`-returning signatures
  without truly asynchronous I/O under the hood; `System.Text.Json`'s `SerializeAsync`/
  `DeserializeAsync` perform genuine incremental asynchronous I/O against the underlying `Stream`.

This list exists only to flag concrete, factual behavioral deltas that cause bugs when porting
code or expectations from one library to the other — it is not a migration guide, and doesn't
cover configuration-equivalent differences (attribute names, options names) that don't change
behavior.
