---
last_updated: 2026-08-09
discovery_complete: true
scope_ledger:
  - path: fixtures/src/Billing/Billing.csproj
    role: root
    files: 1
scan_ledger:
  - path: fixtures/src/Billing
    files_total: 1
    files_scanned: 1
    state: complete
feature_counts:
  not_started: 0
  in_progress: 0
  documented: 1
  documented_open_questions: 0
  verification_failed: 0
  candidate_orphan_unconfirmed: 0
---
# Feature Index

Last updated: 2026-08-09
Output root: `scripts/fixtures/docs-good`

| ID | Feature | Entry Point(s) | Size | Status | Doc | Last Updated | Verified | Notes |
|---|---|---|---|---|---|---|---|---|
| feat-0001 | Order Approval | `Billing/OrderApproval.aspx.cs` (btnApprove_Click) | M | documented | [features/order-approval.md](features/order-approval.md) | 2026-08-09 | pass 2026-08-09T14:00:00Z | |
