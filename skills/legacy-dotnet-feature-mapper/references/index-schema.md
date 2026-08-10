# index.md and system-overview.md

## index.md

The master, resumable status tracker and the authority on scope, coverage, and
what has actually been produced. Skeleton: `templates/index.md`.

```markdown
---
last_updated: 2026-08-09
schema_version: 2
discovery_complete: true
output_root: "C:/src/acme/docs/legacy-map"
roots:
  - src/Web
  - src/Billing
scope_ledger:
  - path: src/Web
    role: root
    files: 412
  - path: src/Acme.Core
    role: transitive-dep
    files: 94
  - path: src/Legacy.Reporting
    role: excluded
    reason: "user excluded at kickoff 2026-08-09"
    files: 380
  - path: Newtonsoft.Json 9.0.1
    role: boundary
    reason: "third-party, no source available"
    files: 0
scan_ledger:
  - path: src/Web
    files_total: 412
    files_scanned: 412
    state: complete
sql_definitions_path: "db/definitions"
feature_counts:
  not_started: 5
  in_progress: 0
  documented: 34
  documented_open_questions: 4
  verification_failed: 2
  candidate_orphan_unconfirmed: 1
---
# Feature Index

Last updated: 2026-08-09
Output root: `C:/src/acme/docs/legacy-map`
Discovery complete: yes

| ID | Feature | Entry Point(s) | Size | Status | Doc | Last Updated | Verified | Notes |
|---|---|---|---|---|---|---|---|---|
| feat-0001 | Order Approval | `Billing/OrderApproval.aspx.cs` (btnApprove_Click) | M | documented | [features/order-approval.md](features/order-approval.md) | 2026-08-09 | pass 2026-08-09T14:22:07Z | |
| feat-0002 | Customer Search | `Search/CustomerSearch.aspx.cs` | S | not started | - | - | - | Possibly related to feat-0003 |
```

**Column order matters** - the validator reads by position:
`ID | Feature | Entry Point(s) | Size | Status | Doc | Last Updated | Verified | Notes`

**Status values:**

```
not started
in progress
documented
documented (open questions)      # verified, but a blocking question is open
verification failed              # written, failed the gate twice
candidate orphan/scheduled proc, unconfirmed
```

### Rules

- `feat-XXXX` IDs are canonical and stable: assigned once at Discovery, never
  reused or renumbered - even on rename, merge, or split.
- **A row may only claim `documented` / `documented (open questions)` /
  `verification failed` if the linked doc exists on disk.** The validator
  enforces this; it is the ghost-completion check.
- The `Verified` column records the last verification result:
  `pass <ISO-8601>`, `fail <ISO-8601>`, `manual <ISO-8601>` (script could not
  run), or `- (pre-v2, unverified)` for migrated v1 rows.
- Never remove a row or regress a status because a later Discovery re-scanned
  the same code. Update entry points, size, and notes only.
- `Size` is never blank. Unsure -> `M` with a note.
- `feature_counts` must match the rows. Recount whenever a status changes.
- Keep `scope_ledger` and `scan_ledger` current as scope widens or scanning
  progresses.
- `discovery_complete: true` only when every `scan_ledger` entry is `complete`.

## system-overview.md

App-wide narrative map, kept in sync as features are documented.

```markdown
---
last_updated: <date>
schema_version: 2
coverage:
  documented: 34
  total_known: 41
---
# System Overview

Last updated: <date>

## What this application does

Plain-language summary built up from documented features so far. Say plainly
when this is still incomplete.

## Feature map

A Mermaid diagram showing how documented features relate - what feeds into
what, shared workflows, major modules. Update incrementally; reflecting only
what is documented so far is fine, with a note on what is not yet covered.

## Major shared components

The most-depended-on `shared-components/` docs (referenced by 3+ features) -
the highest-blast-radius pieces of the old system.

## Coverage

34 of 41 known features documented. 4 have open questions, 2 failed
verification. See index.md for full status.
```

Update at the end of any batch that documented at least one new feature. Do not
let it drift stale.
