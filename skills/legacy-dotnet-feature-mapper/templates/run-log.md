# Run Log

Forensic record of every feature processed, in both attended and unattended
runs. Append-only. This file exists so that "it said it was done" can always be
checked against what was actually produced and actually verified.

One entry per feature. Never batch these to the end of a run - write each entry
as the feature completes, so an interrupted run is still inspectable.

---

## <ISO-8601 start> -> <ISO-8601 end> | feat-XXXX | <feature-slug>

- **Duration:** <e.g. 4m12s>
- **Phase:** deep-dive
- **Mode:** new doc | incremental update | full regenerate
- **Agent:** general-purpose (writer) | Explore (verifier) | orchestrator
- **Output root:** `<absolute path>` <!-- restated every entry on purpose -->
- **Doc written:** `features/<slug>.md` (<N> lines)
- **Citations:** <N> total - verified <N> / inferred <N> / unverified <N>
- **Shared components touched:** comp-XXXX, comp-XXXX (or none)
- **Verification:**
  - script: `pass` | `fail` | `DID NOT RUN - fell back to manual checklist`
  - checks: <N> run, <N> failed
  - failed items: <item names, or none>
  - citation spot-check (Explore): <N>/<N> confirmed
- **Result:** documented | documented (open questions) | verification failed
- **Questions filed:** qst-XXXX (blocking), qst-XXXX (non-blocking) - or none
- **Notes:** <anything a human would want to know, or none>

---

## <ISO-8601> | BATCH ROLLUP | batch <N>

- **Batch contents:** feat-XXXX, feat-XXXX, feat-XXXX
- **Completed:** <N> documented, <N> with open questions, <N> verification failed
- **Batch sweep:** `pass` | `fail` (Test-MapperOutput.ps1 -Mode Batch)
- **Elapsed:** <duration>
- **Next batch:** feat-XXXX, feat-XXXX (or "run complete")
