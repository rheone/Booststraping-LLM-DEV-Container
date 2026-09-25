# File Tree and Naming

## `references/` vs. `specialized/`

- **`references/`** is strictly version-gated: each file's content is superseded or extended by
  the next tier's file, and each states what it falls back to on an older target. One file per
  tier, chronological.
- **`specialized/`** holds patterns usable at whatever tier they apply to — a constraint
  reference table spanning every version, a design-your-own-variant-interface deep dive, the
  feature used as a test-authoring tool. Individual examples inside a specialized file may still
  need a specific C# version, but the file itself isn't *itself* one rung of the fallback chain —
  it's reached from whichever reference tier(s) are relevant, not superseded by the next one.

A rule of thumb: if the next C# version's file would need to say "everything in the previous
file, plus X," it's a `references/` tier. If a topic instead spans several tiers sideways
(a table row per tier, a pattern usable since C# 2.0 that gets one more trick in C# 11), it's
`specialized/`.

## Naming reference files

`csharpN-<slug>.md`, where `N` is the version that introduced the tier's headline change and
`<slug>` names *what changed*, not the whole feature again:

- Good: `csharp13-ref-struct-generics.md` (the tier's whole content is the `allows ref struct`
  addition).
- Bad: `csharp13-generics.md` (restates "generics," which every file in the skill is already
  about — the slug should distinguish this tier from its neighbors at a glance).

When a version adds nothing to the feature's domain, it gets no file — don't create
`csharp12-generics.md` just to note "nothing changed here." A gap in the version sequence is
itself informative; a reader scanning `references/` from csharp8 straight to csharp11 correctly
infers nothing relevant shipped in C# 9 or 10.

## Naming specialized files

`<pattern-name>.md`, descriptive and reasonably short — no version number, since these aren't
tiers. Group by the pattern's own name, not by which reference file it's "attached to"
(`generic-math-numeric-abstractions.md`, not `csharp11-generic-math-deep-dive.md`).

## The pre-history tier

When a feature has a meaningful "how did people do this before the feature existed" answer, write
it as the *first* reference file, not a skipped starting point. It's genuinely useful, portable
content on its own — the workaround pattern a project stuck on an old target still needs — not
mere scene-setting. `csharp-extension-members`' `pre-csharp3-no-extensions.md` (the static-helper
substitute for extension methods, for .NET Framework 1.0–2.0) is the template shape: what the
absence looks like, the workaround pattern, and how to port forward once the real feature becomes
available.

Skip the pre-history tier only when there genuinely is no meaningful "before" — a feature that
replaces nothing and has no natural workaround (rare; most C# features have *some* older idiom
they superseded).
