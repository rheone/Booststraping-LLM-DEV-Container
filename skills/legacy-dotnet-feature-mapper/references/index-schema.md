# index.md and system-overview.md

## index.md

The master, resumable status tracker. One row per discovered feature. Format:

```markdown
---
last_updated: <date>
in_scope_csprojs:
  - <csproj path>
feature_counts:
  not_started: <count>
  in_progress: <count>
  documented: <count>
  candidate_orphan_unconfirmed: <count>
---
# Feature Index

Last updated: <date>
In-scope projects: <list of csproj paths currently covered by this index>

| Feature | Entry Point(s) | Status | Doc | Last Updated | Notes |
|---|---|---|---|---|---|
| Order Approval Workflow | `Billing/OrderApproval.aspx.cs` (btnApprove_Click, btnReject_Click) | documented | [features/order-approval-workflow.md](features/order-approval-workflow.md) | 2026-08-08 | |
| Customer Search | `Search/CustomerSearch.aspx.cs` | not started | - | - | Possibly related to Order Lookup - confirm during deep-dive |
| Order Lookup | `Search/OrderLookup.aspx.cs` | not started | - | - | |
```

**Status values**: `not started` | `in progress` | `documented` | `candidate orphan/scheduled proc, unconfirmed` (a stored procedure with no traceable in-code caller - needs user confirmation of its actual trigger, typically a SQL Agent job, before it can move to `in progress`)

Rules:

- This file's feat-XXXX IDs are the canonical source of stable feature IDs - assign the next unused number whenever Discovery finds a feature not already in the index, and never reuse or renumber an existing one, even if that feature is later merged/split/renamed
- Never remove a row or downgrade its status just because a later discovery pass re-scans the same code - only update entry points/notes, never regress status.
- Add new rows as new entry points are discovered (broader scope in a later session, or a feature found during a deep-dive that wasn't caught in Discovery).
- Keep the "In-scope projects" line current - if a session covers new csproj files, add them to this list.

## system-overview.md

App-wide narrative map, kept in sync as features are documented (not written once and forgotten). Structure:

```markdown
---
last_updated: <date>
in_scope_csprojs:
  - <csproj path>
feature_counts:
  not_started: <count>
  in_progress: <count>
  documented: <count>
  candidate_orphan_unconfirmed: <count>
---
# System Overview

Last updated: <date>

## What this application does

Short plain-language summary of the overall app's purpose, built up from documented features so far (this will be incomplete early on - say so).

## Feature map

A Mermaid diagram (typically a flowchart or graph) showing how documented features relate - which features feed into which, shared workflows, major modules/areas. Update incrementally; it's fine for this to only reflect features documented so far, with a note on what's not yet covered.

## Major shared components

Bullet list of the most-depended-on shared-components/ docs (e.g., referenced by 3+ features), since these represent the highest-blast-radius pieces of the old system.

## Coverage

X of Y known features documented. Link to index.md for full status.
```

Update `system-overview.md` at the end of any deep-dive session that documents at least one new feature - don't let it drift stale.
