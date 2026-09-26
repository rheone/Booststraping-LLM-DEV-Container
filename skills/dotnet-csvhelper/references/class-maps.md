# Class maps

A `ClassMap<T>` states explicitly how a type's members correspond to CSV columns, for any case
where CsvHelper's default (match a public property name to a header name, case-insensitively)
doesn't hold: renamed columns, positional-only files with no header, computed/derived values, or
members that should be skipped entirely.

## Defining a map

```csharp
public sealed class OrderMap : ClassMap<Order>
{
    public OrderMap()
    {
        Map(o => o.CustomerName).Name("Customer");
        Map(o => o.Quantity).Name("Qty");
        Map(o => o.PlacedAt).Name("Order Date");
        Map(o => o.InternalNotes).Ignore();
    }
}
```

```csharp
csv.Context.RegisterClassMap<OrderMap>();
IEnumerable<Order> orders = csv.GetRecords<Order>();
```

Register the map on the `CsvReader`/`CsvWriter`'s `Context` before calling `GetRecords<T>`/
`WriteRecords` — registering it after the first record has already been processed has no effect on
records already read or written.

## `Name()` vs. `Index()`

- `Name("Customer")` matches by header text — use this whenever the file has a header row, since it
  tolerates column reordering between file versions.
- `Index(0)` matches by zero-based column position — use this only for genuinely headerless files,
  where there is no name to match against. Combine with `CsvConfiguration.HasHeaderRecord = false`.

```csharp
public sealed class HeaderlessOrderMap : ClassMap<Order>
{
    public HeaderlessOrderMap()
    {
        Map(o => o.CustomerName).Index(0);
        Map(o => o.Quantity).Index(1);
    }
}
```

## `Ignore()`

Excludes a member from both reading and writing entirely — use for computed properties, navigation
properties on an entity, or any member that has no corresponding column and would otherwise cause
CsvHelper's automatic mapping to look for a column that doesn't exist.

```csharp
Map(o => o.ComputedTotal).Ignore();
```

## `Default()` and `Optional()`

`Default(value)` supplies a fallback when the column is present but the cell is empty.
`Optional()` tells CsvHelper the column itself may be entirely absent from the file's header without
that being a mapping error (distinct from `MissingFieldFound`, which governs a row with fewer
columns than the header — see [references/malformed-data.md](malformed-data.md)).

```csharp
Map(o => o.DiscountCode).Optional();
Map(o => o.Priority).Default("Standard");
```

## `Convert()` for one-off, map-local conversion logic

`Convert()` takes a delegate that builds the member's value from the current row, for a conversion
specific to this one map rather than a reusable type-wide rule (which belongs in an `ITypeConverter`
— see [references/type-converters.md](type-converters.md)).

```csharp
Map(o => o.TotalWithTax).Convert(row =>
{
    decimal subtotal = row.Row.GetField<decimal>("Subtotal");
    return subtotal * 1.08m;
});
```

## Auto-mapping with targeted overrides

`ClassMap<T>.AutoMap(configuration)` inside a map's constructor generates the default member-to-
column mapping, which you can then adjust with additional `Map()` calls for just the members that
need to differ from the default — cheaper to maintain than hand-writing every member when only a
few need custom treatment.

```csharp
public sealed class OrderMap : ClassMap<Order>
{
    public OrderMap()
    {
        AutoMap(CultureInfo.InvariantCulture);
        Map(o => o.PlacedAt).Name("Order Date");
        Map(o => o.InternalNotes).Ignore();
    }
}
```
