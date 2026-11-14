---
type: llm
focus: trace
weight: 1
---

review.md flags that ReorderHandler (Features/Orders/Reorder) injects and calls CreateOrderHandler (handler calling handler / slice referencing slice).
