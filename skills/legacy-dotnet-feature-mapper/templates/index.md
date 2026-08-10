---
last_updated: <date>
schema_version: 2
discovery_complete: false
output_root: "<absolute output docs path>"
roots:
  - <root path given by the user>
scope_ledger:
  - path: <project path>
    role: root            # root | transitive-dep | boundary | excluded
    reason: "<why, for boundary/excluded>"
    files: 0
scan_ledger:
  - path: <project or folder path>
    files_total: 0
    files_scanned: 0
    state: not-scanned    # complete | partial | not-scanned
sql_definitions_path: "<path to .sql definitions folder>"
feature_counts:
  not_started: 0
  in_progress: 0
  documented: 0
  documented_open_questions: 0
  verification_failed: 0
  candidate_orphan_unconfirmed: 0
---
# Feature Index

Last updated: <date>
Output root: `<absolute output docs path>`
Discovery complete: no

| ID | Feature | Entry Point(s) | Size | Status | Doc | Last Updated | Verified | Notes |
|---|---|---|---|---|---|---|---|---|
| feat-0001 | <Feature Name> | `<entry point>` | M | not started | - | - | - | |

<!--
Column contract (order matters - the validator reads by position):
  ID          feat-NNNN, assigned once, never reused or renumbered
  Size        S | M | L  (see references/batching-and-agents.md)
  Status      not started | in progress | documented |
              documented (open questions) | verification failed |
              candidate orphan/scheduled proc, unconfirmed
  Doc         markdown link to features/<slug>.md - REQUIRED for any status
              that asserts completion; the validator fails a row that claims
              completion with no file behind it
  Verified    pass|fail + ISO-8601 timestamp of the last verification run
-->
