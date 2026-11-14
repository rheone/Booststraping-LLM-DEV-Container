---
type: llm
focus: trace
weight: 1
---

The state change goes through a new method on the Shipment entity (for example Shipment.Deliver), and the handler checks the state and returns an outcome before calling it, so no expected outcome throws.
