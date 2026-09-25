# Property-shaping attributes

Per-type/per-member attributes (namespace `System.Text.Json.Serialization`) that control how a
specific type or property serializes, without touching global `JsonSerializerOptions`.

## `[JsonPropertyName]`

Overrides the JSON property name for one member, independent of any naming policy in effect.

```csharp
public class Order
{
    [JsonPropertyName("order_id")]
    public int Id { get; set; }
}
```

An explicit `[JsonPropertyName]` always wins over a `PropertyNamingPolicy` — the policy only
applies to members that don't carry this attribute.

## `[JsonIgnore]`

Excludes a member from serialization and/or deserialization.

```csharp
public class User
{
    public string Name { get; set; } = "";

    [JsonIgnore]
    public string? Password { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MiddleName { get; set; }
}
```

`Condition` (`JsonIgnoreCondition` enum) controls when the member is skipped:

- `Always` (the default when `Condition` is omitted) — never serialized or deserialized.
- `Never` — always included, overriding a type-level or global ignore-null default.
- `WhenWritingDefault` — omitted when the value equals `default` for its type (`null` for
  reference types, `0`/`false`/etc. for value types).
- `WhenWritingNull` — omitted only when the value is `null` (reference types and `Nullable<T>`).

This is the per-property counterpart to `JsonSerializerOptions.DefaultIgnoreCondition`, which
applies the same conditions globally.

## `[JsonInclude]`

Opts a normally-excluded member into serialization: **fields** (never included by default) and
**non-public property setters/getters**.

```csharp
public class Point
{
    [JsonInclude]
    public readonly int X; // fields are excluded by default; this opts it in

    [JsonInclude]
    public int Y { get; private set; } // exposes a private setter to the serializer
}
```

By default, `System.Text.Json` only serializes **public properties** (not fields, not
non-public members) — this is a deliberate, more conservative default than Newtonsoft.Json, which
includes public fields automatically.

## `[JsonConstructor]`

Marks the constructor the deserializer should use when a type has multiple public constructors
(or when you want to force use of a non-default one).

```csharp
public class Coordinates
{
    public double Lat { get; }
    public double Lng { get; }

    [JsonConstructor]
    public Coordinates(double lat, double lng)
    {
        Lat = lat;
        Lng = lng;
    }
}
```

Without `[JsonConstructor]`, the serializer picks: a public parameterless constructor if one
exists, or — if there's exactly one public parameterized constructor — that one (constructor
parameter names are matched to JSON property names case-insensitively by default for this
purpose). With more than one public parameterized constructor and no parameterless one, the
serializer throws `InvalidOperationException` unless exactly one constructor is marked
`[JsonConstructor]`.

## `[JsonPropertyOrder]`

Controls the order properties are written in serialized output (does not affect deserialization,
which matches by name regardless of JSON property order).

```csharp
public class Envelope
{
    [JsonPropertyOrder(-1)]
    public string Type { get; set; } = "";

    public string Payload { get; set; } = "";
}
```

Default order value is `0`; properties are written in ascending order-value, and properties
sharing the same order value fall back to declaration order. Useful for putting a discriminator or
`id`-like field first in output for readability, without relying on incidental declaration order.

## `[JsonNumberHandling]`

Relaxes strict JSON number handling for a specific numeric property (or an entire type, or
globally via `JsonSerializerOptions.NumberHandling`).

```csharp
public class Invoice
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Total { get; set; } // accepts both 42.50 and "42.50" on read
}
```

Flags (combinable): `AllowReadingFromString` (accept a JSON string for a number-typed property on
read), `WriteAsString` (emit numbers as JSON strings), `AllowNamedFloatingPointLiterals` (accept/
emit `"NaN"`, `"Infinity"`, `"-Infinity"` for `float`/`double`). Default is `Strict` — JSON numbers
must be JSON number tokens, not strings. This exists because JSON's own number type cannot
represent `NaN`/`Infinity`, and because some producers (JavaScript numeric precision limits,
certain client libraries) emit large/decimal numbers as JSON strings.

## `[JsonConverter]`

Attaches a specific converter (custom or built-in) to a type or property, taking precedence over
whatever converter would otherwise be selected (including one registered in
`JsonSerializerOptions.Converters`).

```csharp
public class Product
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductStatus Status { get; set; }
}

[JsonConverter(typeof(ProductIdConverter))]
public readonly struct ProductId
{
    public int Value { get; }
    public ProductId(int value) => Value = value;
}
```

Placing `[JsonConverter]` directly on a type definition applies it everywhere that type is
serialized, without needing to register the converter in every `JsonSerializerOptions` instance
used across the app. See `custom-converters.md` for writing the converter itself, and
`type-handling.md` for `JsonStringEnumConverter` specifically.
