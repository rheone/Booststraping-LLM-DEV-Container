---
name: csharp-skill-authoring
description: Scaffold or extend a skill for a C# language feature, design pattern, architectural style, or coding convention (`csharp-*`), or for a .NET/BCL API, third-party library, dev tool, or testing framework (`dotnet-*`). Applies the naming decoder, the version-gated tier workflow for language features, the task-organized workflow for everything else, and the insularity/writing-style/testability rules every produced skill follows.
license: Apache-2.0
user-invocable: true
disable-model-invocation: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0

---

# C# Skill Authoring

You are the meta-skill that creates other skills for C#, .NET, and dotnet tooling. Every file you
produce is an instruction set for an agent — you never reason about, name, or compare against any
other skill's existence except where the rules below explicitly allow it.

## Step 0: name it

Apply the decoder in [references/naming-decoder.md](references/naming-decoder.md):

| Type | Prefix | Covers |
| --- | --- | --- |
| Language | `csharp-*` | C# language/compiler features |
| .NET/BCL | `dotnet-*` | .NET/BCL/platform functionality |
| Library | `dotnet-*` | Specific third-party .NET libraries/packages |
| Pattern | `csharp-*` | Design patterns and their C# implementation |
| Architecture | `csharp-*` | Architectural approaches as applied to C#/.NET |
| Convention | `csharp-*` | Coding practices/conventions |
| Tooling | `dotnet-*` | C#/.NET development tools |
| Testing | `dotnet-*` | Testing frameworks and .NET testing practices |

If the subject doesn't resolve cleanly against this table, read
[references/naming-decoder.md](references/naming-decoder.md)'s worked ambiguous cases. If it still
doesn't resolve, **stop and ask the person commissioning the skill** which prefix and name to use
— never guess and proceed.

## Step 1: pick the workflow

- **Version-gated** (`csharp-*` only): the subject's organizing axis is genuinely "what changed at
  each C# version" — a language feature with syntax or availability that shifted across releases.
  Follow [references/version-gated-workflow.md](references/version-gated-workflow.md).
- **General** (everything else — `dotnet-*` always, plus `csharp-*` patterns/architecture/
  conventions): organized by task or topic instead. Follow
  [references/general-skill-workflow.md](references/general-skill-workflow.md).

Both workflows route through the same three standing rules, applied to every file you write:

- **Insularity** — [references/insularity.md](references/insularity.md). You reason about this
  skill's own job only; you don't compare it to another skill or assume one is present or absent
  unless the person commissioning it explicitly stated a dependency.
- **Writing style** — [references/writing-style.md](references/writing-style.md). Present tense,
  active voice, second person; state the current instruction, never its history.
- **Testability** — [references/testability.md](references/testability.md). Every skill ships a
  testing reference — how to test it plus the most likely use cases — adapted per that file's
  guidance when the subject has no independent runtime behavior or is itself testing
  infrastructure.

## Skill mechanics

- Frontmatter shape: `name`, `description`, `license`, `user-invocable: true`, `metadata: {
  author, version }`.
- Model-invoked (default) vs. user-invoked (`disable-model-invocation: true`): a skill an agent
  should reach for on its own while working in the codebase — which is the normal case for the
  skills you produce — gets model-invocation; write its `description` with the keywords an agent's
  own reasoning would already be using. Reserve user-invoked for something that fires rarely enough
  a description would only add always-loaded cost without earning it (this authoring skill itself
  is the exception, for exactly that reason).
- Progressive disclosure: `SKILL.md` stays a router — quick start, routing table, file list.
  Version-gating or task detail belongs in `references/`, never inlined into `SKILL.md` itself.
