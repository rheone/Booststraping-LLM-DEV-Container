# Buffered vs. Unbuffered Queries, and Performance

## Buffered (default) vs. unbuffered

`Query<T>` (and `QueryAsync<T>`) buffers the entire result set into a `List<T>` in memory before
returning, by default — the underlying data reader is fully consumed and closed before your code
sees the first row. Passing `buffered: false` returns an `IEnumerable<T>` that streams rows lazily as
you enumerate it, keeping the underlying reader (and the connection) open for the duration:

```csharp
foreach (var order in connection.Query<Order>(sql, buffered: false))
{
    Process(order);
}
```

Unbuffered reading avoids materializing a large result set fully in memory at once, which matters for
a genuinely large row count you're streaming through rather than holding onto. It also means the
connection stays open and busy for as long as your enumeration takes — including whatever processing
you do per row inside the loop — so it does not compose with anything else needing that connection
concurrently, and an exception partway through processing leaves the reader in a partially consumed
state. Default to buffered; reach for `buffered: false` specifically for a known-large streaming
scenario where holding the whole set in memory is the actual problem.

There is no async unbuffered equivalent that streams incrementally the same way — `QueryAsync`
always buffers fully; a genuinely async streaming read over a large result set needs the lower-level
`ExecuteReaderAsync`/`IDataReader` API directly rather than Dapper's `Query` family.

## SQL-injection pitfalls

Covered in full in [parameterization.md](parameterization.md): every varying value is a parameter,
never string-concatenated or interpolated into the SQL text. The pitfall recurs specifically around
"trusted" internal values (an ID from another table, an enum's `.ToString()`) that get concatenated
because they don't look like user input at the call site — the SQL text itself has no way to know
where a value came from, so the rule doesn't have a safe-value exception.

## Performance pitfalls

**Re-parsing the same SQL text with cosmetic differences.** Most database engines cache query
execution plans keyed by the literal SQL text. Two calls that concatenate a different literal value
into otherwise-identical SQL produce two different cache entries instead of reusing one — another
reason parameterization matters beyond injection safety: `WHERE Id = @id` with a parameter reuses one
cached plan across every call; `WHERE Id = 42` and `WHERE Id = 43` as literal text do not.

**`Query<T>` against a very wide `SELECT *` when only a few columns are used.** Dapper maps whatever
columns come back; a `SELECT *` against a wide table pulls (and maps) columns nothing downstream
reads. Naming only the columns actually used both reduces data transferred and removes ambiguity
about which columns the mapped type actually depends on.

**Opening a new connection per row inside a loop instead of per unit of work.** Reuse one open
connection across a batch of related calls (see
[core-concepts.md](core-concepts.md)) rather than opening and disposing one per iteration — the
underlying provider's connection pool absorbs a per-unit-of-work open/close cheaply, but a tight loop
still adds unnecessary pool checkout/return overhead per iteration compared to reusing one already-
open connection for the whole batch.
