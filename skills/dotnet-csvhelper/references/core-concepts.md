# Core concepts

## `CsvReader`

Wraps a `TextReader` and exposes CSV rows as either raw fields or strongly-typed records. Always
construct it with an explicit `CultureInfo` (or a `CsvConfiguration` that specifies one) rather than
letting it default, since numeric and date parsing behavior depends on it — see
[references/culture-and-formatting.md](culture-and-formatting.md).

```csharp
using var reader = new StreamReader("orders.csv");
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

IEnumerable<Order> orders = csv.GetRecords<Order>();
```

`GetRecords<T>` returns a single-pass, lazily-evaluated sequence: each `MoveNext()` reads and parses
one more row from the underlying stream. Enumerate it exactly once, in order — a second enumeration
attempt does not re-read the file; it continues (or throws, depending on reader state) from wherever
the first enumeration left off.

## Reading row-by-row without a target type

For files whose shape isn't known ahead of time, or where per-row control is needed, drive the
reader manually instead of `GetRecords<T>`:

```csharp
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
csv.Read();
csv.ReadHeader();

while (csv.Read())
{
    string? customerName = csv.GetField("CustomerName");
    int quantity = csv.GetField<int>("Quantity");
}
```

`csv.Read()` advances to the next row without parsing it into any particular shape; `GetField`/
`GetField<T>` pull individual values out of the current row by column name or index.

## `CsvWriter`

Wraps a `TextWriter` and writes either strongly-typed records or individual fields.

```csharp
using var writer = new StreamWriter("output.csv");
using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

csv.WriteRecords(orders);
```

`WriteRecords` writes the header row (derived from the type's public properties, or a registered
`ClassMap<T>` — see [references/class-maps.md](class-maps.md)) followed by one row per item, and
flushes as it goes rather than buffering the entire output in memory.

## Writing row-by-row without a fixed record type

```csharp
csv.WriteField("CustomerName");
csv.WriteField("Quantity");
csv.NextRecord();

foreach (var order in orders)
{
    csv.WriteField(order.CustomerName);
    csv.WriteField(order.Quantity);
    csv.NextRecord();
}
```

`NextRecord()` terminates the current row (writing the configured newline) and must be called after
every row's fields are written, including the header row — omitting it runs the next row's fields
into the same line.

## Disposal

Both `CsvReader` and `CsvWriter` implement `IDisposable` and hold the underlying `TextReader`/
`TextWriter` they were constructed with. Wrap both the stream/text reader-writer and the CsvHelper
object in `using` (or a single `using` per object, disposed in reverse construction order) so a
`CsvWriter`'s buffered final flush actually happens before the underlying stream closes.
