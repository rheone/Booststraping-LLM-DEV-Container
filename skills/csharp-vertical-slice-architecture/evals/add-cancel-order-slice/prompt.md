---
tags: [vsa]
max_turns: 60
timeout_seconds: 1500
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---
Add a cancel-order endpoint to the Shop API: POST /orders/{id}/cancel. If the order does not exist return 404, if it has already shipped return 409, otherwise mark it cancelled and return 204. I can't answer questions right now, so follow what the repo already does. Make sure the tests pass.

Input files: Shop.slnx, global.json, ARCHITECTURE.md, src/, tests/
