# Migrating an Existing Codebase to Nullable Reference Types

Flipping `<Nullable>enable</Nullable>` on an established, multi-year codebase at the project level
in one commit routinely surfaces hundreds or thousands of warnings at once — every field, property,
parameter, and return type in every file is suddenly evaluated against a default (non-nullable)
that the code was never written against. This file covers staging that rollout incrementally
instead. It assumes the syntax from
[csharp8-nullable-reference-types.md](../references/csharp8-nullable-reference-types.md); nothing
here is new syntax, just a sequencing strategy for adopting existing syntax gradually.

## Basic: per-file opt-in before project-wide opt-in

```csharp
// LegacyReportGenerator.cs -- not yet migrated, no #nullable directive, inherits the
// project-level <Nullable> setting (disable, until the project-wide flip happens)

// OrderValidator.cs -- migrated file, opts in explicitly regardless of the project setting
#nullable enable

public class OrderValidator
{
    public bool Validate(Order? order, out string? error)
    {
        if (order is null)
        {
            error = "Order is required.";
            return false;
        }

        error = null;
        return true;
    }
}
```

Leaving the project-level `<Nullable>` setting at `disable` (or unset) while adding
`#nullable enable` to individual files lets a team migrate file-by-file, newest or highest-risk
files first, without a single all-at-once warning flood. Each migrated file's warnings are scoped
to that file alone — a caller in an unmigrated file passing `null` into `OrderValidator.Validate`
still compiles without warning, because the caller's own file is still oblivious.

## Basic: `warnings`/`annotations` as intermediate project-wide settings

```xml
<!-- Stage 1: turn on the ? syntax so newly-written code can use it, without
     yet turning on warnings for the whole (mostly unmigrated) codebase. -->
<PropertyGroup>
  <Nullable>annotations</Nullable>
</PropertyGroup>
```

```xml
<!-- Stage 2, once most files are annotated: flip to full enforcement. -->
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

`annotations` lets `?` be written and understood by the compiler (so a newly-written or freshly
migrated file can use `string?` correctly) without generating warnings project-wide for every
not-yet-migrated file's oblivious code. `warnings` is the inverse — useful briefly to see the full
scope of what enabling everywhere would surface, without yet letting `?` be written anywhere. Most
migrations move `disable → annotations` (adopt syntax incrementally) → `enable` (turn on
enforcement once coverage is high enough that the remaining warnings are a manageable, reviewable
list) rather than jumping straight to `enable`.

## Advanced: warning-as-error staged by severity, not all at once

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <!-- Promote only the highest-confidence nullable warnings to errors first --
       CS8600 (converting possible null to non-nullable) and CS8602 (dereference of
       possibly null reference) catch real bugs with very few false positives.
       Leave lower-confidence ones (e.g. CS8625 on a legacy null-literal assignment
       pattern) as warnings until more of the codebase is migrated. -->
  <WarningsAsErrors>$(WarningsAsErrors);CS8600;CS8602</WarningsAsErrors>
</PropertyGroup>
```

Promoting specific nullable warning codes to errors (rather than every `CS86xx`/`CS87xx` code at
once via a blanket `<Nullable>enable</Nullable>` plus a blanket `<WarningsAsErrors>`) lets a team
lock in the categories of warning they're confident are genuine bugs while continuing to migrate
the rest under advisory-only warnings. An `.editorconfig` `dotnet_diagnostic.CS8602.severity = error`
rule (scoped to a folder, if migrating directory-by-directory) achieves the same staging at
finer-than-project granularity than the MSBuild property alone allows.

## Advanced: suppressing a warning during migration without disabling analysis

```csharp
#nullable enable

public string LegacyFormat(string? input)
{
    // TODO(migration): input is genuinely nullable here per the legacy caller in
    // OrderProcessor.cs, not yet migrated. Suppressing narrowly instead of reaching
    // for #nullable disable, which would blind the analyzer to every other line below.
#pragma warning disable CS8604 // Possible null reference argument
    return string.Format(_template, input);
#pragma warning restore CS8604
}
```

`#pragma warning disable`/`restore` scoped to the specific line (paired with a `TODO` comment
naming why) keeps the rest of the file's analysis active during migration, unlike reaching for
`#nullable disable` on the whole file — which silences every warning in the file, including ones
unrelated to the specific known gap, and is easy to forget to revert once the underlying call site
is eventually fixed.

## Fallback

Every technique here assumes [C# 8.0](../references/csharp8-nullable-reference-types.md) is
available to migrate *to* — there's no meaningful "migrate to NRT" story on an older language
version, since the feature doesn't exist yet. A codebase stuck below C# 8.0 has nothing to stage;
see
[pre-csharp8-nullable-oblivious.md](../references/pre-csharp8-nullable-oblivious.md) for the
convention-based defensive-check pattern that's the only option there.
