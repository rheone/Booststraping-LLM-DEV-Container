# StringBuilder

`StringBuilder` maintains a mutable, resizable internal character buffer, letting repeated
append/insert/remove operations avoid the repeated allocate-and-copy cost plain string
concatenation incurs in a loop (see
[string-immutability-and-performance.md](string-immutability-and-performance.md)).

## Basic usage

```csharp
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.Append(item.Name).Append(", ");
}
string result = sb.ToString();
```

`Append` returns the `StringBuilder` itself, so chaining calls together is idiomatic and avoids
repeating the variable name for every piece appended.

## Sizing capacity up front

`StringBuilder`'s internal buffer grows by reallocating and copying, the same underlying cost
plain string concatenation has — just amortized across doubling growth instead of paid on every
single append. When the final size is knowable or reasonably estimable ahead of time, pass it to
the constructor to avoid that reallocation entirely:

```csharp
var sb = new StringBuilder(capacity: items.Count * averageItemLength);
```

Getting the estimate exactly right isn't necessary — even an approximate capacity that avoids most
of the doubling growth steps captures most of the benefit. Guessing too low costs a few
reallocations; guessing too high costs a bit of unused memory, which is the cheaper failure mode of
the two for most code.

## `Append` overloads avoid intermediate string allocations

Prefer `sb.Append(value)` for a non-string value (`int`, `double`, a formattable struct) over
`sb.Append(value.ToString())` — the direct overload formats into the existing buffer without
allocating an intermediate string first:

```csharp
sb.Append(count).Append(" items, total: ").Append(total.ToString("C"));
```

`AppendFormat`/`AppendJoin` cover composite formatting and separator-joining without manually
interleaving literal separators between `Append` calls:

```csharp
sb.AppendJoin(", ", items.Select(i => i.Name));
```

## Interpolated strings inside `Append`

`StringBuilder.Append` has an overload accepting an interpolated string directly
(`sb.Append($"{name}: {value}")`) that, since the interpolated string handler mechanism (see
[string-interpolation-and-handlers.md](string-interpolation-and-handlers.md)), formats each
interpolation hole directly into the `StringBuilder`'s buffer rather than building an intermediate
`string` first and then appending that whole string — prefer this over manually breaking the
interpolated string into separate `Append` calls, since the handler-based overload already gets the
allocation-avoidance benefit without the awkwardness of splitting the expression by hand.

## Reuse vs. discard

A `StringBuilder` can be reused across multiple build cycles via `Clear()` (which resets `Length`
to zero but keeps the existing buffer capacity) — useful in a loop that builds and consumes many
strings in sequence, avoiding a fresh allocation and fresh growth-doubling on every iteration:

```csharp
var sb = new StringBuilder(256);
foreach (var batch in batches)
{
    sb.Clear();
    foreach (var item in batch)
    {
        sb.Append(item).Append(';');
    }
    Process(sb.ToString());
}
```

## `ToString()` still allocates

Calling `ToString()` allocates the final immutable `string` and copies the buffer's content into
it — `StringBuilder` defers allocation and copying until this point, it doesn't eliminate it
entirely. Avoid calling `ToString()` more than once on the same `StringBuilder` state when only one
final string is actually needed; each call re-copies the buffer.
