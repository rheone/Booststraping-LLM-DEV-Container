# CsvHelper

CsvHelper is a third-party library for reading and writing CSV (and other delimited) files in .NET.
This skill covers `CsvReader`/`CsvWriter` basics, mapping columns to properties with `ClassMap<T>`,
handling malformed rows, and streaming large files without loading them fully into memory.

## When to reach for it

- Reading or writing a CSV file for the first time, or reviewing an existing `CsvReader`/`CsvWriter`
  setup.
- Columns don't line up 1:1 with property names, or a property needs custom conversion logic.
- A file has malformed rows, inconsistent column counts, or quoting errors that throw partway
  through a read.
- Processing a file too large to comfortably hold in memory as a single collection.
- Numbers or dates in a file don't parse the way you expect because of culture settings.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing or
debugging code that reads or produces CSV. You can also invoke it directly as `/dotnet-csvhelper`.

## What it covers

| Topic | Reference |
| --- | --- |
| CsvReader, CsvWriter, GetRecords, WriteRecords | [references/core-concepts.md](references/core-concepts.md) |
| ClassMap, Map, Name, Index, Ignore, Convert | [references/class-maps.md](references/class-maps.md) |
| Custom ITypeConverter implementations | [references/type-converters.md](references/type-converters.md) |
| Handling malformed or inconsistent rows | [references/malformed-data.md](references/malformed-data.md) |
| Streaming large files without full materialization | [references/streaming-large-files.md](references/streaming-large-files.md) |
| Culture-specific number and date formatting | [references/culture-and-formatting.md](references/culture-and-formatting.md) |
| Testing code that reads or writes CSV | [references/testing-with-csvhelper.md](references/testing-with-csvhelper.md) |

## Example prompts

- "Why does this CsvReader throw a BadDataFound exception on row 40 of this file?"
- "Write a ClassMap that maps this CSV's snake_case headers to my Order class's properties."
- "This file is a few gigabytes: how do I process it without loading it all into memory?"
