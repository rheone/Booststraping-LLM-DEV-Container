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

A set of homegrown skills for AI coding agents. Skills are reusable instruction sets that guide an AI agent's behavior for specific tasks — composable building blocks that live alongside your project. The skills in this repository are tailored to the C# / .NET development workflow the dev container supports.

Each skill lives under `skills/{skill-name}/`. All skills have `SKILL.md` (rules). Optional companion files include `REFERENCE.md` (API tables), `EXAMPLES.md` (worked examples), `ANTI-PATTERNS.md` (pitfalls), and `QUALITY-CHECKLIST.md` (design judgment) — usage varies by skill.

#### Installing

Install all skills in this repository with a single command:

```bash
npx skills add rheone/Booststraping-LLM-DEV-Container
```

The CLI auto-discovers every `SKILL.md` under the `skills/` directory — no configuration required.

Or install from a local path:

```bash
npx skills add /path/to/llm-dev-container
```

**Install specific skills** using the `--skill` flag:

```bash
# Install just the test sweep and xUnit companion
npx skills add rheone/Booststraping-LLM-DEV-Container --skill csharp-test-sweep --skill xunit-csharp

# Install documentation and refactoring skills
npx skills add rheone/Booststraping-LLM-DEV-Container --skill reverse-engineered-docs --skill csharp-split-type-to-partials
```

After installing all skills, selectively enable/disable what you need:

```bash
npx skills enable csharp-test-sweep reverse-engineered-docs
npx skills disable homeassistant-awtrix homeassistant-pixoo64
```

To see what's installed and which are active:

```bash
npx skills list
```

#### Skills

##### Application Lifecycle & Automation

| Skill | Description |
|-------|-------------|
| [`homeassistant-awtrix`](/homeassistant-awtrix) | Control AWTRIX 3 on Ulanzi TC001 pixel clocks — notifications, custom apps, RTTTL sounds, display settings |
| [`homeassistant-pixoo64`](/homeassistant-pixoo64) | Control Divoom Pixoo 64 displays — 8 page types, wake-notify-sleep, push notifications |
| [`obsidian-ics-sync`](/obsidian-ics-sync) | Sync ICS calendar events into Obsidian daily notes with wikilink entity matching |

##### Code Review & Remediation

| Skill | Description |
|-------|-------------|
| [`audit-remediation-pipeline`](/audit-remediation-pipeline) | Systematic multi-agent pipeline for audit findings — research → pedantic review → tech writer → auditor → implement → verify |

##### Documentation

| Skill | Description |
|-------|-------------|
| [`csharp-docs-and-comments`](/csharp-docs-and-comments) | Add/improve XML doc comments and inline comments in C# codebases |
| [`reverse-engineered-docs`](/reverse-engineered-docs) | Reverse-engineer source code into structured markdown docs with confidence annotations |

##### Refactoring

| Skill | Description |
|-------|-------------|
| [`csharp-split-type-to-partials`](/csharp-split-type-to-partials) | Split C# types into partial files by interface/functional grouping |
| [`csharp-library-repo-structure`](/csharp-library-repo-structure) | Bootstrap, audit, and refactor .NET library repo layout for NuGet distribution |

##### Test Suite Sweep (Orchestrator)

| Skill | Description |
|-------|-------------|
| [`csharp-test-sweep`](/csharp-test-sweep) | Orchestrates project-wide test suite improvement — detects framework/mocking library, runs 16-step discovery, dispatches to companion skills, iterates each class with verification |

##### Test Frameworks (Companion)

| Skill | Description |
|-------|-------------|
| [`xunit-csharp`](/xunit-csharp) | xUnit v3 rules — `[Fact]`/`[Theory]`, `TheoryData<T>`, `Assert.Equivalent`, fixtures |
| [`nunit-csharp`](/nunit-csharp) | NUnit v5 rules — constraint-based assertions, `[TestCase]`, `[Retry]`, parallelization |
| [`mstest-csharp`](/mstest-csharp) | MSTest v4 rules — `[DataRow]`/`[DynamicData]`, `CollectionAssert`, lifecycle attributes |

##### Mocking Libraries (Companion)

| Skill | Description |
|-------|-------------|
| [`moq-csharp`](/moq-csharp) | Moq 4.x — `MockBehavior`, `.Setup().Returns()`, argument matchers, verification |
| [`nsubstitute-csharp`](/nsubstitute-csharp) | NSubstitute — `Substitute.For<T>()`, `Arg.Is<T>()`, `Received()` verification |
| [`justmock-csharp`](/justmock-csharp) | Telerik JustMock — free/elevated mode, non-virtual/static interception in elevated |
| [`rhinomocks-csharp`](/rhinomocks-csharp) | RhinoMocks — legacy suite maintenance, AAA style, migration pathway to NSubstitute/Moq |

The 7 **companion** skills (test frameworks + mocking libraries) live under `skills/csharp-test-sweep/skills/`. They are invoked automatically by `csharp-test-sweep` and can also be used standalone via their skill name.


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
