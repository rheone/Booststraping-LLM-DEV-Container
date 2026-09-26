# CsvHelper

Guidance on CsvHelper, a third-party CSV reading and writing library for C#/.NET — the routing
table (by situation, not by CsvHelper/C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per CsvHelper/C# version

| File | Covers |
| --- | --- |
| `core-concepts.md` | CsvReader, CsvWriter, GetRecords\<T>, WriteRecords, disposal |
| `class-maps.md` | ClassMap\<T>, Map(), Name(), Index(), Ignore(), Convert() |
| `type-converters.md` | ITypeConverter, TypeConverterAttribute, registering custom converters |
| `malformed-data.md` | BadDataFound, MissingFieldFound, HeaderValidated, ReadingExceptionOccurred |
| `streaming-large-files.md` | Streaming GetRecords\<T> as IEnumerable\<T>, avoiding full materialization |
| `culture-and-formatting.md` | CsvConfiguration.CultureInfo, per-member format strings, invariant vs. locale parsing |
| `testing-with-csvhelper.md` | In-memory StringReader/StringWriter, asserting parsed/written CSV, testing maps and converters |

## Scope

CsvHelper's `CsvReader`/`CsvWriter`/`ClassMap<T>` object model (`CsvHelper` package) for reading and
writing delimited text files. Out of scope: spreadsheet-format packages, non-CsvHelper CSV parsing
approaches, and database bulk-import mechanisms that happen to consume CSV.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
33.1.0.
