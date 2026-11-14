---
type: llm
focus: trace
weight: 1
---

The new DeliverShipment handler takes `CancellationToken ct = default` as its last parameter, passes the token to every async call, and uses `.ConfigureAwait(false)` on every await.
