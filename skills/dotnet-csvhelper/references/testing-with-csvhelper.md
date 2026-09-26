# Testing code that reads or writes CSV

## How to test it

- **Use `StringReader`/`StringWriter` instead of real files.** CsvHelper's constructors take any
  `TextReader`/`TextWriter`, so a unit test never needs to touch disk — build the input as an
  in-memory string, or capture output into a `StringWriter` and assert on its final text.

```csharp
[Fact]
public void GetRecords_maps_columns_by_header_name()
{
    const string csvText = "Customer,Qty\nAcme Co,5\n";
    using var reader = new StringReader(csvText);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    csv.Context.RegisterClassMap<OrderMap>();

    var orders = csv.GetRecords<Order>().ToList();

    Assert.Single(orders);
    Assert.Equal("Acme Co", orders[0].CustomerName);
    Assert.Equal(5, orders[0].Quantity);
}
```

```csharp
[Fact]
public void WriteRecords_produces_expected_csv_text()
{
    var orders = new[] { new Order { CustomerName = "Acme Co", Quantity = 5 } };
    using var writer = new StringWriter();
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        csv.Context.RegisterClassMap<OrderMap>();
        csv.WriteRecords(orders);
    }

    Assert.Equal("Customer,Qty\r\nAcme Co,5\r\n", writer.ToString());
}
```

- **Assert on the parsed objects, not the raw text, when testing reading.** The point of a
  `ClassMap<T>` or type converter is the shape it produces — assert on property values, not on
  intermediate strings.
- **Assert on exact written text, including the newline style, when testing writing.** CsvHelper
  defaults to `\r\n` row terminators regardless of platform; pin the expected string to that default
  (or to whatever `CsvConfiguration.NewLine` the code under test configures) rather than using a
  platform-dependent `Environment.NewLine` in the assertion.
- **Dispose the writer before reading back its `StringWriter`'s buffer.** `CsvWriter` buffers writes
  and flushes on disposal; asserting on `writer.ToString()` before the `using` block for the
  `CsvWriter` has closed can observe a partially-flushed buffer.
- **Test a custom `ITypeConverter` directly**, independent of a full read/write round trip — call
  `ConvertFromString`/`ConvertToString` with representative inputs and assert on the returned value,
  which isolates a conversion bug from a mapping or configuration bug.

## Common scenarios

### Testing a `ClassMap<T>` maps every expected column

Feed a minimal CSV string containing every mapped column plus one extra to confirm the map both
picks up what it should and ignores what it's told to ignore. Include a row that exercises
`Optional()`/`Default()` members by omitting or leaving that column blank.

### Testing custom malformed-data handling

Construct a `CsvConfiguration` with the same `BadDataFound`/`MissingFieldFound`/
`ReadingExceptionOccurred` delegates the production code uses, feed input crafted to trigger each
one, and assert both that the callback fired (e.g. by capturing its arguments into a test-local
list) and what the reader ultimately produced for that row.

### Testing round-trip fidelity for a custom type converter

Write a value with the converter's `ConvertToString`, then read the resulting text back with
`ConvertFromString`, and assert the round-tripped value equals the original — catches an asymmetric
converter (writes one format, only reads a different one) that a one-directional test would miss.
