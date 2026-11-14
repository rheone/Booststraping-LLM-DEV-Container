---
type: llm
focus: trace
weight: 1
---

Integration tests exist for each slice, including borrow returning 404 and 409, and they run against SQLite rather than the EF Core in-memory provider.
