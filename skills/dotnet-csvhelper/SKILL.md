---
name: dotnet-csvhelper
description: Guidance on CsvHelper, a third-party CSV reading/writing library for C#/.NET (current stable release 33.1.0). Covers CsvReader/CsvWriter basics, ClassMap<T> configuration for member-to-column mapping, custom ITypeConverter implementations for non-standard types, handling malformed input via BadDataFound/MissingFieldFound callbacks, streaming large files without loading them fully into memory, culture-specific numeric/date formatting via CsvConfiguration.CultureInfo, and testing code that reads or writes CSV. Use when writing, reviewing, or debugging code that parses or produces CSV files or streams through CsvHelper's CsvReader/CsvWriter/ClassMap API.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# CsvHelper

Guidance on CsvHelper, a third-party CSV reading and writing library for .NET. Current stable
release as of this writing: **33.1.0**. Organized by concern/topic — each reference file notes a
version-introduced fact inline rather than splitting files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| First time reading or writing a CSV file, or reviewing basic setup | `CsvReader`, `CsvWriter`, `GetRecords<T>`, `WriteRecords` | [references/core-concepts.md](references/core-concepts.md) |
| Mapping columns to properties that don't line up 1:1, or need custom conversion per member | `ClassMap<T>`, `Map()`, `Name()`, `Index()`, `Ignore()`, `Convert()` | [references/class-maps.md](references/class-maps.md) |
| A property's type isn't a plain string/number/date CsvHelper handles automatically | `ITypeConverter`, `TypeConverterAttribute`, registering converters on the configuration or the map | [references/type-converters.md](references/type-converters.md) |
| A file has malformed rows, quote errors, or inconsistent column counts | `BadDataFound`, `MissingFieldFound`, `HeaderValidated`, `ReadingExceptionOccurred` | [references/malformed-data.md](references/malformed-data.md) |
| Processing a file too large to hold in memory at once | `CsvReader` as an `IEnumerable<T>` streamed via `GetRecords<T>`, avoiding `ToList()` on the whole sequence | [references/streaming-large-files.md](references/streaming-large-files.md) |
| Numbers/dates in the file don't match the current culture, or output needs a specific culture | `CsvConfiguration.CultureInfo`, per-member format strings, invariant vs. locale-specific parsing | [references/culture-and-formatting.md](references/culture-and-formatting.md) |
| Unit testing code that reads or writes CSV | In-memory `StringReader`/`StringWriter`, asserting on parsed records or written text, testing custom converters and maps in isolation | [references/testing-with-csvhelper.md](references/testing-with-csvhelper.md) |

## Quick start

```csharp
using CsvHelper;
using System.Globalization;

using var reader = new StreamReader("orders.csv");
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

IEnumerable<Order> orders = csv.GetRecords<Order>();
foreach (var order in orders)
{
    Process(order);
}
```

```csharp
using var writer = new StreamWriter("output.csv");
using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

csv.WriteRecords(orders);
```

`GetRecords<T>` returns a lazily-evaluated `IEnumerable<T>` backed by the single underlying
`CsvReader` — enumerate it once, in order; materializing it twice (two separate `foreach` loops, or
calling `.ToList()` and then iterating the reader again) does not produce the same records twice,
because the reader has already advanced past them.

## Out of scope

- `CsvHelper.Excel` or any spreadsheet-format package — this skill covers plain delimited text
  files (CSV/TSV-style), not binary spreadsheet formats.
- Non-CsvHelper CSV parsing (hand-rolled `string.Split` parsing, `Microsoft.VisualBasic.FileIO.
  TextFieldParser`) — out of scope by definition, since this skill's subject is CsvHelper's own API.
- Database bulk-import mechanisms (e.g. SQL Server's `BULK INSERT`) that happen to consume CSV —
  a separate operational concern from producing or consuming CSV through CsvHelper's object model.
