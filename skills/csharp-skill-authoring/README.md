# C# Skill Authoring

Scaffolds or extends a skill for a C# language feature, design pattern, architectural style, or
coding convention (`csharp-*`), or for a .NET/BCL API, third-party library, dev tool, or testing
framework (`dotnet-*`). The naming decoder and workflow choice are in [SKILL.md](SKILL.md).

**`references/`** — one file per rule or workflow, not per C# version

| File | Covers |
| --- | --- |
| `naming-decoder.md` | the `csharp-*`/`dotnet-*` decision table, worked ambiguous cases, when to ask instead of guessing |
| `version-gated-workflow.md` | building a `csharp-*` skill organized by C#-version tiers |
| `general-skill-workflow.md` | building a `dotnet-*` or pattern/architecture/convention `csharp-*` skill organized by task |
| `maintenance-workflow.md` | extending an existing version-gated skill with a newly shipped C# version's tier |
| `fact-verification-checklist.md` | verifying version, GA/RC date, and license claims against a current search |
| `file-tree-and-naming.md` | `references/` vs. `specialized/` split rule and file-naming conventions for version-gated skills |
| `insularity.md` | the rule against comparing to, or naming, another skill without an explicit stated dependency |
| `writing-style.md` | present tense, active voice, second person, no delta-narrative |
| `testability.md` | when and how a skill ships a testing reference and example use cases |

**`assets/templates/`** — one template per version-gated file type

| File | Produces |
| --- | --- |
| `SKILL.md.tmpl` | a version-gated skill's `SKILL.md` |
| `README.md.tmpl` | a version-gated skill's `README.md` |
| `reference-file.md.tmpl` | one `references/csharpN-<slug>.md` tier file |
| `specialized-file.md.tmpl` | one `specialized/<pattern>.md` file |
