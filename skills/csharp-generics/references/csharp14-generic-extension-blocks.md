# Generic Extension Blocks (C# 14 / .NET 10, C# 15 / .NET 11)

C# 14 (.NET 10, GA November 2025) lets an `extension(...)` block declare its own type
parameter, shared by every member inside the block — a distinct generics mechanism from a
classic generic extension method, where each method repeats its own `<T>`.

```csharp
public static class ReadOnlyListExtensions
{
    extension<T>(IReadOnlyList<T> items) where T : IComparable<T>
    {
        public T? Max() => items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) >= 0 ? a : b);
        public T? Min() => items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) <= 0 ? a : b);
    }
}
```

This skill covers generics; the extension-member mechanics themselves (instance vs. static
blocks, properties, operators, IL compatibility with classic extension methods) live in the
sibling [csharp-extension-members](../../csharp-extension-members/SKILL.md) skill —
specifically [references/csharp14-extension-members.md](../../csharp-extension-members/references/csharp14-extension-members.md)
and [specialized/generic-extension-members.md](../../csharp-extension-members/specialized/generic-extension-members.md),
which has the full generic-block treatment including static generic members
(`extension<T>(T) where T : IParsable<T>`) and worked constraint examples.

## What's new for generics specifically

- The type parameter and its constraints are declared **once per block**, not once per member —
  the ergonomic difference from classic generic extension methods repeating `<T> where T : ...`
  on every method.
- Constraints on an extension block's type parameter compose with every constraint kind covered
  elsewhere in this skill: `allows ref struct` (C# 13), `notnull` (C# 8), `unmanaged` (C# 7.3),
  and interface constraints referencing `static abstract` members (C# 11 generic math) all work
  inside an `extension<T>(...)` block exactly as they do on an ordinary generic method.

## C# 15 / .NET 11 status

C# 15 (.NET 11, RC1 as of September 2026; GA expected November 2026) adds extension **indexers**
to the block syntax (see the sibling skill's
[csharp15-extension-indexers.md](../../csharp-extension-members/references/csharp15-extension-indexers.md))
but introduces no new generics feature of its own. C# 15 also ships union types and closed
hierarchies; both support generic case types, documented in the
[csharp-union skill](../../csharp-union/SKILL.md) rather than here.

## Fallback

Below C# 14, write a classic generic extension method per member instead of one shared block —
repeat `<T>` and its constraints on each method. See
[references/csharp2-generics-fundamentals.md](csharp2-generics-fundamentals.md) for the classic
generic-method shape and
[csharp-extension-members' references/csharp3-extension-methods.md](../../csharp-extension-members/references/csharp3-extension-methods.md)
for the `this`-parameter mechanics that combine with it.
