# Numbers to words

## `ToWords()`

Spells out an integer as English (or the active culture's) words.

```csharp
1337.ToWords();   // "one thousand three hundred and thirty-seven"
0.ToWords();      // "zero"
(-42).ToWords();  // "negative forty-two"
```

`ToWords()` and `int.Humanize()` produce the same output for a plain integer — reach for whichever
name reads more clearly at the call site; `ToWords()` communicates the intent ("spell this number
out") more directly when the surrounding code isn't already using `Humanize()` for other types.

## `ToOrdinalWords()`

Spells out an integer as an ordinal in words, rather than the digit-plus-suffix form `Ordinalize()`
produces (see [references/ordinalize-and-truncate.md](ordinalize-and-truncate.md)).

```csharp
1.ToOrdinalWords();  // "first"
21.ToOrdinalWords(); // "twenty-first"
```

## Grammatical gender for languages that need it

For a culture whose ordinal/cardinal word forms vary by grammatical gender (many Romance and Slavic
languages), an overload accepts a `GrammaticalGender` value to select the correct form rather than
defaulting to one arbitrarily:

```csharp
1.ToOrdinalWords(GrammaticalGender.Feminine, new CultureInfo("ru-RU"));
```

English has no grammatical gender distinction for these forms, so the gender parameter has no
visible effect on English output — pass it explicitly only when targeting a culture where it
actually changes the result; see [references/localization.md](localization.md) for how the active
culture is selected.

## When to reach for `ToWords()` instead of formatting a number normally

Spelled-out numbers read naturally in prose-style UI text ("You have three new messages") and legal/
financial documents that conventionally spell out small numbers, but read awkwardly for anything a
user needs to scan quickly or compare (a table column, a price, a count next to a filter control) —
use plain numeric formatting (`ToString("N0")`, a bound numeric display) for those instead, and
reserve `ToWords()`/`Humanize()` for genuinely sentence-shaped output.
