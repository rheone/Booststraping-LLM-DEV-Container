# Write C# Version Skill

Scaffolds or extends a version-gated C# language-feature reference skill: one `references/` file
per C# version tier where the feature actually changed, each with a stated fallback to the
previous tier, plus `specialized/` files for patterns that cut across tiers. The workflow is in
[SKILL.md](SKILL.md).

```text
references/
  fact-verification-checklist.md    how to verify version/GA-date/feature-availability claims
  file-tree-and-naming.md           references/ vs specialized/ split, csharpN-<slug>.md naming
  maintenance-workflow.md           extending an existing versioned skill with a new tier
  portability-and-cross-referencing.md   soft-pointer rule for links to other skills

assets/templates/
  SKILL.md.tmpl
  README.md.tmpl
  reference-file.md.tmpl
  specialized-file.md.tmpl
```

## Worked examples

`csharp-extension-members` and `csharp-generics`, elsewhere in this repo's `skills/` folder, are
the pattern this skill scaffolds — read them for a full worked example if you have them
installed. This skill doesn't depend on them; see
[references/portability-and-cross-referencing.md](references/portability-and-cross-referencing.md)
for why every skill this tool produces treats sibling-skill links the same way.
