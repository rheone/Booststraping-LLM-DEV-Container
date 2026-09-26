# Custom converters

When attributes and naming policies can't express the transformation you need, write a
`JsonConverter<T>` (for a specific type) or `JsonConverterFactory` (for a family of related/
open-generic types).

## `JsonConverter<T>`

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ProductIdConverter : JsonConverter<ProductId>
{
    public override ProductId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"Expected number for ProductId, got {reader.TokenType}.");
        }

        return new ProductId(reader.GetInt32());
    }

    public override void Write(Utf8JsonWriter writer, ProductId value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
```

Register it either on the type (`[JsonConverter(typeof(ProductIdConverter))]` — see
`attributes.md`), on a specific property, or globally via
`JsonSerializerOptions.Converters.Add(new ProductIdConverter())`. Resolution order when more than
one converter could apply: an attribute on the **property** wins over one on the **type**, which
wins over one registered in `Converters` on the options instance.

Key points:

- `Read` receives the reader positioned **at** the value's first token (not before it) and must
  leave the reader positioned at the value's last token when it returns — the serializer advances
  past it.
- Throw `JsonException` for malformed input; don't let an unrelated exception type (e.g.
  `FormatException` from a raw `int.Parse`) escape, since callers generally only catch
  `JsonException` around deserialization.
- `Write` receives the writer already open at the correct position — call exactly one
  `Write*Value`/`WriteStartObject`+members+`WriteEndObject`/etc. sequence that produces one
  complete JSON value.
- A converter for an object type that itself contains properties can call
  `JsonSerializer.Serialize`/`Deserialize` recursively on nested values using the `options`
  parameter passed in — reuse it rather than constructing a new `JsonSerializerOptions`, both for
  correctness (so nested converters/policies stay consistent) and to avoid the metadata-caching
  cost of an options instance the serializer hasn't seen before.

## `JsonConverterFactory`

Use this when the same conversion logic applies to a whole family of types known only at runtime
— most commonly an open generic type (`Nullable<T>`-style wrappers, `Result<T>`, a custom
`Optional<T>`).

```csharp
public sealed class OptionalConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type valueType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(OptionalConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class OptionalConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null
            ? Optional<T>.None
            : Optional<T>.Some(JsonSerializer.Deserialize<T>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
```

`CanConvert` is called once per distinct `Type` the serializer encounters (results are cached), so
it doesn't need to be especially cheap, but it should be precise — an over-broad `CanConvert` can
silently hijack conversion for unrelated types registered later. `CreateConverter` is where you
close the open generic converter over the concrete type argument and construct it.

## Interaction with source generation

A source-generated `JsonSerializerContext` can still use custom converters — reference them via
`[JsonSerializable]`'s type list plus the normal `[JsonConverter]` attribute placement, or add them
to the generated context's `JsonSerializerOptions.Converters`. The one constraint: a
`JsonConverterFactory` that constructs converters via reflection (e.g. `Activator.CreateInstance`
over a `MakeGenericType` result, as above) is not trim/AOT-safe on its own — if the app is
published Native AOT, either avoid reflection inside the factory (e.g. a `switch` over known
closed generic types instead of `MakeGenericType`) or accept that this specific converter needs
the reflection-based serialization path. See `source-generation.md` for the AOT tradeoffs.
