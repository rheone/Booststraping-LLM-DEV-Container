# Ordinalizing and truncating

## `Ordinalize()`

Appends the correct ordinal suffix to an integer (or a numeric string), handling the 11th/12th/13th
exceptions to the usual 1st/2nd/3rd pattern.

```csharp
1.Ordinalize();   // "1st"
2.Ordinalize();   // "2nd"
11.Ordinalize();  // "11th"
21.Ordinalize();  // "21st"
"3".Ordinalize(); // "3rd"
```

`Ordinalize()` produces the compact digit-plus-suffix form; use `ToOrdinalWords()` (see
[references/numbers-to-words.md](numbers-to-words.md)) when the value needs to be spelled out in
full words instead ("third" rather than "3rd").

## `Truncate()`

Shortens a string to a maximum length, appending a truncation indicator so the result signals it was
cut short rather than looking like a complete, shorter string by coincidence.

```csharp
"A long sentence that needs shortening".Truncate(10); // "A long...
```

By default, `Truncate()` counts the truncation indicator's own length against the limit and cuts at
a character boundary — pass a specific `ITruncator` to change where the cut happens:

```csharp
"A long sentence that needs shortening".Truncate(10, Truncator.FixedNumberOfWords);
// "A long..."  -- cuts on a whole-word boundary instead of mid-word

"A long sentence that needs shortening".Truncate(10, "…", Truncator.FixedLength);
// custom truncation string instead of the default "..."
```

- `Truncator.FixedLength` (the default) — cuts at an exact character count, which can land mid-word.
- `Truncator.FixedNumberOfWords` — treats the length as a word count instead of a character count,
  avoiding a truncation that cuts a word in half.
- `Truncator.FixedNumberOfCharacters` — an explicit alias for the character-counting behavior, for
  call sites that want to state the choice rather than rely on the default.

## Choosing a truncation length and strategy

Truncating for a UI label (a card title, a list row) generally wants `FixedNumberOfWords` or a
character count with headroom for the indicator, since a label cut mid-word reads as broken rather
than intentionally shortened. Truncating for a fixed-width backend field (a database column with a
hard length limit) wants `FixedLength` with the exact remaining character budget, since the
constraint is a byte/character count the stored value must not exceed regardless of word boundaries.
