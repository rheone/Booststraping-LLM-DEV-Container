# Bootstrapping LLM Dev Container

An opinionated, drop-in, reproducible development environment for C# / .NET projects with first-class support for AI-assisted workflows. The repository packages a ready-to-use `.devcontainer` configuration and a collection of homegrown skills for use with AI coding agents such as [Claude Code](https://docs.anthropic.com/en/docs/claude-code/overview) and [OpenCode](https://opencode.ai/).

This is a work in progress. Enjoy! Or don't — I don't care. —[Robert](https://rheone.com)

---

## What's in this repository?

```
.
├── devcontainer/
│   └── .devcontainer/          # Drop-in dev container configuration
├── skills/                     # Custom AI agent skills
├── DEV CONTAINER README.md     # Detailed dev container documentation
└── README.md                   # This file
```

### `.devcontainer`

A fully configured development container built around modern .NET development. It provides a consistent, reproducible environment that works the same across Windows, macOS, and Linux — fewer "works on my machine" problems.

**Included tooling at a glance:**

| Category | Tools |
|---|---|
| Runtime | .NET 10 SDK, Python, Node.js, PowerShell |
| AI Agents | Claude Code, OpenCode |
| AI Agent Plugins | csharp-lsp, pyright-lsp |
| Formatters | CSharpier, Prettier |
| Shell | Zsh, Oh My Zsh, Oh My Posh |
| .NET CLI Tools | dotnet-ef, dotnet-script, dotnet-trace, dotnet-counters, dotnet-dump, dotnet-outdated |
| Terminal utilities | ripgrep, fd, fzf, zoxide, bat, lazygit, delta, jq, tree, vim |
| Git | Git LFS, GitHub CLI |

> For full architecture details, setup steps, and troubleshooting, see **[DEV CONTAINER README.md](./DEV%20CONTAINER%20README.md)**.

### `skills/`

A set of homegrown skills for AI coding agents. Skills are reusable instruction sets that guide an AI agent's behaviour for specific tasks — composable building blocks that live alongside your project. The skills in this repository are tailored to the C# / .NET development workflow the dev container supports.

#### Orchestration

| Skill | Description |
|---|---|
| `csharp-test-sweep` | Systematically sweeps C# test projects one class at a time, reviewing and improving tests against quality standards. Auto-detects the test framework and mocking library from the project file and delegates to the appropriate companion skill. Supports scoped invocation by method, region, class, namespace, or full project. |

#### Testing — Frameworks

| Skill | Description |
|---|---|
| `xunit-csharp` | Write, update, and improve xUnit v3 unit and integration tests. Covers naming conventions, AAA structure, `[Theory]`/`[Fact]` patterns, `TheoryData<T>`, `IXunitSerializer`, Object Mother, and fixture patterns. |
| `nunit-csharp` | Write, update, and improve NUnit tests. Stub that expands with NUnit-specific patterns (`[TestCase]`, `[TestCaseSource]`, constraint model) on first use against an NUnit project. |
| `mstest-csharp` | Write, update, and improve MSTest tests. Stub that expands with MSTest-specific patterns (`[DataTestMethod]`, `[DataRow]`, `[DynamicData]`) on first use. |

#### Testing — Mocking Libraries

| Skill | Description |
|---|---|
| `nsubstitute-csharp` | Write, update, and improve NSubstitute mock setups. Covers the abstract-class interception trap, partial substitutes, argument matchers, and call verification. |
| `moq-csharp` | Write, update, and improve Moq mock setups. Stub that expands with Moq-specific patterns (`MockBehavior`, `SetupProperty`, `Capture`, `InSequence`) on first use. |
| `justmock-csharp` | Write, update, and improve Telerik JustMock mock setups. Covers free vs elevated/profiler mode, `Mock.Arrange`, `Mock.Assert`, and `Behavior` modes. |
| `rhinomocks-csharp` | Write, update, and improve RhinoMocks mock setups. Stub that expands with RhinoMocks AAA patterns on first use. Flags migration opportunities to NSubstitute or Moq — RhinoMocks is largely unmaintained. |

#### Documentation

| Skill | Description |
|---|---|
| `gen-dotnet-docs-comments` | Add and improve XML documentation comments (`///`) and inline comments (`//`) in C# codebases. Scoped by type, member, or namespace. Writes for senior developer consumers — context, constraints, and domain concepts, not narrative repetition of signatures. |
| `reverse-engineered-docs` | Reverse-engineers an existing project from source code and produces structured markdown documentation: project overview, DDD domain breakdown, and per-feature docs. Supports interactive and batch modes. |

#### Refactoring

| Skill | Description |
|---|---|
| `split-type-to-partials` | Refactors a C# type into partial files split by implemented interface and functional grouping (Factory, Operators). Mirrors the split in the corresponding test file and updates the `.csproj` with `DependentUpon` nesting. |

> **Note on stubs:** Several mocking and framework skills (`moq-csharp`, `mstest-csharp`, `nunit-csharp`, `rhinomocks-csharp`) ship as stubs. They contain universal rules pre-applied to the library's API but expand automatically with library-specific patterns the first time `csharp-test-sweep` runs against a project using that library.

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows / macOS) or Docker Engine (Linux)
- [Visual Studio Code](https://code.visualstudio.com/) with the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
  - Other dev container hosts may work, but this was built with VS Code in mind — your mileage may vary

---

## Quick Start

### 1. Copy the dev container into your project

Copy the `devcontainer/.devcontainer` folder into the root of your project:

```
your-project/
└── .devcontainer/      ← drop this folder here
    ├── devcontainer.json
    ├── Dockerfile
    ├── docker-compose.yml
    ├── docker-compose.local.example.yml
    ├── post-create.sh
    ├── post-start.sh
    └── dotfiles/
```

### 2. Create your local override file

The container uses a gitignored local compose override for personal host paths. Copy the example template:

```bash
cp .devcontainer/docker-compose.local.example.yml .devcontainer/docker-compose.local.yml
```

> **Do not commit this file.** It holds personal paths and is already listed in `.devcontainer/.gitignore`.

### 3. Set the required environment variables

The local override references three host directories. Set them as persistent user-level variables so VS Code picks them up.

**Windows (PowerShell):**

```powershell
[System.Environment]::SetEnvironmentVariable("CLAUDE_DIR",     "$HOME\.claude",    "User")
[System.Environment]::SetEnvironmentVariable("GITCONFIG_PATH", "$HOME\.gitconfig",  "User")
[System.Environment]::SetEnvironmentVariable("SSH_DIR",        "$HOME\.ssh",        "User")
```

**Linux / macOS** — add to `~/.bashrc` or `~/.zshrc`:

```bash
export CLAUDE_DIR="$HOME/.claude"
export GITCONFIG_PATH="$HOME/.gitconfig"
export SSH_DIR="$HOME/.ssh"
```

Alternatively, create a `.devcontainer/.env` file (also gitignored) to avoid a shell restart:

```env
CLAUDE_DIR=C:\Users\YourName\.claude
GITCONFIG_PATH=C:\Users\YourName\.gitconfig
SSH_DIR=C:\Users\YourName\.ssh
```

### 4. Open in VS Code

Open your project in VS Code. When prompted, select **Reopen in Container** — or run from the Command Palette (`Ctrl+Shift+P`):

```
Dev Containers: Rebuild and Reopen in Container
```

The container will build, run the post-create script, and land you in a fully configured environment.

---

## Design Philosophy

**Reproducibility.** All shared tooling is defined once in the container image and inherited by every developer on the project. Nothing to install manually.

**Portability.** Personal configuration — AI agent state, SSH keys, git identity — is supplied at runtime through bind mounts and never baked into the image. The repository stays clean.

**Persistence.** NuGet packages and Claude Code configuration survive container rebuilds via named Docker volumes and host bind mounts. Skills, memory, and agent state persist across rebuilds.

**AI-first.** The environment is designed for AI-assisted workflows. Two agents (Claude Code and OpenCode) are pre-configured with LSP diagnostics, marketplace plugins, and a library of C# skills ready to use on first launch.

---

## Further Reading

- [DEV CONTAINER README.md](./DEV%20CONTAINER%20README.md) — full architecture reference, per-platform setup, persistent storage, and troubleshooting
- [Dev Containers specification](https://containers.dev/) — upstream documentation for the `.devcontainer` format
- [Claude Code documentation](https://docs.anthropic.com/en/docs/claude-code/overview) — documentation for Claude Code
- [OpenCode documentation](https://opencode.ai/docs/) — documentation for OpenCode
