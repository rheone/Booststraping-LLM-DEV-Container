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

Every C#/.NET skill carries a prefix that identifies what it's about: `csharp-<concept>` for a
language feature, design pattern, architectural style, or convention; `dotnet-<technology>` for a
specific .NET/BCL API, third-party library, dev tool, or testing framework.
[`csharp-skill-authoring`](skills/csharp-skill-authoring) is the meta-skill that builds every other
skill in the catalog to this same standard.

| Category | Skill count | A few examples |
| --- | --- | --- |
| C# Language Features | 13 | [`csharp-generics`](skills/csharp-generics), [`csharp-pattern-matching`](skills/csharp-pattern-matching), [`csharp-async`](skills/csharp-async) |
| Design Patterns | 14 | [`csharp-strategy-pattern`](skills/csharp-strategy-pattern), [`csharp-repository-pattern`](skills/csharp-repository-pattern), [`csharp-decorator-pattern`](skills/csharp-decorator-pattern) |
| ASP.NET Core & Web | 8 | [`dotnet-minimal-apis`](skills/dotnet-minimal-apis), [`dotnet-aspnetcore-authentication`](skills/dotnet-aspnetcore-authentication), [`dotnet-openiddict`](skills/dotnet-openiddict) |
| Data Access | 4 | [`dotnet-ef-core`](skills/dotnet-ef-core), [`dotnet-dapper`](skills/dotnet-dapper), [`dotnet-automapper`](skills/dotnet-automapper) |
| Testing Tools | 7 | [`dotnet-xunit`](skills/dotnet-xunit), [`dotnet-nsubstitute`](skills/dotnet-nsubstitute), [`dotnet-testcontainers`](skills/dotnet-testcontainers) |
| Messaging, Background Work & Resilience | 8 | [`dotnet-mediatr`](skills/dotnet-mediatr), [`dotnet-masstransit`](skills/dotnet-masstransit), [`dotnet-polly`](skills/dotnet-polly) |
| Observability & Logging | 3 | [`dotnet-serilog`](skills/dotnet-serilog), [`dotnet-opentelemetry`](skills/dotnet-opentelemetry) |
| .NET/BCL Platform | 6 | [`dotnet-linq`](skills/dotnet-linq), [`dotnet-system-text-json`](skills/dotnet-system-text-json), [`dotnet-channels`](skills/dotnet-channels) |
| Other Libraries & Tooling | 10 | [`dotnet-autofac`](skills/dotnet-autofac), [`dotnet-refit`](skills/dotnet-refit), [`dotnet-roslyn-analyzers`](skills/dotnet-roslyn-analyzers) |
| Documentation, Diagrams & Refactoring | 6 | [`reverse-engineered-docs`](skills/reverse-engineered-docs), [`mermaid-diagram-generator`](skills/mermaid-diagram-generator), [`csharp-code-organization`](skills/csharp-code-organization) |
| Test Suite Sweep (Orchestrator + 7 companions) | 8 | [`csharp-test-sweep`](skills/csharp-test-sweep) auto-detects the project's test framework and mocking library, then sweeps every class |

> Full catalog, every skill's summary, and a link to each skill's own README → **[SKILLS.md](./SKILLS.md)**

## Design Philosophy

All shared tooling is defined once in the container image and inherited by every developer, so there's nothing to install manually. Personal configuration, things like AI agent state, SSH keys, and git identity, comes in at runtime through bind mounts instead of being baked into the image. NuGet packages and Claude Code state survive rebuilds through named Docker volumes and host bind mounts. Claude Code and OpenCode both come pre-configured with LSP diagnostics, plugins, and C# development skills ready on first launch.

## Further Reading

- [DEV CONTAINER README.md](./DEV%20CONTAINER%20README.md) — full architecture, per-platform setup, persistent storage, troubleshooting
- [Dev Containers specification](https://containers.dev/)
- [Claude Code documentation](https://docs.anthropic.com/en/docs/claude-code/overview)
- [OpenCode documentation](https://opencode.ai/docs/)
