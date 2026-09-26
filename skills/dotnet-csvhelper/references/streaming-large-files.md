# Streaming large files

`CsvReader.GetRecords<T>()` and `CsvWriter.WriteRecords<T>(IEnumerable<T>)` are both already
streaming operations by default — the risk in practice is code around them that accidentally
forces full materialization.

## Reading: don't materialize what you don't need to

```csharp
// Streams: one record is read, processed, and discarded before the next is read.
foreach (var order in csv.GetRecords<Order>())
{
    Process(order);
}
```

```csharp
// Defeats streaming: reads and holds every record in memory before processing any of them.
List<Order> allOrders = csv.GetRecords<Order>().ToList();
foreach (var order in allOrders)
{
    Process(order);
}
```

Call `.ToList()`/`.ToArray()` only when downstream code genuinely needs random access or multiple
passes over the full set — and only when the file's size is known to fit comfortably in memory.
Otherwise, process each record as it's read and let it go out of scope (or dispose it) before the
next `Read()` pulls the following row into the same reused row buffer.

## Writing: flush incrementally, don't build the whole output in a string first

```csharp
// Streams: each record is serialized and written to the underlying stream as it's produced.
using var writer = new StreamWriter("output.csv");
using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
csv.WriteRecords(GetOrdersFromDatabase()); // an IEnumerable<Order>, not a pre-materialized list
```

Pass `WriteRecords` a lazily-produced `IEnumerable<T>` (a database cursor, a paged query, a
generator method using `yield return`) instead of a fully materialized collection whenever the
source of the records is itself capable of streaming — this keeps peak memory bounded by one record
at a time rather than by the total row count.

## Async I/O for large files

`CsvReader`/`CsvWriter` expose async counterparts for the underlying I/O-bound operations —
`await csv.ReadAsync()` for manual row-by-row reads, and `await csv.WriteRecordsAsync(records)` for
bulk writes against an `IAsyncEnumerable<T>` or `IEnumerable<T>` source. Prefer these over the
synchronous methods in any code already running on an async path (a web request handler, a
background service) so a large file's I/O doesn't block a thread pool thread for its duration.

```csharp
await foreach (var order in csv.GetRecordsAsync<Order>())
{
    await ProcessAsync(order);
}
```

## Watch the row buffer, not just the record count

CsvHelper reuses an internal buffer for the current row's raw text rather than allocating fresh per
row, so raw parsing itself scales to very large files without a per-row allocation spike. The actual
memory risk is almost always in application code holding onto more parsed records, converted domain
objects, or side-collected data (e.g. accumulating a running list "just in case") than the streaming
pattern above intends.
