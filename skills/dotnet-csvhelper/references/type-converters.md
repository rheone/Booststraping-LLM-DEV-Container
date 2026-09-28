# Type converters

CsvHelper resolves a string cell to a member's type through `ITypeConverter`, and already has
built-in converters for the common BCL types (numbers, `DateTime`, `bool`, `enum`, `Guid`, and
more). Write a custom `ITypeConverter` for a type CsvHelper can't already convert, or where the
built-in conversion rule doesn't match this file's actual format.

## Implementing `ITypeConverter`

```csharp
public sealed class MoneyConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Money.Zero;
        }

        return Money.Parse(text.TrimStart('$'), CultureInfo.InvariantCulture);
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value is Money money ? $"${money.Amount:F2}" : base.ConvertToString(value, row, memberMapData);
    }
}
```

Deriving from `DefaultTypeConverter` (rather than implementing `ITypeConverter` from scratch) gives
a reasonable `ConvertToString` fallback (`.ToString()`) for free when only reading needs custom
behavior.

## Registering a converter

Per-member, on the class map — the common case, since most custom conversion needs are specific to
one member's format rather than every occurrence of a type:

```csharp
Map(o => o.Total).TypeConverter<MoneyConverter>();
```

Type-wide, via the configuration's `TypeConverterCache` — for a type that should always convert the
same way everywhere it appears across the whole mapped object graph:

```csharp
csv.Context.TypeConverterCache.AddConverter<Money>(new MoneyConverter());
```

Via `[TypeConverter(typeof(MoneyConverter))]` directly on the property — couples the domain type to
CsvHelper, so prefer one of the two registration approaches above unless the type genuinely has no
meaning outside CSV serialization.

## Custom enum text that doesn't match the enum member names

`EnumConverter` (the default for enum members) matches by member name. When a file's text doesn't
match the enum's C# names (localized text, legacy codes), map it explicitly rather than renaming the
enum to match the file:

```csharp
Map(o => o.Status).Convert(row => row.Row.GetField("Status") switch
{
    "P" => OrderStatus.Pending,
    "S" => OrderStatus.Shipped,
    _ => throw new FormatException($"Unrecognized status code: {row.Row.GetField("Status")}"),
});
```

## Nullable value types

CsvHelper's built-in converters already handle `Nullable<T>` for the value types they support
(`int?`, `DateTime?`, and so on) by returning `null` for an empty cell — a custom converter for a
nullable wrapper is only needed when the underlying type itself needs custom conversion, in which
case implement the converter against the non-nullable type and let CsvHelper's nullable handling
wrap it.
