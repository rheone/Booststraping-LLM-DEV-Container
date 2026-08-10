---
name: legacy-dotnet-feature-mapper
description: Reverse-engineers and documents a legacy .NET Framework 4.8 / .NET 5 WebForms + TSQL application into a faithful, citable feature-by-feature map (business rules, permissions, happy/failure paths, DB behavior, Mermaid diagrams) using static analysis only - never by running or compiling the code. Every generated doc is machine-verified against a template manifest before it can be marked complete. Use this whenever the user wants to document, map, reverse-engineer, inventory, or "understand" an old/legacy/undocumented .NET WebForms codebase before a rewrite or migration, wants a feature inventory of a solution, wants business rules extracted from stored procedures/triggers, or mentions preparing documentation as a reference for a future rewrite team. Also trigger for requests to run this process "AFK", unattended, or "until done."
metadata: 
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
license: Apache-2.0
user-invocable: true
---

# Legacy .NET Feature Mapper

Produces an as-is, citable map of what an old .NET WebForms + TSQL application
actually does today - not what it should do, not rewrite advice. The output is
a reference the rewrite team uses to make sure no feature, business rule, or
permission is silently lost.

**Everything is discovered via static analysis (reading source, never executing
it).** Never run, build, or compile the application. If something cannot be
determined by reading code/SQL, flag it - do not guess.

## The two rules that matter most

**1. A completion claim is worthless without an artifact.**
A feature is complete when a file exists on disk, passes the manifest, and its
citations point at real lines in real files - **not** when an agent says so.
The orchestrator verifies its own reading of the filesystem. A subagent's
report is a pointer, never evidence. Never report work as done without having
checked the file yourself.

**2. Deliverables live at the output root, nowhere else.**
Feature docs, shared-component docs, the index, the run log, and the questions
file are written **directly** to the output path. Temp and scratch directories
are prohibited for deliverables. Nothing that exists only in an agent's report
or a working file counts as delivered.

## The five phases

Read the relevant reference file before doing that phase's work. Do not rely on
this summary alone.

| Phase | Reference | Produces |
|---|---|---|
| **0 - Kickoff** | `references/kickoff-checklist.md` | Scope ledger, output path, batch plan, Session Contract |
| **1 - Discovery** | `references/discovery-phase.md` | `index.md` - every feature, entry points, size, scan ledger |
| **2 - Deep-dive** | `references/deep-dive-phase.md` | `features/<slug>.md` per feature, batched and parallel |
| **3 - Verification** | `references/verification-phase.md` | Pass/fail gate; nothing reaches `documented` without it |
| **4 - Rollup** | `references/status-reporting.md` | Status lines, batch rollup, `run-log.md`, `system-overview.md` |

Phase 3 fails -> back to Phase 2 (one retry). Phase 4 completes -> next batch.

## Required inputs

Resolve in this order (`references/session-state.md`): `.feature-mapper.json`
at the repo root -> skill memory -> ask the user. Never assume.

1. **Roots** - one or more paths. Everything at or below is in scope, and
   first-party project references are followed transitively. `.csproj` paths
   optional; folders are fine.
2. **DB object definitions folder** - the `.sql` schema/proc/trigger/view
   scripts.
3. **Output docs path** - where everything is written.

Ask with **multiple choice** wherever the options are enumerable, and glob the
workspace first so the choices are real paths. The user can always override.

Confirm the resolved transitive scope before scanning - that set defines the
size of the whole job, and the user gets to prune it.

## Cross-cutting rules

- **Static analysis only.** Read call sites, method bodies, `.aspx`/`.ascx`
  markup and code-behind, config, `.sql` definitions. No execution, no
  debugger. (This governs the *target app*; the skill's own validator script is
  fine to run.)
- **Templates are enforced, not suggested.** Copy the skeleton from
  `templates/`, then fill it. `references/doc-manifest.md` defines the contract;
  `scripts/Test-MapperOutput.ps1` checks it.
- **TSQL is traced deep.** Much of the business logic lives in the database.
  Follow procs into views, functions, triggers, and computed columns, and flag
  side effects. See `references/tsql-analysis.md`.
- **Every claim is cited** - file path + line (or SQL object + line) plus a
  confidence tag. See `references/citation-and-confidence.md`.
- **Permissions:** look for code-enforced role checks first, then DB/config
  driven rules. "No visible authorization check found" is a finding in its own
  right, and is `verified in code` once you have read the whole handler.
- **Shared code and DB objects** used by more than one feature get their own doc
  under `shared-components/`, linked from every feature - never re-explained
  inline.
- **Diagrams are earned.** Add a Mermaid diagram only where it beats prose - a
  branching workflow, a non-obvious call chain, an order-dependent interaction.
  Skip for simple CRUD, and say why.
- **Report progress on a cadence.** A timestamped status line per feature, a
  rollup per batch, a heartbeat inside anything long. Never go quiet for more
  than ~10 minutes. See `references/status-reporting.md`.
- **Defer, do not stall.** When only the user can settle something, file a
  question and keep working. See `references/questions-and-deferral.md`.
- **This is a map of the past, not advice for the future.** No modernization
  recommendations, no "you should refactor this", no opinions about the new
  framework.

## Batching and subagents

See `references/batching-and-agents.md`. Discovery sizes every feature `S`/`M`/`L`
in the index; batches are bounded by a **3-slot budget** (L=3, M=2, S=1) rather
than a feature count. Always propose suggested batches as multiple choice, and
always include "let me pick".

| Job | Agent | Why |
|---|---|---|
| Discovery sweep | `Explore` | read-only fan-out |
| Deep-dive writer | `general-purpose` | needs `Write` to author the doc |
| Verification spot-check | `Explore` | cannot edit what it grades |
| Index/log/questions updates | orchestrator | single writer, no clobbering |

## Output structure

```text
<output-docs-path>/
├── index.md                  # master status index - references/index-schema.md
├── system-overview.md        # app-wide map, updated as features land
├── run-log.md                # forensic per-feature record (attended AND AFK)
├── questions-for-user.md     # deferred blockers awaiting answers
├── features/
│   └── <feature-slug>.md
└── shared-components/
    └── <component-slug>.md

<repo-root>/.feature-mapper.json   # committed, repo-relative paths
```

## Updating an already-documented feature

If a doc exists and the user asks to improve/update it: **ask whether to fully
regenerate or incrementally update, every time** - never carry a prior answer
forward. Exception: in AFK mode, default to incremental and log the decision.

## AFK / unattended mode

Read `references/afk-mode.md`. Get the endpoint condition explicitly before
starting. Finish Discovery before deep-diving. File questions and continue
instead of stalling. Hard-stop only on: unwritable output path, all roots
missing, or three consecutive verification failures.

## Migrating v1 output

`references/migration-v1-to-v2.md` - migrate in place, preserve every id and
status, mark pre-v2 docs as unverified rather than back-dating a pass.

## Confidence tags

| Tag | Meaning |
|---|---|
| `verified in code` | Directly observed in source/SQL, unambiguous. Includes a confirmed *absence*. |
| `inferred from naming` | Reasonable inference from names/conventions, not confirmed by reading the logic |
| `unverified assumption` | Static analysis could not resolve it (dynamic SQL, reflection, runtime config) - must include a one-line reason |

## Statuses

```text
not started | in progress | documented | documented (open questions)
verification failed | candidate orphan/scheduled proc, unconfirmed
```

Full details, templates, and phase-by-phase instructions are in `references/`
and `templates/`. Read the relevant file before producing output.
