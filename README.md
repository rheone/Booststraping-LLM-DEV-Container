# Bootstrapping LLM Dev Container & Tooling

An opinionated, drop-in, reproducible development environment for C# / .NET projects with first-class support for AI-assisted workflows. This repository packages a ready-to-use `.devcontainer` configuration and Agentic AI skills for [Claude Code](https://docs.anthropic.com/en/docs/claude-code/overview), [OpenCode](https://opencode.ai/), etc.

This is a work in progress. Enjoy! Or don't — I don't care. —[Robert](https://rheone.com)

## What's inside?

| Component                                                     | Description                                                                    |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| [`devcontainer/.devcontainer/`](./devcontainer/.devcontainer) | Drop-in dev container — .NET 10 SDK, AI agents, formatters, terminal utilities |
| [`skills/`](./skills/)                                        | AI agent skills for test suites, refactoring, documentation, and automation    |

## Dev Container Quick Start

```bash
# 1. Copy the devcontainer into your own project
cp -r devcontainer/.devcontainer your-project/.devcontainer
```

```powershell
# 2. Set environment variables (Windows — persistent user scope)
[System.Environment]::SetEnvironmentVariable("CLAUDE_DIR",     "$HOME\.claude",    "User")
[System.Environment]::SetEnvironmentVariable("AGENTS_DIR",     "$HOME\.agents",    "User")
[System.Environment]::SetEnvironmentVariable("OPENCODE_DIR",   "$HOME\.opencode",    "User")
[System.Environment]::SetEnvironmentVariable("GITCONFIG_PATH", "$HOME\.gitconfig",  "User")
[System.Environment]::SetEnvironmentVariable("SSH_DIR",        "$HOME\.ssh",        "User")
```

```bash
# 2. Set environment variables (Linux / macOS)
export CLAUDE_DIR="$HOME/.claude"
export AGENTS_DIR="$HOME/.agents"
export OPENCODE_DIR="$HOME/.opencode"
export GITCONFIG_PATH="$HOME/.gitconfig"
export SSH_DIR="$HOME/.ssh"
```

**3.** Open your project in VS Code → **Reopen in Container**.

> Full platform-specific setup, Docker Desktop config, and troubleshooting → **[DEV CONTAINER README.md](./DEV%20CONTAINER%20README.md)**

## Skills

AI agent skills for C# / .NET development. Install all with a single command:

```bash
npx skills add rheone/Booststraping-LLM-DEV-Container
```

| Category                      | Skills                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Test Suite Sweep**          | [`csharp-test-sweep`](skills/csharp-test-sweep) orchestrates project-wide test improvement — auto-detects framework/mocking lib, sweeps each class. 7 companion skills: [`xunit-csharp`](skills/csharp-test-sweep/skills/xunit-csharp), [`nunit-csharp`](skills/csharp-test-sweep/skills/nunit-csharp), [`mstest-csharp`](skills/csharp-test-sweep/skills/mstest-csharp), [`moq-csharp`](skills/csharp-test-sweep/skills/moq-csharp), [`nsubstitute-csharp`](skills/csharp-test-sweep/skills/nsubstitute-csharp), [`justmock-csharp`](skills/csharp-test-sweep/skills/justmock-csharp), [`rhinomocks-csharp`](skills/csharp-test-sweep/skills/rhinomocks-csharp) |
| **Documentation**             | [`csharp-docs-and-comments`](skills/csharp-docs-and-comments) — XML doc / inline comments; [`reverse-engineered-docs`](skills/reverse-engineered-docs) — source code → structured markdown                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| **Diagrams**                  | [`mermaid-diagram-generator`](skills/mermaid-diagram-generator) — generates any Mermaid diagram type (flowchart, sequence, class, ER, Gantt, C4, and 25+ more) as `.mermaid`/`.mmd` files or markdown-embedded blocks                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| **Legacy Modernization**      | [`legacy-dotnet-feature-mapper`](skills/legacy-dotnet-feature-mapper) — reverse-engineers legacy .NET WebForms + TSQL apps into a citable, feature-by-feature map (business rules, permissions, DB behavior) via static analysis only, ahead of a rewrite                                                                                                                                                                                                                                                                                                                                                                                                        |
| **Refactoring**               | [`csharp-split-type-to-partials`](skills/csharp-split-type-to-partials) — split types by interface/grouping; [`csharp-library-repo-structure`](skills/csharp-library-repo-structure) — audit .NET library layout for NuGet publishing                                                                                                                                                                                                                                                                                                                                                                                                                            |
| **Code Review & Remediation** | [`audit-remediation-pipeline`](skills/audit-remediation-pipeline) — multi-agent pipeline for audit findings                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |

> Full catalog with per-skill install commands and usage guidance → **[SKILLS.md](./SKILLS.md)**

## Design Philosophy

**Reproducibility.** All shared tooling defined once in the container image, inherited by every developer. Nothing to install manually.

**Portability.** Personal configuration — AI agent state, SSH keys, git identity — supplied at runtime through bind mounts, never baked into the image.

**Persistence.** NuGet packages and Claude Code state survive rebuilds via named Docker volumes and host bind mounts.

**AI-first.** Two agents (Claude Code and OpenCode) pre-configured with LSP diagnostics, plugins, and C# development skills ready on first launch.

## Further Reading

- [DEV CONTAINER README.md](./DEV%20CONTAINER%20README.md) — full architecture, per-platform setup, persistent storage, troubleshooting
- [Dev Containers specification](https://containers.dev/)
- [Claude Code documentation](https://docs.anthropic.com/en/docs/claude-code/overview)
- [OpenCode documentation](https://opencode.ai/docs/)
