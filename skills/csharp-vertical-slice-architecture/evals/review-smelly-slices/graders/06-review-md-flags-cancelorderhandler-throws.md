---
type: llm
focus: trace
weight: 1
---

review.md flags that CancelOrderHandler throws NotFoundException for a missing order (exception for an expected outcome) and that nothing maps it, so it becomes a 500.
