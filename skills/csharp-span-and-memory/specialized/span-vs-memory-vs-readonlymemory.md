# `Span<T>` vs. `Memory<T>` vs. `ReadOnlyMemory<T>`: choosing the right one

All four types (`Span<T>`, `ReadOnlySpan<T>`, `Memory<T>`, `ReadOnlyMemory<T>`) represent a
contiguous region of memory, but only the `Span<T>` pair is a `ref struct` — see
[csharp7.2-ref-struct-and-span.md](../references/csharp7.2-ref-struct-and-span.md) for why that
makes it stack-only. `Memory<T>`/`ReadOnlyMemory<T>` are ordinary structs specifically *because*
`Span<T>` can't go where they need to: onto the heap, into a field, across an `await`, into a
closure, or into an async iterator's state machine. Picking between them is a single question:
does this value need to survive past the current synchronous call stack?

## Basic: a synchronous, single-call API uses `Span<T>`

```csharp
public static int CountVowels(ReadOnlySpan<char> text)
{
    int count = 0;
    foreach (char c in text)
    {
        if ("aeiouAEIOU".Contains(c))
        {
            count++;
        }
    }
    return count;
}

int vowels = CountVowels("hello world"); // string -> ReadOnlySpan<char> implicit conversion
```

`Span<T>`/`ReadOnlySpan<T>` is the right choice whenever the method runs entirely synchronously and
doesn't need to store the reference anywhere beyond its own call frame — it's strictly cheaper (no
extra indirection to unwrap before use) and the compiler enforces the "doesn't escape" contract for
you.

## Basic: an async API, or one that stores the reference, needs `Memory<T>`

```csharp
public async Task<int> CountVowelsAsync(ReadOnlyMemory<char> text)
{
    await Task.Yield(); // Span<T>/ReadOnlySpan<T> could never appear as a parameter here at all
    ReadOnlySpan<char> span = text.Span; // materialize a Span only in the synchronous portion that needs it
    int count = 0;
    foreach (char c in span)
    {
        if ("aeiouAEIOU".Contains(c))
        {
            count++;
        }
    }
    return count;
}
```

`ReadOnlyMemory<T>`/`Memory<T>` can be a field, a parameter to an `async` method, captured in a
lambda, or stored in a collection — everything `Span<T>` cannot do, because it's an ordinary struct
wrapping either an array reference plus offset/length, or a `MemoryManager<T>` for non-array-backed
memory. Call `.Span` to get a `Span<T>`/`ReadOnlySpan<T>` view for the synchronous portion of code
that actually indexes into it; that view itself still can't cross an `await` or be stored.

## Advanced: an API surface offering both, without duplicating logic

```csharp
public class LogBuffer
{
    private readonly Memory<char> _storage;

    public LogBuffer(int capacity) => _storage = new char[capacity];

    // synchronous, hot-path write: Span<T> parameter, zero extra indirection
    public void Append(ReadOnlySpan<char> text) => text.CopyTo(_storage.Span[..text.Length]);

    // async path: Memory<T> is the only option, since this crosses an await
    public async Task FlushAsync(Stream destination)
    {
        await destination.WriteAsync(GetBytes(_storage)); // Memory<T> survives the await; Span<T> could not
    }

    private static ReadOnlyMemory<byte> GetBytes(Memory<char> chars) =>
        System.Text.Encoding.UTF8.GetBytes(chars.Span.ToString());
}
```

A single type can expose both shapes: `Span<T>` parameters for hot, synchronous entry points, and
`Memory<T>` fields/parameters for anything that needs to persist or cross an `await`. The field
itself is always `Memory<T>` (or an array) — never `Span<T>` — because a `ref struct` field on a
non-`ref struct` class is illegal at the language level regardless of async at all; see
[ref-struct-constraints-and-limitations.md](ref-struct-constraints-and-limitations.md).

## Advanced: `ReadOnlyMemory<T>` for defensive, non-mutable API contracts

```csharp
public sealed class Document
{
    public ReadOnlyMemory<char> Content { get; }

    public Document(string content) => Content = content.AsMemory();
}

// callers can read via .Span, but the property itself signals "you will not mutate this"
void Print(Document doc) => Console.WriteLine(doc.Content.Span.ToString());
```

`ReadOnlyMemory<T>` is to `Memory<T>` what `ReadOnlySpan<T>` is to `Span<T>` — same escape
capabilities, but the type itself documents and enforces (at the `Span` view level) that callers
can't write through it. Prefer it over `Memory<T>` on any public member exposing stored data the
type doesn't intend consumers to mutate in place.

## Fallback

None of these four types exist before [C# 7.2 / the `System.Memory` package](../references/csharp7.2-ref-struct-and-span.md#the-language-version-vs-bcl-version-split).
Below that BCL availability, use `ArraySegment<T>` for the synchronous, array-backed case and plain
arrays (or `byte[]`/`char[]` fields) for anything that needs to survive an `async` boundary — see
[pre-csharp7-arrays-and-pointers.md](../references/pre-csharp7-arrays-and-pointers.md).
