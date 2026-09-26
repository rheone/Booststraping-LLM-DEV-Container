# Raw String Literals

Raw string literals (C# 11 / .NET 7) start and end with three or more consecutive double-quote
characters and require no escaping of anything inside them, including embedded double quotes —
solving the readability problem verbatim strings (`@"..."`) only partly solve (a verbatim string
still needs `""` to represent one embedded quote).

## Basic syntax

```csharp
string json = """
    {
        "name": "Ada",
        "role": "Engineer"
    }
    """;
```

- The opening and closing delimiters are each three (or more) double quotes on their own line.
- Common leading whitespace, measured from the closing delimiter's indentation, is stripped from
  every line — the raw string above renders with `{` starting at column 0, not indented to match
  the source file's indentation, as long as the closing `"""` is indented consistently with the
  content.
- No backslash escaping is needed or interpreted inside a raw string literal at all — `\n`,
  `\"`, `\\` are all literal characters, not escape sequences.

## When content itself contains three double quotes

Use more than three quote characters as the delimiter — the delimiter must be as long as the
longest run of consecutive double quotes appearing in the content plus at least one:

```csharp
string textContainingTripleQuotes = """"
    Here's a raw string containing """ as literal text.
    """";
```

## Embedding JSON, regex, or SQL without escape noise

Raw string literals are the natural fit for embedded JSON payloads (shown above), regex patterns
that would otherwise need doubled backslashes, and SQL/XML fragments with embedded quotes:

```csharp
Regex pathSegment = new("""^[a-zA-Z0-9_\-]+$""");

string sql = """
    SELECT "Id", "Name"
    FROM "Users"
    WHERE "Email" = @email
    """;
```

## Interpolated raw strings

Combine `$` with `"""` for an interpolated raw string; because `{`/`}` are ordinary literal
characters inside a raw string (not interpolation syntax) once no leading `$` is present, an
interpolated raw string needs a matching number of `$` characters to control how many braces open
an interpolation hole — one `$` means single braces interpolate, two `$$` means double braces
interpolate (letting single braces appear as literal text, useful for a raw string mixing JSON-like
literal braces with actual interpolation holes):

```csharp
string singleDollar = $"""
    {{ "id": {userId} }}
    """;
// literal braces need doubling here because a single `$` makes single `{ }` the interpolation syntax

string doubleDollar = $$"""
    { "id": {{userId}} }
    """;
// with `$$`, single braces are literal text and `{{ }}` is the interpolation syntax — reads more
// naturally for JSON-shaped content mixing literal and interpolated braces
```

Prefer `$$"""..."""` (or more `$` characters, matching however many are needed) whenever the raw
string's literal content itself contains single braces (JSON, many templating languages) — it
avoids doubling every literal brace just to escape it.

## When verbatim strings (`@"..."`) still make sense

A single-line string needing only a literal backslash or two (a Windows file path) reads just as
clearly as a verbatim string as it would as a raw string literal, and doesn't need the multi-line
delimiter ceremony:

```csharp
string path = @"C:\Users\name\file.txt"; // fine as-is; a raw string buys nothing extra here
```

Reach for raw string literals specifically when the content has embedded double quotes, needs
precise multi-line formatting control, or mixes both — not as a blanket replacement for every
verbatim string in a codebase.
