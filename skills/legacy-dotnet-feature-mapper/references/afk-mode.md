# AFK / unattended mode

Runs Discovery and Deep-dive continuously without per-feature confirmation,
until an endpoint condition is met or a hard blocker forces a stop.

AFK does **not** relax any rule. Verification still gates every feature, the
templates are still enforced, and status reports are still emitted - if
anything they matter more here, because nobody is watching.

## Before starting

1. **Endpoint condition**, explicitly - exactly one of:
   - *Full completion*: every feature in (or discovered during) `index.md`
     reaches a terminal status.
   - *Target list*: a specific named/counted set ("these 12 entry points", "the
     first 30 features", "everything under `/Billing/`"). Confirm the exact set
     so the endpoint is unambiguous.
2. The three required inputs are resolved (`kickoff-checklist.md`). AFK does
   not relax this.
3. The scope ledger is confirmed. Pruning happens at kickoff, not mid-run.
4. Tell the user what will happen and roughly how long - then stop asking.

## The run loop

1. Reconcile answered questions (`questions-and-deferral.md`).
2. If `discovery_complete` is not `true`, **finish Discovery first**. Always.
   Deep-diving over a partial map is how a run produces confident nonsense.
3. Build the next batch from the index within the 3-slot budget.
4. Print the Session Contract.
5. Run the batch: writers in parallel, then verification per feature.
6. Update `index.md`, `run-log.md`, `questions-for-user.md` **immediately** as
   each feature completes - never batched to the end, so an interrupted run is
   still inspectable and resumable.
7. Emit the per-feature status line, then the batch rollup.
8. Check the endpoint condition. Repeat.

Existing doc during AFK: default to **incremental update** without asking, and
log the decision.

## Defer, do not stall

When the run hits something only the user can settle, **file a `qst-XXXX`, note
it in the affected doc, and move to the next feature** (see
`questions-and-deferral.md`). Document what can be documented; mark what
cannot. A blocked feature is not a blocked run.

## Hard stops (everything else defers)

Stop and report only when continuing correctly is impossible:

1. **The output path is missing or unwritable.** Nothing can be delivered.
2. **All roots are missing or unreadable.** Nothing can be analyzed.
3. **Three consecutive features fail verification.** That is a systemic
   problem - bad scope, an unreadable source root, a misunderstood template -
   and the next forty features would fail the same way. Stop, report the
   pattern, and show the failing check items.

Notably *not* hard stops any more:

- An ambiguous feature boundary -> question, continue
- A proc with no traceable caller -> `candidate orphan` row + question, continue
- Logic living in a project outside the roots -> the scope ledger already names
  it; document the boundary crossing, file a question, continue
- A claim that could not be verified -> `unverified assumption` tag, continue.
  This has never been a stopping condition and still is not.

## Reporting back

At the end of any AFK run:

```
[2026-08-09T18:03:44-06:00] AFK RUN COMPLETE - 3h41m

Features:  41 total | 34 documented | 4 documented (open questions)
                    | 2 verification failed | 1 candidate orphan
Batches:   9 of 9   | batch sweeps: 9 pass
Coverage:  discovery complete (all 6 projects scanned)
Output:    C:\src\acme\docs\legacy-map

Needs you:
  7 questions -> questions-for-user.md
    3 blocking (feat-0009, feat-0021, feat-0033 re-derive once answered)
    4 non-blocking (patched into existing docs on answer)
  2 verification failures -> feat-0017, feat-0028 (see run-log.md)

Answer the questions in the file, then re-run - reconciliation is automatic.
```

Never end a run by reporting only success counts. The questions and the
failures are the part the user has to act on.
