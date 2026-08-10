# Phase 2: Deep-dive

Fully document a batch of features to the standard in `doc-manifest.md`. This
is the rigorous pass - every business rule, permission check, failure state,
and DB interaction found and cited.

Nothing here is complete until Phase 3 says so. A written doc is a claim; a
verified doc is a result.

## Before starting a batch

1. Print the **Session Contract** (`session-state.md`). Absolute output root
   included, every batch, no exceptions.
2. Check `discovery_complete` in `index.md`. If `false`, warn and name the
   unscanned areas before proceeding.
3. Confirm each target feature exists in `index.md` (add a row if the user
   pointed at an entry point that was never indexed - assign the next unused
   `feat-XXXX`).
4. If a doc already exists: **ask whether to fully regenerate or incrementally
   update** - every time, never carrying forward a prior answer. In AFK mode,
   default to incremental and log the decision.
5. Set each target's index status to `in progress`.

## Dispatching writers

One `general-purpose` subagent per feature, within the 3-slot budget from
`batching-and-agents.md`. Each prompt must contain:

- The Session Contract verbatim
- The feature's id, name, entry points, and index row
- The absolute path it must write to: `<output-root>/features/<slug>.md`
- The instruction to copy `templates/feature-doc.md` and fill it
- The rule that **temp/scratch paths are prohibited for deliverables**
- The structured result it must return - and nothing more

The orchestrator, never a writer, updates `index.md`, `run-log.md`, and
`questions-for-user.md`.

## Tracing process (what each writer does)

1. **Start at the entry point(s)**. Read the actual code in full -
   `Page_Load`, event handlers, action methods.
2. **Follow every call path** through business/service classes to data access.
   Note every conditional branch that encodes a business rule - validation,
   eligibility, calculated values, workflow gating.
3. **Identify DB touchpoints**: stored proc calls, inline SQL (flag dynamic SQL
   explicitly - it limits static traceability), ORM queries, direct table
   access. Trace them per `tsql-analysis.md`.
4. **Identify permission/role checks**: `User.IsInRole`, authorization
   attributes, membership/role providers, DB-driven permission lookups. If no
   check guards an action that looks like it needs one, **say so explicitly** -
   that absence is a finding, and `verified in code` once you have read the
   whole handler.
5. **Identify predecessors/successors** from actual navigation code -
   `Response.Redirect`, post-back targets, status transitions - and cite them.
   Never from assumption.
6. **Identify failure states**: validation failures, `try`/`catch` behavior,
   what the user sees and what happens to the data.
7. **Note anything unresolvable statically** - dynamic SQL, reflection, runtime
   config, feature flags - with `unverified assumption` and a one-line reason.
8. **Cross a scope boundary?** If the trail leads into a project marked
   `boundary` or `excluded` in the scope ledger, document the crossing
   (what was called, with what, expected effect) and file a question. Do not
   silently expand scope, and do not silently stop.

## Writing the doc

Copy `templates/feature-doc.md` verbatim, then fill it. Do not compose freehand.

Hard requirements, all machine-checked in Phase 3:

- Every Business Rules bullet carries **both** a `file:line` citation and a
  confidence tag
- `confidence_summary` counts match the tags actually in the body
- `open_questions_count` matches the bullets actually in that section
- `has_diagram` matches reality; if there is no diagram, the section says
  `No diagram - <reason>`
- The `Authentication` section exists **iff** `trigger_type: webhook`
- No template placeholder text survives

## Shared code and DB objects

When you hit a class, method, proc, view, function, or trigger that is
generic/reusable or called from more than one place, do not inline it:

- Create or update `shared-components/<slug>.md` (see `shared-components.md`)
- Link it from the feature doc with a one-line reason for the dependency
- First time documented: assign the next unused `comp-XXXX` - same rules as
  `feat-XXXX`, assigned once, never reused or renumbered. Already documented:
  reuse its existing id.
- Add this feature's id to the component's `used_by` list

## Frontmatter is maintained on every write

Not a one-time setup step. On create, regenerate, **and** incremental update:

- `confidence_summary` recounted from the finished body
- `last_updated` and `source_snapshot` refreshed for this session
- `db_objects`, `predecessors`, `successors`, `related_shared_components` /
  `used_by` updated to reflect this session's findings - by id, not name
- `open_questions_count` matched to the actual section

`id` and the slug-derived filename are the only fields that must not change.

## Diagrams

Only where they add real clarity beyond prose:

- A multi-state workflow (state diagram) - e.g. an approval process
- A non-obvious multi-hop call chain across layers (flowchart)
- Time-ordered interaction across components/DB calls where order matters
  (sequence diagram)

Skip for single-path CRUD. "Load page -> save one table" as a diagram adds
nothing. Whichever way you go, `has_diagram` must agree with the body.

## Completing a feature

1. Writer returns its structured result.
2. Orchestrator **reads the file from disk itself** - the writer's report is a
   pointer, never evidence.
3. Run Phase 3 verification (`verification-phase.md`).
4. On pass: set status `documented` (or `documented (open questions)` if a
   blocking question is filed against it), fill the `Verified` column, append
   the `run-log.md` entry, emit the status line.
5. On fail: one retry with the failures fed back verbatim; then
   `verification failed` plus a question. Never loop.
6. Update `system-overview.md` at the end of any batch that documented at least
   one new feature.

## What blocks marking a feature documented

- No file on disk at the expected path
- Validation failures outstanding
- A required section missing or still holding template text
- A citation that does not resolve to a real file and line

An item that static analysis genuinely could not resolve does **not** block
completion - it belongs in Open Questions with an `unverified assumption` tag.
Unresolvable is fine and expected; unverified-but-claimed is not.
