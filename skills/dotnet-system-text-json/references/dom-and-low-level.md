# DOM manipulation and low-level reading/writing

Three tiers below the top-level `JsonSerializer.Serialize`/`Deserialize` calls, for when you don't
have (or don't want) a static C# type to deserialize into, or need direct control over the byte
stream.

## `JsonDocument` / `JsonElement` — readonly, pooled, fast

```csharp
using JsonDocument doc = JsonDocument.Parse(jsonString);
JsonElement root = doc.RootElement;

string name = root.GetProperty("name").GetString()!;
int age = root.GetProperty("age").GetInt32();

if (root.TryGetProperty("nickname", out JsonElement nickname) && nickname.ValueKind != JsonValueKind.Null)
{
    Console.WriteLine(nickname.GetString());
}

foreach (JsonElement item in root.GetProperty("tags").EnumerateArray())
{
    Console.WriteLine(item.GetString());
}
```

- `JsonDocument` parses the entire JSON text into an efficient, **read-only** in-memory
  representation backed by pooled buffers. `JsonElement` is a lightweight struct view over that
  buffer.
- `JsonDocument` implements `IDisposable` — **always dispose it** (`using`) to return its rented
  buffers to the pool; failing to dispose doesn't corrupt anything but does defeat the pooling and
  increase GC pressure.
- `JsonElement` values obtained from a `JsonDocument` are only valid **while the parent
  `JsonDocument` is alive** — accessing one after disposal throws `ObjectDisposedException`. Call
  `JsonElement.Clone()` if you need an element to outlive its source document.
- Best fit: read-heavy, parse-once, inspect-and-discard scenarios (inspecting one field of a large
  payload, routing on a discriminator before deciding which type to deserialize into) where you
  never need to mutate the structure.
- `JsonSerializer.Deserialize<JsonElement>(...)` (or a property typed `JsonElement` on an
  otherwise-typed class) captures a sub-tree as raw, still-structured JSON without committing to a
  shape for it — a common pattern for "mostly-typed payload with one open-ended extension field."

## `JsonNode` (and `JsonObject`/`JsonArray`/`JsonValue`) — mutable, LINQ-to-JSON style

```csharp
using System.Text.Json.Nodes;

JsonNode? node = JsonNode.Parse(jsonString);
node!["age"] = (int?)node["age"] + 1; // mutate in place
node["tags"]!.AsArray().Add("new-tag");

JsonObject obj = new()
{
    ["name"] = "Ada",
    ["scores"] = new JsonArray(90, 85, 100),
};

string json = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
```

- `JsonNode` is a **mutable** DOM — build up, edit, or graft together JSON structures
  programmatically, closer to Newtonsoft.Json's `JToken`/`JObject` model than `JsonElement` is.
- Not pooled and generally allocates more than `JsonDocument`/`JsonElement` — the right tradeoff
  when you need to *construct or modify* JSON, not just read it.
- `JsonNode.Parse`/`JsonObject`/`JsonArray`/`JsonValue` don't require an enclosing `using` — no
  pooled-buffer lifetime to manage, unlike `JsonDocument`.
- Interop with typed objects: `JsonSerializer.SerializeToNode(myObject)` and
  `node.Deserialize<MyType>()` convert between a `JsonNode` tree and a strongly-typed object.

## Choosing between them

| Need | Use |
| --- | --- |
| Parse once, read a few fields, discard | `JsonDocument`/`JsonElement` |
| Build or mutate JSON programmatically | `JsonNode` family |
| A known static C# type exists | `JsonSerializer.Serialize`/`Deserialize<T>` — skip the DOM entirely |
| Maximum throughput, full manual control | `Utf8JsonReader`/`Utf8JsonWriter` (below) |

## `Utf8JsonWriter` — low-level, high-performance writing

```csharp
using var stream = new MemoryStream();
using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
{
    writer.WriteStartObject();
    writer.WriteString("name", "Ada");
    writer.WriteNumber("age", 36);
    writer.WriteStartArray("tags");
    writer.WriteStringValue("pioneer");
    writer.WriteEndArray();
    writer.WriteEndObject();
} // disposing/Flush writes any buffered bytes
```

Writes UTF-8 JSON bytes directly to a `Stream`, an `IBufferWriter<byte>`, or an internal buffer,
without ever materializing a JSON `string`. This is what `JsonSerializer` itself uses internally,
and what a custom `JsonConverter<T>.Write` implementation is handed — see `custom-converters.md`.
Reach for it directly (bypassing `JsonSerializer` and any C# object model entirely) only in
throughput-critical paths that are constructing JSON from data that's naturally already in
hand piece-by-piece (e.g. streaming a large result set to a response body row-by-row).

## `Utf8JsonReader` — low-level, high-performance reading

```csharp
var reader = new Utf8JsonReader(utf8Bytes);
while (reader.Read())
{
    switch (reader.TokenType)
    {
        case JsonTokenType.PropertyName when reader.GetString() == "age":
            reader.Read();
            int age = reader.GetInt32();
            break;
    }
}
```

`Utf8JsonReader` is a **forward-only, low-allocation `ref struct` token reader** over a UTF-8
byte span — no tree is built, no allocation per token. Because it's a `ref struct`, it cannot be
stored as a field, cannot be used across `await` boundaries, and cannot be captured in a closure or
iterator/async method — a constraint that also applies to any converter's `Read` override (see
`custom-converters.md`), which receives one by `ref`. For chunked input (e.g. reading from a
`Stream` in pieces rather than one complete byte buffer), `Utf8JsonReader` supports a
partial-read/resume protocol via its constructor overload taking previous reader state — a detail
that matters when hand-rolling a converter over a `PipeReader`, but rarely comes up otherwise.

Reach for `Utf8JsonReader` directly (rather than through `JsonSerializer`/`JsonDocument`) only for
custom converters and genuinely hot, allocation-sensitive parsing paths — for ordinary application
code, `JsonSerializer.Deserialize<T>` or `JsonDocument.Parse` is simpler and, for typical payload
sizes, plenty fast.
