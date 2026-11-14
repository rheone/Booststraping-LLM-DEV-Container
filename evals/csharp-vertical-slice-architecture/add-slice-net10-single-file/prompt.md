---
tags: [vsa]
max_turns: 60
timeout_seconds: 1500
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

Add an endpoint to mark a dispatched shipment as delivered: POST /shipments/{id}/deliver. Return 404 if the shipment is unknown, 409 unless it is currently Dispatched, and 204 on success. Nobody is around to answer questions, so follow the existing code. Run the tests when you are done.

Input files: Depot.slnx, global.json, src/, tests/
