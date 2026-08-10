# Phase 0: Kickoff

Establish scope, output location, and the plan - then get out of the way. Ask
with **multiple choice wherever the options are enumerable**, and always leave
an override open (the "Other" escape in `AskUserQuestion` is that override).

Finding facts is this skill's job, never the user's. Glob the workspace *first*
so the choices offered are real paths, not a blank prompt.

## Step 1 - Resume or fresh start?

Before asking anything, follow the resolution order in `session-state.md`:
`.feature-mapper.json` -> skill memory -> ask. If prior work exists, read
`index.md`, run the reconciliation step in `questions-and-deferral.md`, and
open with a summary of where things stand rather than a blank interview.

## Step 2 - The three required inputs

Never assumed, never silently defaulted.

### 1. Roots (what to analyze)

One or more paths. **Everything at or below a root is in scope**, and
first-party project references are followed transitively.

Glob for `.csproj`/`.sln` files first, then offer what you found:

> **Which part of the codebase should I map?**
> - A) The whole solution - `Acme.sln` (14 projects)
> - B) `src/Web` and `src/Billing` (the WebForms front end)
> - C) `src/Web` only
> - D) Other - give me paths

Roots may be folders; `.csproj` paths are no longer required.

### 2. SQL definitions folder

Glob for folders containing `.sql` files and offer them. Ask whether the folder
is a current-state dump or incremental change scripts - it changes how objects
are resolved (see `tsql-analysis.md`). If it contains objects outside this
app's schema, ask whether to filter by prefix.

### 3. Output docs path

Offer any existing `docs/` folders plus a sensible new one. Confirm whether an
`index.md` already exists there - if so, this is a continuation.

## Step 3 - Resolve and confirm the scope ledger

**Before scanning anything**, resolve the transitive closure of project
references from the roots and show it back. This set defines the size of the
entire job, so the user gets to prune it:

> **I resolved 9 projects from your roots. Uncheck anything I should skip:**
> ```
> [x] src/Web                  root            412 files
> [x] src/Billing              root            168 files
> [x] src/Acme.Core            transitive-dep   94 files
> [x] src/Acme.Data            transitive-dep  131 files
> [ ] src/Legacy.Reporting     transitive-dep  380 files
>     Newtonsoft.Json 9.0.1    boundary        third-party, no source
> ```

Record every project in the `scope_ledger` with its role - `root`,
`transitive-dep`, `boundary` (third-party, no source to read), or `excluded`
(with the reason). Excluded is *recorded, not forgotten*: calls into an
excluded project are documented as boundary crossings, never silently
untraced.

## Step 4 - Phase, depth, and run mode

Multiple choice again:

- **Phase** - Discovery (inventory) or Deep-dive (full docs)? If no `index.md`
  exists at the output path, Discovery must run first regardless; explain that
  rather than skipping into a deep-dive with nothing to dive into.
- **Run mode** - attended (report and pause at batch boundaries) or unattended
  (AFK, run to an endpoint condition, defer questions)?
- **Batching** - offer the suggested batches from `batching-and-agents.md`,
  plus "let me pick".
- **AFK endpoint condition**, if unattended - full completion, or a named/counted
  target set. Required, not optional.

## Step 5 - Write the contract and start

1. Write `.feature-mapper.json` at the repo root (`templates/feature-mapper.json`).
   If `.gitignore` should mention it, **ask** - do not edit `.gitignore`
   unprompted.
2. Create the output tree and seed `index.md`, `run-log.md`, and
   `questions-for-user.md` from `templates/`.
3. Print the Session Contract (`session-state.md`).
4. Tell the user the plan and the estimate, then begin.

## On existing feature docs

If a deep-dive targets a feature that already has a doc, **ask every time**
whether to fully regenerate or incrementally update - do not carry a prior
answer forward. Exception: in AFK mode, default to incremental update
automatically and log the decision.

## Hard rule

Do not begin analysis while roots, SQL definitions path, or output path are
unresolved. Everything else can be defaulted and corrected later; these three
cannot.
