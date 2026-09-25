---
name: write-csharp-version-skill
description: Scaffold or extend a version-gated C# language-feature reference skill — per-C#-version reference files with stated fallback chains, specialized cross-cutting pattern files, and a SKILL.md routing table. Modeled on this repo's csharp-extension-members and csharp-generics skills.
license: Apache-2.0
user-invocable: true
disable-model-invocation: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Write C# Version Skill

Builds a **tier**-based reference skill for a C# language feature: one file per C# version
where the feature's syntax or availability actually changed, each stating what it falls back to
on an older target, plus a `specialized/` set of cross-cutting patterns that apply across tiers.
`csharp-extension-members` and `csharp-generics` are worked examples of the output, if you have
them installed — not a dependency of this skill.

## Mode

- **New skill**, for a feature with no version-gated skill yet — follow Steps 1–7 below.
- **Extend an existing** version-gated C# skill with a newly shipped C# version's tier — Steps 1–2
  below still apply, then jump to [references/maintenance-workflow.md](references/maintenance-workflow.md)
  for the rest; it's a different enough sequence to earn its own file rather than branching inline.

## Steps (new skill)

1. **Scope the feature and its version range.** Establish, as a plain list:
   - The earliest C# version where the feature exists in *any* form — including "never before
     C#N," itself a valid first tier (a pre-history file naming the pre-feature workaround
     pattern, the way `csharp-extension-members` documents .NET Framework 1.0–2.0's static-helper
     substitute for extension methods).
   - Every later C# version where the feature's syntax, constraints, or availability actually
     changed — a tier exists only where behavior changed, not one file per C# release
     indiscriminately. Most C# versions touch most features not at all.
   - The current latest C# version, stable or RC, as of today.

   Completion: a version → C# number → "what changed" list, oldest to newest, nothing skipped,
   nothing invented.

2. **Verify every fact from Step 1 against a current search.** Hard requirement: do not write a
   version number, GA/RC date, or feature-availability/deferral claim from memory alone — a
   language model's training data on C# version timelines goes stale within months of each new
   release, and a version-gated skill's entire value is getting the gating right. Full method,
   source-preference order, and how to handle the "still an RC" case in
   [references/fact-verification-checklist.md](references/fact-verification-checklist.md).
   Completion: every claim in Step 1's list has a search result from *this* session backing it.

3. **Design the file tree.**
   - `references/csharpN-<slug>.md` — one per tier from Step 1, oldest to newest.
   - `specialized/<pattern>.md` — one per cross-cutting pattern spanning more than one tier
     (constraint/rule tables, a design-your-own-X deep dive, the feature used as a
     test-authoring tool, resolution/coexistence rules between old and new syntax, ...).
   - `SKILL.md` and `README.md` at the skill root.

   Naming convention and the `references/` vs. `specialized/` split rule:
   [references/file-tree-and-naming.md](references/file-tree-and-naming.md).
   Completion: every tier from Step 1 has exactly one reference file; every specialized pattern
   you can already name has a stub path.

4. **Write each reference file** from
   [assets/templates/reference-file.md.tmpl](assets/templates/reference-file.md.tmpl): syntax,
   basic use case, advanced use case, requirements/restrictions, and a **Fallback** section
   naming the previous tier's file by path. The fallback chain is the whole point of version
   gating — a file with no Fallback section, or one that fails to name an actual sibling file, is
   not done.

5. **Write each specialized file** from
   [assets/templates/specialized-file.md.tmpl](assets/templates/specialized-file.md.tmpl). One
   rule applies specifically to any testing-themed specialized file: it covers the feature used
   **as a test-authoring tool** — fluent assertions, builders, mock/stub helpers — never tests
   that validate this skill's own syntax examples. (This is not a hypothetical mistake: an
   earlier draft of `csharp-extension-members` got it backwards and had to be reworked after
   review.)

6. **Write `SKILL.md`** from [assets/templates/SKILL.md.tmpl](assets/templates/SKILL.md.tmpl): a
   quick-start example that compiles on every tier, a routing table (target → C# version →
   reference file), and the specialized-pattern list. **Write `README.md`** from
   [assets/templates/README.md.tmpl](assets/templates/README.md.tmpl), mirroring the same file
   tree as plain text.

7. **Audit portability.** Read
   [references/portability-and-cross-referencing.md](references/portability-and-cross-referencing.md)
   and apply it to every link this skill makes to any other skill, whichever repo it lives in.
   Completion: no file's core content depends on a sibling skill's link actually resolving.

## Skill mechanics reminder

If `write-a-skill` and/or `writing-for-agents` are installed, prefer their fuller treatment of
frontmatter, invocation choice, and progressive disclosure over this section — it exists only so
this skill works without them:

- Frontmatter shape: `name`, `description`, `license`, `user-invocable: true`,
  `metadata: { author, version }`. Match whatever sibling skills already exist in the target
  repo's `skills/` folder for the exact fields in use there.
- Model-invoked (default) vs. user-invoked (`disable-model-invocation: true`): a C#
  version-gated reference skill is the kind an agent should reach for on its own while working in
  the codebase, so model-invocation is normally right for the skill you're building — write its
  `description` with the language/feature keywords an agent's own reasoning would already be
  using (this builder skill itself is the exception: it fires rarely enough to justify
  user-invoked).
- Progressive disclosure: `SKILL.md` stays a router — quick start, routing table, specialized-file
  list. Version-gating detail belongs in `references/`, never inlined into `SKILL.md` itself.
