# C# Skill Authoring

The meta-skill that builds every other skill in this repository's C#/.NET catalog. Point it at a
language feature, a design pattern, an architectural style, a coding convention, a third-party
library, or a platform API, and it scaffolds a complete, consistently structured skill for it.

## When to reach for it

- Adding a new skill to this repository's C#/.NET catalog and want it to follow the same
  conventions (naming, file structure, testing coverage, tone) as every other skill here.
- Unsure whether something should be named `csharp-*` or `dotnet-*`.
- Extending an existing version-gated language-feature skill with a newly shipped C# version's
  tier.

## Using it

This skill is user-invoked: type `/csharp-skill-authoring` (or ask for it by name) rather than
expecting it to fire on its own; it's rare enough to use that an always-loaded description would
cost more than it saves.

## What it covers

| Topic | Reference |
| --- | --- |
| Deciding `csharp-*` vs. `dotnet-*`, and what to do when it's ambiguous | [references/naming-decoder.md](references/naming-decoder.md) |
| Building a skill organized by C#-version tiers (a language feature) | [references/version-gated-workflow.md](references/version-gated-workflow.md) |
| Building a skill organized by task (a library, pattern, architecture, or convention) | [references/general-skill-workflow.md](references/general-skill-workflow.md) |
| Adding a newly shipped C# version's tier to an existing version-gated skill | [references/maintenance-workflow.md](references/maintenance-workflow.md) |
| Verifying version, license, and availability claims before writing them down | [references/fact-verification-checklist.md](references/fact-verification-checklist.md) |
| Keeping every skill self-contained, with no reference to any sibling | [references/insularity.md](references/insularity.md) |
| The present-tense, no-delta-narrative voice every skill file is written in | [references/writing-style.md](references/writing-style.md) |
| Requiring a testing reference on every skill, and adapting it to the subject | [references/testability.md](references/testability.md) |
| The shared human-facing README shape every skill in this repo uses | [references/readme-template.md](references/readme-template.md) |

## Example prompts

- "Build me a skill for the Polly resilience library."
- "I want a skill for the Visitor pattern in C#, same style as the other pattern skills."
- "C# 15 just shipped a new tier for the union feature. Extend that skill."
