---
type: llm
focus: trace
weight: 1
---

review.md flags that the GetOrderById endpoint queries the DbContext and changes order state (expiry) inside a GET endpoint, i.e. logic in the edge and a query with side effects.
