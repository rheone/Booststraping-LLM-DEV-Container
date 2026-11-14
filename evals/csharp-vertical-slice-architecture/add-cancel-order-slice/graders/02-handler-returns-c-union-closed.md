---
type: llm
focus: trace
weight: 1
---

The handler returns a C# union (or closed hierarchy) whose cases are named after the domain (for example OrderCancelled, OrderMissing/OrderNotFound, OrderAlreadyShipped), and the endpoint maps it to 204, 404, and 409 with a switch that has no `_` discard arm.
