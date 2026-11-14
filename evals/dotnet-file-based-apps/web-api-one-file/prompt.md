---
tags: [dotnet-file-based-apps]
max_turns: 30
timeout_seconds: 900
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

Make me a one-file minimal API in todo.cs that I can start with dotnet: GET /todos returns a list of todo items (id, title, done) as JSON, and GET /todos/{id} returns one or 404. In-memory data is fine.
