# Status Reporting

Long runs are opaque by default. These reports are mandatory, not optional
courtesy - they are how the user knows work is happening, and how a
ghost-completion becomes visible while there is still time to react.

## Per-feature status line (to the conversation, as each feature completes)

One line, emitted the moment a feature reaches a terminal state. Never batched.

```
[2026-08-09T14:22:07-06:00] feat-0007 order-approval -> documented
    12 rules | 18 citations (12v/4i/2u) | 2 procs | 1 open question | verified: pass
```

Format:

```
[<ISO-8601 with offset>] <feat-id> <slug> -> <status>
    <N> rules | <N> citations (<v>v/<i>i/<u>u) | <N> DB objects | <N> open | verified: pass|fail|MANUAL
```

- The timestamp is real wall-clock time, taken when the line is emitted.
- `verified:` reflects the actual result from `verification-phase.md`. If the
  script did not run, say `MANUAL` - never `pass`.

## Batch rollup (to the conversation, at each batch boundary)

```
[2026-08-09T14:41:55-06:00] BATCH 3/7 complete - 18m22s
  documented: 2 (feat-0011, feat-0013)
  documented (open questions): 1 (feat-0012 - qst-0004 blocking)
  verification failed: 0
  batch sweep: pass (61 checks)
  questions filed: 1 blocking, 2 non-blocking -> questions-for-user.md
  next: batch 4/7 -> feat-0014, feat-0015, feat-0016   [ETA ~20m]
  output root: C:\src\acme\docs\legacy-map
```

Restating the output root here is deliberate - it is the cheapest possible
guard against the run drifting somewhere else.

## Heartbeat during a single long feature

If one feature (typically an `L`) is taking more than ~10 minutes, emit a
progress line so silence is never ambiguous:

```
[2026-08-09T14:31:02-06:00] feat-0012 order-fulfilment - still tracing
    done: entry point, 3 service classes | now: usp_FulfilOrder -> 2 triggers
```

## `run-log.md` - the forensic record

Skeleton: `templates/run-log.md`. **Append one entry per feature as it
completes**, never batched to the end of the run, so an interrupted run remains
inspectable and resumable.

Used in **attended and unattended runs alike** - there is no separate AFK log.
Two log formats for the same work is a bug.

The entry records start/end/duration, mode, agent type, output root, doc path
and size, citation counts by tag, shared components touched, the full
verification result **with per-item pass/fail**, the resulting status, and any
questions filed.

The verification block is the point of the whole file: when someone later asks
"did this actually get done?", the answer is a recorded list of checks that ran
and what they returned - not a recollection.

## What a status report must never do

- Report a feature as complete before the orchestrator has verified the file on
  disk itself.
- Report a count (rules, citations, DB objects) that was not actually counted
  from the written doc.
- Say `verified: pass` when the validator did not run.
- Go quiet for more than ~10 minutes during an active run.
