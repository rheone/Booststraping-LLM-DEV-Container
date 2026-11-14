---
tags: [vsa]
max_turns: 60
timeout_seconds: 1500
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---
Move the order endpoints in this API (OrdersController -> OrderService -> generic Repository) to vertical slices. Mobile clients depend on this API, so nothing they can see may change: same routes, same JSON, same status codes, same error bodies. I'm offline today, so wherever you'd normally ask me, follow what the existing code does and list those decisions at the end. You may add a test project to prove the behavior did not change.

Input files: src/
