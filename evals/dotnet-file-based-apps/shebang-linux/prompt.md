---
tags: [dotnet-file-based-apps]
max_turns: 30
timeout_seconds: 900
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

Our Linux build agents should be able to run cleanup.cs directly as ./cleanup.cs 14, like a bash script. Make that work. The repo root is this folder (see .gitattributes).
Input files: .gitattributes, cleanup.cs
