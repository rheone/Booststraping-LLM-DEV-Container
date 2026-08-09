---
name: legacy-dotnet-feature-mapper
description: Reverse-engineers and documents a legacy .NET Framework 4.8 / .NET 5 WebForms + TSQL application into a faithful, citable feature-by-feature map (business rules, permissions, happy/failure paths, DB behavior, Mermaid diagrams) using static analysis only - never by running or compiling the code. Use this whenever the user wants to document, map, reverse-engineer, inventory, or "understand" an old/legacy/undocumented .NET WebForms codebase before a rewrite or migration, wants a feature inventory of a solution, wants business rules extracted from stored procedures/triggers, or mentions preparing documentation as a reference for a future rewrite team. Also trigger for requests to run this process "AFK", unattended, or "until done."
metadata: 
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
license: Apache-2.0
user-invocable: true
---

# Legacy .NET Feature Mapper

Produces an as-is, citable map of what an old .NET WebForms + TSQL application actually does today; not what it should do, not rewrite advice. The output is a reference future developers use to make sure no feature/business rule/permission is silently lost during a rewrite.

**Everything here is discovered via static analysis (reading source, not executing it).** Never run, build, or compile the application or any script to "check" behavior. If something can't be determined by reading code/SQL, it must be flagged, not guessed.

## Required inputs before starting

Every invocation needs, explicitly, from the user (never assume or auto-discover these):
1. **In-scope `.csproj` project paths** - the specific projects to investigate. Do not wander into other projects in the solution.
2. **DB object definitions folder** - path to the `.sql` scripts (schema/proc/trigger/view/function definitions).
3. **Output docs path** - where to write everything. Ask if not given; do not default to a guessed location.

If any of these three are missing, ask before doing any analysis. See `references/kickoff-checklist.md` for the exact questions to ask when inputs are incomplete or ambiguous.

## The two phases

Always determine which phase the user wants (or run Discovery first if the output docs path has no index yet). Full details for each phase are in the reference files - read the relevant one before starting work.

### Phase 1 - Discovery
Read `references/discovery-phase.md`. Scans the in-scope projects only, for static entry points (aspx pages/code-behind, MVC/API controllers, .ashx handlers, aspx, WCF/web services). Produces/updates the master index (`index.md`) with one row per discovered feature: name, entry point(s), status. Does not write full feature docs.

### Phase 2 - Deep-dive
Read `references/deep-dive-phase.md`. Fully documents one or more features from the index: traces entry point → business logic → DB, writes the feature doc using the template in `references/feature-doc-template.md`, and creates/links shared-component docs for reused code or DB objects per `references/shared-components.md`.

Default batch behavior: one feature at a time unless the user names specific entry points to batch, or is running in AFK mode (see below).

## Cross-cutting rules (apply in both phases)

- **Static analysis only.** Trace code by reading it - call sites, method bodies, `.aspx`/`.ascx` markup and code-behind, config files, `.sql` definitions. No execution, no debugger, no "let's just run it and see."
- **TSQL is traced deep.** A large share of business logic lives in the database. When a feature touches a stored procedure, follow it into any views, functions, triggers, and computed columns it touches, and flag side effects (e.g., a trigger writing to another table). See `references/tsql-analysis.md`.
- **Every claim is cited.** File path + line number (or SQL object + relevant line) alongside a confidence tag: `verified in code`, `inferred from naming`, or `unverified assumption`. No non-cited claims in a feature doc. See `references/citation-and-confidence.md`.
- **Permissions:** look for role checks in code (`User.IsInRole`, roles config, membership/role providers) first. Where authorization isn't obviously code-enforced, investigate further (DB-driven permission tables, config-driven rules) and document what you find - including "no visible authorization check found" as a finding in its own right.
- **Shared code and DB objects** used by more than one feature get their own doc under `shared-components/` and are linked from every feature that uses them - never re-explained inline in each feature doc.
- **Diagrams are earned, not default.** Add a Mermaid diagram (flowchart, state, or sequence - whichever fits) only when it adds real value beyond the prose, e.g., a multi-step workflow with branching states or a non-obvious call chain. Skip diagrams for simple CRUD-style features.
- **This is a map of the past, not advice for the future.** Do not include modernization recommendations, "you should refactor this," or opinions about the new framework. Stick to what the old app does and how.

## Output structure

```
<output-docs-path>/
├── index.md                  # master status index - see references/index-schema.md
├── system-overview.md        # app-wide map of how features relate - updated as features are documented
├── features/
│   └── <feature-slug>.md     # one per feature - see references/feature-doc-template.md
└── shared-components/
    └── <component-slug>.md   # one per shared class/proc/view/etc. reused across features
```

## Updating an already-documented feature

If a feature doc already exists and the user asks to improve/update it: **ask whether to fully regenerate or incrementally update**, every time - do not assume based on a prior answer. Exception: in AFK mode, default to incremental update automatically without asking, and log that decision (see below).

## AFK / unattended / until done mode 

If the user asks to run this unattended, "AFK," "until done," or similar:

1. **Get the endpoint condition explicitly before starting** - e.g., "all features in the index reach `documented`" or a specific named/counted target list. Do not assume; ask if it wasn't stated in this message.
2. Read `references/afk-mode.md` for the full run loop, logging format, and stop conditions.
3. Work through features without pausing for confirmation between them. When a feature is partially documented, default to incremental update automatically and log the decision in the AFK log - do not ask.
4. Stop and report back when: the endpoint condition is met, OR a hard blocker is hit (e.g., a referenced `.csproj` or SQL folder path doesn't exist, scope is ambiguous in a way that can't be resolved by convention). Hard blockers pause AFK mode and surface the specific issue - don't guess past them.
5. Keep the index and AFK log updated continuously (not just at the end) so a run can be inspected or resumed if interrupted.

## Confidence tags - quick reference

| Tag | Meaning |
|---|---|
| `verified in code` | Directly observed in source/SQL, unambiguous |
| `inferred from naming` | Reasonable inference from names/conventions, not directly confirmed by logic |
| `unverified assumption` | Best guess where static analysis couldn't resolve it (e.g., dynamic SQL, reflection, runtime-only config) - must include a one-line reason it couldn't be verified |

Full details, templates, and phase-by-phase instructions are in `references/`. Read the relevant file(s) before producing output rather than relying on this summary alone.
