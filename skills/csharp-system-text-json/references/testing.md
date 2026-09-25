# Testing serialization code

Patterns for verifying custom converters, naming/options configuration, source-generated
contexts, and general serialize/deserialize behavior — framework-agnostic (examples below read as
plain assertions; adapt the assertion syntax to whichever test framework a project uses).

## Testing a custom `JsonConverter<T>` in isolation

Test the converter directly against `JsonSerializer.Serialize`/`Deserialize` with a minimal
`JsonSerializerOptions` containing just that converter — don't require the full application's
options/type graph to exercise one converter's logic.

```csharp
var options = new JsonSerializerOptions { Converters = { new ProductIdConverter() } };

// Write
string json = JsonSerializer.Serialize(new ProductId(42), options);
Assert.Equal("42", json);

// Read
ProductId id = JsonSerializer.Deserialize<ProductId>("42", options);
Assert.Equal(42, id.Value);

// Malformed input should throw JsonException, not some other exception type
Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProductId>("\"not-a-number\"", options));
```

For a converter with meaningfully different branches (null handling, malformed-token handling,
boundary values), enumerate those branches as separate cases rather than one broad
serialize-then-deserialize test — a round-trip test alone can pass even when `Read` and `Write`
share a symmetrical bug (e.g. both silently truncate a value the same way).

## Snapshot / golden-file testing of serialized output

Useful for locking down an API's exact wire shape (property order, casing, presence/absence of
optional fields) so an accidental shape change is caught in review rather than by a downstream
consumer.

```csharp
var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
string actual = JsonSerializer.Serialize(BuildSampleOrder(), options);
string expected = File.ReadAllText("Snapshots/order.json");
Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(actual));
```

- Use `WriteIndented = true` for the snapshot's own serialization pass — a stable, human-diffable
  golden file makes review of an intentional shape change straightforward, and an unintentional
  one obvious.
- Normalize line endings (`\r\n` vs `\n`) before comparing if the golden file and the test runner
  might disagree on them across platforms — this is a common source of a snapshot test that only
  fails in CI or only on Windows.
- Commit the golden file to source control like any other test fixture; update it deliberately
  (regenerate and re-review the diff) rather than programmatically overwriting it from a failing
  test run without inspection.
- For a dedicated snapshot-testing library integrated into the project's test framework, follow
  that library's own comparison/update workflow instead of hand-rolling file comparison — the
  pattern above is the underlying mechanism such libraries automate, useful when no such library is
  in play.

## Testing source-generated contexts

A source-generated `JsonSerializerContext` (see `source-generation.md`) is worth testing the same
way as any other serialization configuration — plus two failure modes specific to source
generation:

```csharp
// 1. Ordinary round-trip through the generated metadata, not the reflection-based default
string json = JsonSerializer.Serialize(order, AppJsonContext.Default.Order);
Order back = JsonSerializer.Deserialize(json, AppJsonContext.Default.Order)!;
Assert.Equal(order, back);

// 2. A type reachable only as a nested property, but also (de)serialized directly elsewhere,
//    needs its own [JsonSerializable] entry — a test that only exercises the top-level type
//    won't catch a missing entry for a type serialized independently in a different code path.
string statusJson = JsonSerializer.Serialize(OrderStatus.Shipped, AppJsonContext.Default.OrderStatus);
```

If a build configures both reflection-based and source-generated paths in different environments
(e.g. reflection in normal builds, source-gen only under Native AOT), run the same test cases
against both `JsonSerializerOptions` configurations and assert they produce identical output —
catches a case where generation-time options (`[JsonSourceGenerationOptions]`) have silently
drifted from the runtime `JsonSerializerOptions` used elsewhere.

## Round-trip patterns

The general shape for verifying a type serializes and deserializes back to an equal value:

```csharp
static void AssertRoundTrips<T>(T value, JsonSerializerOptions options)
{
    string json = JsonSerializer.Serialize(value, options);
    T? result = JsonSerializer.Deserialize<T>(json, options);
    Assert.Equal(value, result);
}
```

- This requires `T` to have a meaningful `Equals` — records get this for free (member-wise
  equality); for an ordinary mutable class, either implement `Equals` for the test or compare
  individual members explicitly instead of relying on reference equality (which will always fail
  after a round trip through a *new* instance).
- A round-trip test alone does **not** verify the wire shape is correct (property names, casing,
  presence of optional fields) — pair it with an explicit shape assertion (a snapshot test, or a
  targeted assertion on the intermediate JSON string/`JsonElement`) for anything with an external
  contract, since a symmetrical bug in both serialize and deserialize can pass a round-trip check
  while still producing the wrong wire format for a real consumer.
- Include at least one edge-case value per round-trip suite: `null`/default values on optional
  members, an empty collection vs. an absent one (these are different JSON shapes but can be the
  same C# default), and any boundary numeric value relevant to `[JsonNumberHandling]`
  configuration in play.
