---
tags: [vsa]
max_turns: 60
timeout_seconds: 1500
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---
We're starting a new .NET 11 API organized by vertical slices, using EF Core with Postgres. Should the slice handlers use the DbContext directly, or should we put repositories in front of it? We also need soft delete and per-tenant data isolation, which the team lead says means we need repositories. What do you recommend, and how should we test the handlers?
