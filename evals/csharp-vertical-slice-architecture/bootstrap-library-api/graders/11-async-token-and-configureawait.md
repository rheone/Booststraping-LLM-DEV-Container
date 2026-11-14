---
type: llm
focus: trace
weight: 1
---

Every handler HandleAsync takes `CancellationToken ct = default` (or another name with `= default`) as its last parameter, every async call in src/ passes the token it received, every await in src/ uses `.ConfigureAwait(false)`, and .editorconfig enables CA1068, CA2016, and CA2007 for src/.
