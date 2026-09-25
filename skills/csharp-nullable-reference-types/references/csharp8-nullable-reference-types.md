# Nullable Reference Types (C# 8.0 / .NET Core 3.0, September 2019)

C# 8.0 shipped nullable reference types (NRT) as a purely compile-time, opt-in feature: `string?`
now means something different from `string`, the compiler runs static flow analysis over every
reference-typed variable to decide whether it's definitely non-null, possibly null, or unknown at
each point in the code, and it warns — never errors, by default — when that analysis finds a
mismatch. Nothing about this changes runtime behavior or IL; a `string?` and a `string` are the
identical CLR type, and a build with warnings ignored runs exactly as it did in C# 7. The feature
is off by default project-wide, so every codebase adopting it is an explicit opt-in.

## Syntax

```csharp
#nullable enable   // turn on both annotations (the ? syntax) and warnings for this file/region
#nullable disable  // oblivious context: back to pre-C#8 behavior for this file/region
#nullable restore  // revert to whatever the project-level setting says

public class Order
{
    public string CustomerNote { get; set; } = string.Empty; // non-nullable: must never be null
    public string? TrackingNumber { get; set; }               // nullable: null is a valid value
}
```

Project-wide, the equivalent switch is the `<Nullable>` MSBuild property:

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

`<Nullable>` accepts `enable`, `disable` (the C# 8.0-era default for existing projects), `warnings`
(annotations off, warnings on — useful mid-migration), and `annotations` (annotations on, warnings
off — lets a library expose `?` in its public API without forcing warnings on its own
not-yet-migrated implementation).

## Basic use case: annotations plus the null-forgiving operator

```csharp
#nullable enable

public class CustomerLookup
{
    private readonly Dictionary<int, string> _names = new();

    public string? TryGetName(int id) =>
        _names.TryGetValue(id, out string? name) ? name : null;

    public string GetNameOrThrow(int id)
    {
        string? name = TryGetName(id);
        if (name is null)
        {
            throw new KeyNotFoundException($"No customer {id}.");
        }

        return name; // flow analysis narrows: after the null check and throw, name is non-null here
    }

    public string GetKnownName(int id)
    {
        // the caller has independent knowledge the compiler can't see (id came from a
        // just-populated dictionary) -- ! tells the compiler to trust it, suppressing the warning
        return TryGetName(id)!;
    }
}
```

`!`, the null-forgiving operator, changes nothing at runtime — it's a compile-time-only annotation
telling the analyzer "treat this expression as non-null from here," and a wrong `!` still throws
`NullReferenceException` at the dereference site exactly as if NRT didn't exist. It is a promise to
the compiler, not a guard.

## Advanced use case: flow-analysis narrowing and the `notnull` generic constraint

```csharp
#nullable enable

public class Cache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _store = new();

    public bool TryGet(TKey key, out TValue? value) => _store.TryGetValue(key, out value);

    public void Set(TKey key, TValue value) => _store[key] = value;
}

public string Summarize(string? input)
{
    if (input is not null && input.Length > 0)
    {
        return input.ToUpperInvariant(); // narrowed non-null by the && chain, not just a single check
    }

    return input?.Length == 0 ? "(empty)" : "(none)";
}
```

`where TKey : notnull` matches `Dictionary<TKey, TValue>`'s own constraint (a key can never be
`null`) — it accepts any non-nullable reference type or any value type (nullable or not; `int?` as
`TKey` still satisfies `notnull` in an oblivious context, but produces a warning in a
nullable-enabled one). Flow-analysis narrowing follows `&&`/`||` chains, ternaries, pattern
matches (`is not null`, `is { }`), and early returns — not just a bare `if (x != null)` block —
tracking the null state of a local or parameter across the whole method body.

Two related attribute families exist to extend this same flow analysis beyond what plain signature
types can express — one lets a method's body tell the analyzer facts about its own parameters and
return value (a `TryXxx` pattern, a helper that always throws), and the BCL ships the ones the
compiler recognizes from this same release; if you need that level of detail, look for coverage of
the nullable-analysis attribute catalog specifically. This skill covers the language feature they
extend, not the attribute catalog itself.

## Requirements and restrictions

- Enabling `<Nullable>enable</Nullable>` on an existing, unannotated codebase typically surfaces a
  large number of warnings at once — see the migration-focused pattern in this skill's
  `specialized/` folder for staging that incrementally instead of all at once.
- Warnings are warnings, not errors, by default — a project can ship with nullable warnings present
  unless `<WarningsAsErrors>` (or a specific `CS86xx`/`CS87xx` code) is configured to fail the build.
- The `notnull` constraint only has effect in a nullable-enabled context; in an oblivious
  (`#nullable disable`) file or project, violating it produces no diagnostic at all.
- Flow-analysis narrowing is intra-procedural and resets at method boundaries — the compiler
  doesn't remember that a private field was null-checked three calls up the stack; each method's
  analysis starts fresh from the field's declared nullability.

## Fallback

Below C# 8.0 / .NET Core 3.0, none of this exists: no `?` on reference types, no `#nullable`
directive, no `<Nullable>` MSBuild property, no `!` operator, no `notnull` constraint. Fall back to
[pre-csharp8-nullable-oblivious.md](pre-csharp8-nullable-oblivious.md) — defensive `if (x == null)`
checks enforced by convention, with no compiler-verified nullability contract anywhere in a method
signature.
