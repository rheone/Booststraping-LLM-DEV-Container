# Batching and Subagents

How a large job is cut into pieces, and who does each piece.

## Sizing (assigned during Discovery)

Every feature gets an `S`/`M`/`L` size written into the `Size` column of
`index.md` at discovery time, so batching is data-driven and survives a
restart. Score on four signals, then take the highest band any signal reaches:

| Signal | S | M | L |
|---|---|---|---|
| Entry points in the feature | 1 | 2-3 | 4+ |
| Lines of code-behind / controller code | < 200 | 200-800 | 800+ |
| Distinct DB objects referenced | 0-2 | 3-6 | 7+ |
| Cross-project call fan-out | same project | 2 projects | 3+ projects |

Anything with dynamic SQL, reflection-based dispatch, or a trigger cascade goes
up one band - those cost time in ways line counts do not show.

If Discovery could not size a feature confidently, mark it `M` and note why in
the Notes column. Do not leave the column blank; an unsized feature cannot be
batched.

## Slot budget

Concurrency is bounded by **work in flight**, not agent count. Total budget:
**3 slots**.

| Size | Slots |
|---|---|
| S | 1 |
| M | 2 |
| L | 3 |

So a batch runs three `S` features together, or one `M` plus one `S`, or a
single `L` on its own. This keeps a heavy feature from competing with two other
heavy features for attention and producing three shallow docs.

## Proposing a batch

Suggest batches from the index, then let the user accept or override. Ask with
multiple choice (see `kickoff-checklist.md`), always offering a hand-picked
option:

```
Suggested next batches (7 remaining, ~2h20m estimated):

A) Batch by domain - Billing        feat-0011 (M), feat-0013 (S)      [recommended]
B) Batch by size    - all smalls    feat-0013, feat-0016, feat-0018 (3xS)
C) Single feature   - feat-0012 (L) on its own
D) Let me pick      - name the features and I will build the batch
```

Prefer grouping features that share DB objects or shared components: tracing
`usp_ApproveOrder` once for three related features is materially cheaper than
three times.

Announce the batch, then run it without asking for per-feature confirmation.

## Agent assignment

| Job | Agent type | Why this one |
|---|---|---|
| Discovery sweep | `Explore` | Read-only fan-out across many files; returns locations, cannot accidentally write |
| Deep-dive writer | `general-purpose` | Needs `Write` to author the doc directly at the output root |
| Verification spot-check | `Explore` | **Cannot write** - a grader that cannot edit what it grades cannot paper over a failure |
| Batch orchestration, index/log/questions updates, validator runs | orchestrator (no subagent) | Single writer for shared files; avoids concurrent-write corruption |

### Rules for writer subagents

1. The prompt **begins with the Session Contract** (`session-state.md`) -
   absolute output root included. A subagent has none of the conversation's
   context.
2. It writes the final doc **directly** to `<output-root>/features/<slug>.md`,
   copied from `templates/feature-doc.md`. Temp and scratch directories are
   prohibited for deliverables.
3. It returns **only** a small structured result:
   `{ slug, path, headings_written, citation_count, db_objects, open_questions, shared_components_touched }`.
   Prose summaries of the analysis are wasted - the analysis belongs in the file.
4. It never touches `index.md`, `run-log.md`, or `questions-for-user.md`. Those
   are the orchestrator's, so that parallel writers cannot clobber each other.
5. Its report is a **pointer, not evidence**. The orchestrator verifies the file
   itself (`verification-phase.md`) before anything is called complete.

### Rules for the orchestrator

- Never report a feature complete on a subagent's claim.
- Update `index.md`, `run-log.md`, and `questions-for-user.md` immediately as
  each feature finishes - never batched to the end of a run.
- Run the validator per doc, and the batch sweep at every batch boundary.
- Emit the status line and batch rollup per `status-reporting.md`.

## Estimating

Rough planning figures for telling the user what they are in for - state them
as estimates, not promises: `S` ~3-6 min, `M` ~8-15 min, `L` ~20-40 min, plus
~1-2 min per feature for verification. A first batch calibrates the rest;
update the estimate in the batch rollup once real timings exist.
