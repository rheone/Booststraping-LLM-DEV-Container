---
type: llm
focus: trace
weight: 1
---

DeliverShipment.Result is an abstract record with a private constructor and nested sealed cases, and the endpoint maps the cases to 204, 404, and 409; its discard arm only throws.
