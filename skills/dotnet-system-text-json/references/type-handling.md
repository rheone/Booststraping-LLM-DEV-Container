# Type-specific handling

How `System.Text.Json` treats specific C# language/BCL features.

## Nullable reference types

`System.Text.Json` does **not** enforce nullable reference type (NRT) annotations by default — a
`string` (non-nullable, per NRT annotations) property will happily accept and store a JSON `null`
during deserialization; NRT annotations are a compile-time-only C# feature and carry no runtime
metadata the serializer inspects automatically. If you need a JSON `null` on a non-nullable
property to be rejected, that's an application-level validation concern (e.g. a data-annotations
pass or manual check after deserialization), not something `JsonSerializerOptions` turns on. What
the serializer *does* honor for non-nullability is `required` members (see below), which is a
narrower, deserialization-time enforcement of "this property must be present in the JSON," not a
null-vs-non-null check.

## Records and init-only properties

Records and `init`-only properties work with the default (parameterless-constructor-based) path
when the type has one, and work equally well through the parameterized-constructor path:

```csharp
public record Order(int Id, string Customer); // positional record — uses its primary constructor

public class Order2
{
    public int Id { get; init; }
    public string Customer { get; init; } = "";
}
```

For a positional record, the serializer matches JSON property names to constructor parameter
names (case-insensitively for this matching, by default) the same way it matches to a
`[JsonConstructor]`-selected constructor for an ordinary class — see `attributes.md`. `init`-only
properties on a non-record class are set via the object initializer during deserialization the
same way; no special attribute is needed for either.

## `required` members

Since .NET 7, the serializer honors the C# `required` modifier: deserialization throws
`JsonException` if the JSON is missing a property mapped to a `required` member.

```csharp
public class Customer
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public int? LoyaltyPoints { get; set; } // not required — fine if absent
}
```

This is a real, enforced constraint at deserialization time (unlike NRT annotations, which the
serializer ignores) — use `required` when a property must always be present in valid input, rather
than relying on a non-nullable type alone.

## `DateTime`, `DateOnly`, `TimeOnly`

- `DateTime`/`DateTimeOffset` serialize to the ISO 8601-1:2019 round-trippable format by default
  (e.g. `"2026-09-25T14:30:00Z"`) — no attribute needed for the common case. To use a different
  format, write a custom `JsonConverter<DateTime>` (see `custom-converters.md`); there is no
  built-in "format string" option on `JsonSerializerOptions` for date formatting.
- `DateOnly` and `TimeOnly` (the date-only/time-only BCL types) have built-in serialization
  support — `DateOnly` as `"yyyy-MM-dd"`, `TimeOnly` as `"HH:mm:ss"` (extended with fractional
  seconds when non-zero) — with no custom converter required.
- Both `DateTime`/`DateTimeOffset` and `DateOnly` are supported as `Dictionary<TKey, TValue>` key
  types (serialized as the JSON object's key string, formatted the same way as their value-position
  serialization); support for less common key types varies by exact type and .NET version, so
  verify against the specific TFM in use if reaching for something unusual as a dictionary key.

## Enums

By default, enums serialize as their **underlying numeric value** (`ProductStatus.Active` with
value `1` serializes as `1`, not `"Active"`), which is often surprising to newcomers who expect
string enum names — see `pitfalls.md`.

To serialize as the member name instead, use `JsonStringEnumConverter`:

```csharp
var options = new JsonSerializerOptions
{
    Converters = { new JsonStringEnumConverter() },
};
```

Or per-property/per-type via `[JsonConverter(typeof(JsonStringEnumConverter))]` (see
`attributes.md`). A generic `JsonStringEnumConverter<TEnum>` variant exists for pinning the
converter to one specific enum type; prefer the generic form in a Native AOT-published app — the
non-generic `JsonStringEnumConverter` relies on reflection internally to discover the enum type at
run time, while the generic form does not, making it the AOT/trim-safe choice.

`JsonStringEnumConverter`'s constructor also accepts an optional `JsonNamingPolicy` (e.g.
`new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`) to camelCase enum member names in
output, independent of whatever naming policy applies to ordinary properties.

## Dictionaries with non-`string` keys

`Dictionary<TKey, TValue>` (and other `IDictionary`-shaped types) normally require `TKey` to be
`string` for JSON serialization, since JSON object keys are always strings. `System.Text.Json`
extends this to also support several other key types directly, converting the key to/from its
JSON-object-key string form automatically: numeric types (`int`, `long`, etc.), `enum` types, and
`Guid`, among others.

```csharp
Dictionary<int, string> byId = new() { [1] = "Ada", [2] = "Linus" };
string json = JsonSerializer.Serialize(byId); // {"1":"Ada","2":"Linus"}
```

For a key type not natively supported, write a custom `JsonConverter<TKey>` — note that a
dictionary-key converter must override `ReadAsPropertyName`/`WriteAsPropertyName` in addition to
(or instead of) the ordinary `Read`/`Write`, since a dictionary key is serialized in JSON's
property-name position, not its value position.
