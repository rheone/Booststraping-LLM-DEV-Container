# More Accurate Flow Analysis and Nullable-by-Default Projects (C# 10 / .NET 6, November 2021)

C# 10 shipped two changes relevant here, neither of which is new syntax: the compiler's
definite-assignment and null-state analysis got measurably more accurate at recognizing patterns it
previously flagged incorrectly (fewer false-positive warnings, not a new warning category), and —
separately, at the tooling/SDK level rather than the language level — new projects created from the
.NET 6 SDK templates set `<Nullable>enable</Nullable>` by default for the first time, where every
earlier SDK version had defaulted new projects to nullable-oblivious. A C# 9.0-targeting project
that already opted in sees no behavior change from the language side; the visible difference is
that opting in is no longer something a new project has to do by hand.

## Syntax

No new syntax — this tier is a compiler-accuracy and SDK-default change, not a language addition.

```xml
<!-- .NET 6+ SDK project templates generate this automatically; earlier SDKs generated
     no <Nullable> element at all, defaulting to disabled. -->
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

## Basic use case: flow analysis that previously over-warned

```csharp
#nullable enable

public string Format(string? left, string? right)
{
    if (left is null && right is null)
    {
        return "(both empty)";
    }

    // Before C# 10, some equivalent multi-branch patterns here could still warn on left/right
    // even though every reachable path had already ruled out both being null. C# 10's more
    // accurate analysis recognizes this shape without needing a rewritten, more awkward
    // condition or a null-forgiving ! to silence a false positive.
    return $"{left ?? "?"} / {right ?? "?"}";
}
```

This tier is intentionally light on "new" examples — its value is fewer false-positive warnings on
code that already looked like the C# 8.0/9.0 examples elsewhere in this skill, not a new pattern to
write differently.

## Advanced use case: a new project starts nullable-enabled

```csharp
// dotnet new console --framework net6.0 (or later) generates Program.cs plus a .csproj
// with <Nullable>enable</Nullable> already present -- no manual opt-in step, and no
// silent oblivious-context gap between "new project" and "nullable turned on."
```

For a team standardizing on NRT across all new work, this removes a step that was easy to forget
under .NET 5 and earlier: the SDK template previously left `<Nullable>` unset, silently defaulting
every fresh project to the oblivious pre-C#-8 behavior until someone added the property by hand.

## Requirements and restrictions

- This is an SDK-template default, not a compiler behavior gated by `<LangVersion>` — a .NET 6+ SDK
  used to build a project explicitly pinned to an older `<LangVersion>` still generates a
  `<Nullable>enable</Nullable>` project file, but a project's own explicit `<Nullable>` setting
  (present from an earlier scaffold, or set by hand) always overrides the template default; this
  only affects brand-new projects, not existing ones.
- "More accurate flow analysis" is not a fixed, enumerable feature list the way `T?` or `!` is — it
  describes an ongoing category of Roslyn bug fixes to the null-state analyzer that shipped with
  this compiler version, not new syntax or a new attribute.

## Fallback

There is no fallback needed for the flow-analysis accuracy improvement itself — it only removes
false-positive warnings, so code written for
[csharp9-unconstrained-generic-nullability.md](csharp9-unconstrained-generic-nullability.md) or
[csharp8-nullable-reference-types.md](csharp8-nullable-reference-types.md) still compiles and warns
the same way (or more accurately) on this tier. On an SDK older than .NET 6, a new project starts
nullable-oblivious by default; add `<Nullable>enable</Nullable>` to the `.csproj` by hand to opt in,
as described in that C# 8.0 tier.
