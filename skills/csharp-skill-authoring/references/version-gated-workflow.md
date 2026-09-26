# Version-Gated Workflow

For a `csharp-*` skill whose organizing axis is version-introduced tiers: syntax or availability
that changed across C# releases (a language feature, most BCL surfaces tightly coupled to language
syntax). If the subject doesn't have that shape, use
[general-skill-workflow.md](general-skill-workflow.md) instead.

Unless otherwise explicitly stated, target .NET Standard 2.0 (C# 7.3), .NET 9 (C# 13) through the
latest C# version.

## New skill

1. **Scope the feature and its version range.** Establish, as a plain list:
   - The earliest C# version where the feature exists in *any* form — including "never before
     C#N," itself a valid first tier (a pre-history file naming the pre-feature workaround pattern
     — e.g. a static-helper substitute for a feature that later became syntax).
   - Every later C# version where the feature's syntax, constraints, or availability actually
     changed — a tier exists only where behavior changed, not one file per C# release
     indiscriminately. Most C# versions touch most features not at all.
   - The current latest C# version, stable or RC, as of today.

   Completion: a version → C# number → "what changed" list, oldest to newest, nothing skipped,
   nothing invented.

2. **Verify every fact from Step 1 against a current search.** Do not write a version number,
   GA/RC date, or feature-availability/deferral claim from memory alone — a language model's
   training data on C# version timelines goes stale within months of each new release, and a
   version-gated skill's entire value is getting the gating right. Full method, source-preference
   order, and how to handle the "still an RC" case in
   [fact-verification-checklist.md](fact-verification-checklist.md). Completion: every claim in
   Step 1's list has a search result from *this* session backing it.

3. **Design the file tree.**
   - `references/csharpN-<slug>.md` — one per tier from Step 1, oldest to newest.
   - `specialized/<pattern>.md` — one per cross-cutting pattern spanning more than one tier
     (constraint/rule tables, a design-your-own-X deep dive, the feature used as a test-authoring
     tool, resolution/coexistence rules between old and new syntax, ...).
   - `SKILL.md` and `README.md` at the skill root.

   Naming convention and the `references/` vs. `specialized/` split rule:
   [file-tree-and-naming.md](file-tree-and-naming.md). Completion: every tier from Step 1 has
   exactly one reference file; every specialized pattern you can already name has a stub path.

4. **Write each reference file** from
   [assets/templates/reference-file.md.tmpl](../assets/templates/reference-file.md.tmpl): syntax,
   basic use case, advanced use case, requirements/restrictions, and a **Fallback** section naming
   the previous tier's file by path. The fallback chain is the whole point of version gating — a
   file with no Fallback section, or one that fails to name an actual sibling file, is not done.

5. **Write each specialized file** from
   [assets/templates/specialized-file.md.tmpl](../assets/templates/specialized-file.md.tmpl).

6. **Write the testing reference.** Every skill gets one — follow
   [testability.md](testability.md)'s shape (how to test it, plus the most likely use cases) and
   its guidance for adapting the content when the feature has no independent runtime behavior.

7. **Write `SKILL.md`** from
   [assets/templates/SKILL.md.tmpl](../assets/templates/SKILL.md.tmpl): a quick-start example that
   compiles on every tier, a routing table (target → C# version → reference file), and the
   specialized-pattern list. **Write `README.md`** from
   [assets/templates/README.md.tmpl](../assets/templates/README.md.tmpl), mirroring the same file
   tree as plain text — use a markdown table for any file listing that carries a per-file
   description; reserve a plain list for a listing that's names only.

8. **Apply insularity.** Read [insularity.md](insularity.md) and apply it to every place any file
   in this skill names another skill or compares itself to one, whichever repo it lives in.
   Completion: no file names or leans on a skill it doesn't have an explicit, stated dependency on.

9. **Apply the writing style.** Read [writing-style.md](writing-style.md) and check every file you
   wrote against it: present tense, active voice, second person, no delta-narrative.

## Extending an existing version-gated skill

Steps 1–2 above still apply, then jump to
[maintenance-workflow.md](maintenance-workflow.md) for the rest; it's a different enough sequence
to earn its own file rather than branching inline.
