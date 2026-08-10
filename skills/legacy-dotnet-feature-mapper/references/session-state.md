# Session State - never lose track of where output goes

A long run drifts. Context gets summarized, the output path scrolls out of
view, and a subagent that was never told where to write invents somewhere
plausible. Three redundant mechanisms prevent that, and they are cheap.

## 1. The Session Contract (load-bearing)

A fixed block the orchestrator **re-prints verbatim at the start of every
batch**, and **pastes into every subagent prompt**. Restating it on a cadence
is the fix; recording it once is not, because the failure is context decay.

```
=== SESSION CONTRACT ===
Output root:   C:\src\acme\docs\legacy-map        (absolute, always)
Index:         C:\src\acme\docs\legacy-map\index.md
Run log:       C:\src\acme\docs\legacy-map\run-log.md
Questions:     C:\src\acme\docs\legacy-map\questions-for-user.md
Roots:         C:\src\acme\src\Billing, C:\src\acme\src\Web
SQL defs:      C:\src\acme\db\definitions
Phase:         2 - Deep-dive
Batch:         3 of 7  -> feat-0011, feat-0012, feat-0013
Rules:         static analysis only; write final docs directly to the output
               root; nothing in temp/scratch counts as a deliverable
========================
```

Rules:

- **Absolute paths only.** A relative path means something different to a
  subagent with a different working directory.
- Re-print it at every batch start, after any interruption, and after any
  context summarization.
- Every writer and verifier subagent prompt begins with it. No exceptions - a
  subagent has none of the conversation's history.

## 2. `.feature-mapper.json` at the repo root

Skeleton: `templates/feature-mapper.json`. **Committed**, with **repo-relative
paths** so it is portable and reviewable, resolved to absolute at load. Being
committed lets a teammate - or a second concurrent run - pick up the same
configuration.

Written at the end of kickoff and updated at the end of every batch. On a new
session, look for it *first*: if it exists, the three required inputs are
already answered and the run is a continuation, not a fresh start.

If it is not in `.gitignore` and the user would rather it were, **ask** - offer
to add the line, do not edit `.gitignore` unprompted.

## 3. Skill memory

Write a memory noting the project's output root and roots for this repo, so a
future session can find prior work even if `.feature-mapper.json` was deleted
or the conversation starts somewhere unexpected. This is the last-resort
backstop, not the primary mechanism.

## Resolution order at the start of every session

1. Read `.feature-mapper.json` at the repo root, if present.
2. Otherwise check skill memory for a known output root for this repo.
3. Otherwise ask (see `kickoff-checklist.md`).
4. Whatever the source, **read `index.md` at that path before doing anything
   else** - it is the authority on what has already been done, and it carries
   the scope and scan ledgers.
5. Then run the reconciliation step in `questions-and-deferral.md`.

## Prohibited

- Writing a deliverable to a temp, scratch, or working directory. Feature docs,
  shared-component docs, the index, the run log, and the questions file are
  written **directly to the output root** and nowhere else.
- Guessing an output path because the contract was not to hand. If you cannot
  determine it, stop and ask - a doc written to the wrong place is worse than
  no doc, because nobody knows to look for it.
