# Handling malformed data

CsvHelper's malformed-input callbacks take a single `args` parameter carrying the relevant context
(this shape applies from CsvHelper 23 onward; the current 33.1.0 release referenced by this skill
uses it) — configure them on `CsvConfiguration`, not on the reader/writer instance directly.

## `BadDataFound`

Fires when a field contains data CsvHelper considers structurally invalid for the current quoting
rules — most commonly, a quote character appearing inside a field that isn't itself quoted.

```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    BadDataFound = args =>
    {
        _logger.LogWarning("Bad data in field {Field} at {RawRow}", args.Field, args.RawRecord);
    },
};

using var csv = new CsvReader(reader, config);
```

Leaving `BadDataFound` unset throws `BadDataException` the moment CsvHelper encounters bad data.
Setting it to a no-op delegate suppresses the exception and continues reading with whatever value
CsvHelper recovered for that field — decide explicitly whether a bad-data row should be logged and
skipped, logged and kept with a best-effort value, or should still fail the whole read, and encode
that decision in the delegate rather than leaving the default throw in place by omission.

## `MissingFieldFound`

Fires when a data row has fewer columns than the header (or than the first row, for headerless
files) led CsvHelper to expect.

```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    MissingFieldFound = args =>
    {
        _logger.LogWarning("Missing field(s) at row {Row}", args.Context.Parser.Row);
    },
};
```

Setting `MissingFieldFound = null` disables the callback entirely and suppresses
`MissingFieldException` — CsvHelper then leaves the corresponding member at its type's default value
instead of throwing. Prefer an explicit logging delegate over `null` even when the outcome is "keep
going," so a genuinely malformed file still leaves a trace instead of silently producing incomplete
records.

## `HeaderValidated`

Fires after the header row is read, letting you check that every column a class map expects is
actually present before any data row is processed — catches a renamed or missing column immediately
rather than as a `MissingFieldFound` callback per affected row.

```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HeaderValidated = args =>
    {
        if (args.InvalidHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"Missing expected column(s): {string.Join(", ", args.InvalidHeaders.Select(h => h.Names[0]))}");
        }
    },
};
```

## `ReadingExceptionOccurred`

Fires when converting a field's text to the target member's type throws (a bad number, an
unparseable date) — returning `true` from the delegate skips the row and continues; returning
`false` (or leaving the callback unset) lets the exception propagate and stops the read.

```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    ReadingExceptionOccurred = args =>
    {
        _logger.LogError(args.Exception, "Failed to parse row {Row}", args.Exception.Context.Parser.Row);
        return false; // stop the read; treat a conversion failure as fatal for this file
    },
};
```

## Choosing "skip and continue" vs. "fail the read"

Treat malformed-input handling as a decision about the file's source, not a default to reach for
uniformly: a file produced by a trusted internal system that fails to parse is more likely a bug
worth surfacing loudly (fail fast, don't set these callbacks to swallow errors), while a file
uploaded by an external party is more likely to have occasional bad rows worth logging and skipping
so one bad row doesn't block the whole batch.
