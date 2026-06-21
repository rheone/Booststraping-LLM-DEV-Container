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

Each skill is invoked by its slash command (shown in parentheses) and lives under `skills/{skill-name}/`. Skill files follow a consistent layout: `SKILL.md` (rules), `REFERENCE.md` (API tables), `EXAMPLES.md` (worked examples), `ANTI-PATTERNS.md` (pitfalls), and `QUALITY-CHECKLIST.md` (design judgment).

#### Orchestration

##### `csharp-test-sweep` (`/csharp-test-sweep`)

Orchestrates project-wide test suite improvement. Discovers test projects, detects the test framework (xUnit, NUnit, MSTest) and mocking library (Moq, NSubstitute, RhinoMocks, JustMock), then dispatches to the appropriate companion skills. Runs a 16-step discovery phase — baseline build, theory serializer audit, duplicate test ID detection, skipped-test audit, assertion anti-pattern audit, `async void` audit, project hygiene checks, and duplicate pattern detection for parameterized refactoring. Then iterates each test class with quality checklist application, sub-agent parallelization for large files, ambiguity annotation, and build+test verification per class. Supports scoped targeting: `/csharp-test-sweep` (full project), `/csharp-test-sweep ClassName` (single class), or `/csharp-test-sweep ClassName.Method_Test` (single method).

#### Testing — Frameworks

##### `xunit-csharp` (`/csharp-test-sweep` dispatches automatically; standalone: `/xunit-csharp`)

Provides xUnit v3 rules for writing and reviewing tests: `[Fact]` vs `[Theory]` decisions, strongly-typed `TheoryData<T>` with equivalence partitioning, `Assert.Equivalent` for structural comparison, `Assert.Multiple` for batched independent assertions, `ITestOutputHelper` guidance, exception message verification, fixture patterns (`IClassFixture<T>`, `IAsyncLifetime`, `ICollectionFixture<T>`), Object Mother pattern, coverage gap detection, and v1/v2→v3 migration guidance. Includes per-file reference tables, worked examples, framework-specific anti-patterns, and a quality checklist for design judgment.

##### `nunit-csharp` (`/csharp-test-sweep` dispatches automatically; standalone: `/nunit-csharp`)

Provides NUnit v5 rules with constraint-based assertion style (`Assert.That(actual, Is.EqualTo(expected))`), composable constraints (`&`, `|`, `Does`, `Has`, `Throws`), `Assert.Multiple` for batched failures, `[TestCase]`/`[TestCaseSource]`, fixture lifecycle (`[SetUp]`, `[OneTimeSetUp]`), parallelization (`[Parallelizable]`, `[NonParallelizable]`), `[Retry]` for flaky infrastructure, and `[TestFixture]` guidance for base class patterns and constructor injection.

##### `mstest-csharp` (`/csharp-test-sweep` dispatches automatically; standalone: `/mstest-csharp`)

Provides MSTest v4 rules with classic assertion style (`Assert.AreEqual(expected, actual)`), `[DataRow]`/`[DynamicData]` parameterized tests, `CollectionAssert` and `StringAssert` for specialized comparisons, lifecycle attributes (`[TestInitialize]` through `[AssemblyCleanup]`), execution control (`[Timeout]`, `[Retry]`, `[STATestMethod]`), `[DiscoverInternals]` for internal test discovery, metadata and work-item tracing (`[WorkItem]`, `[GitHubWorkItem]`), and `Assert.Multiple` for batched independent assertions.

#### Testing — Mocking Libraries

##### `moq-csharp` (`/csharp-test-sweep` dispatches automatically; standalone: `/moq-csharp`)

Provides Moq 4.x rules covering the full 80% practical API surface: mock creation with `MockBehavior` (Loose/Strict), `.Setup()`, `.Returns()`/`.ReturnsAsync()`, `.Throws()`, argument matchers (`It.Is<T>`, `It.IsAny<T>`, `It.IsInRange<T>`), verification with `Times`, `SetupSequence` for ordered returns, `Callback` for argument capture, `Capture.In` for collection capture, `SetupProperty` for auto-stubbed properties, `Mock.Of<T>()` for LINQ-to-mocks, and `.Invocations.Clear()` for call-history reset between tests.

##### `nsubstitute-csharp` (`/csharp-test-sweep` dispatches automatically; standalone: `/nsubstitute-csharp`)

Provides NSubstitute rules: `Substitute.For<T>()` for dependency substitution, `.Returns()`/`.ReturnsAsync()` for stubbing, `Arg.Any<T>()` and `Arg.Is<T>()` matchers, `Received()`/`DidNotReceive()` for verification, `Substitute.ForPartsOf<T>()` for partial substitutes with `.When().DoNotCallBase()`, callbacks with computed returns, and `.ClearReceivedCalls()` for call-history reset.

##### `justmock-csharp` (`/csharp-test-sweep` dispatches automatically; standalone: `/justmock-csharp`)

Provides Telerik JustMock rules with explicit free-vs-elevated mode split. Free mode (open-source, virtual-only interception, same constraints as Moq/NSubstitute) is the default for all examples. Elevated mode (commercial license + profiler, can intercept non-virtual/static/sealed members) is documented in a separate section. Covers `Mock.Create<T>()`, `Mock.Arrange()`, `Mock.Assert()` with `Occurs`, `Mock.CreateLike()` for auto-stubbed properties, `Behavior.Strict`/`Loose`, and `DoInstead()` callbacks. Elevated-only: static method mocking, sealed class interception, `DateTime.Now` interception.

##### `rhinomocks-csharp` (`/csharp-test-sweep` dispatches automatically; standalone: `/rhinomocks-csharp`)

Provides RhinoMocks rules for maintaining legacy test suites. AAA style (`MockRepository.GenerateMock<T>()`, `.Stub().Return()`, `.AssertWasCalled()`) is the recommended pattern. Record/replay model is documented but flagged as deprecated. Covers `StructureMap.AutoMocking`/`RhinoAutoMocker` for auto-mocking containers, `GenerateStub<T>()` for property behavior, `GeneratePartialMock<T>()` for partial overrides, and `Arg<T>.Is.Equal()`/`Arg<T>.Is.Anything` matchers. Includes a migration pathway table mapping RhinoMocks APIs to NSubstitute and Moq equivalents. **Not recommended for new projects** — NSubstitute or Moq should be used instead.

#### Documentation

##### `csharp-docs-and-comments` (`/csharp-docs-and-comments`)

Adds and improves XML documentation comments (`///`) and inline comments (`//`) in C# codebases. Targets senior-developer consumers: explains context, constraints, exceptions, and side effects rather than narrating signatures. Supports scoped invocation by type, member, or namespace. Includes reference tables for common XML doc tags and best practices for non-obvious documentation.

##### `reverse-engineered-docs` (`/reverse-engineered-docs`)

Reverse-engineers an existing project from source code and produces structured markdown documentation: project overview, inferred DDD domain boundaries, per-domain feature descriptions, glossary, open questions, and confidence annotations. Every claim is sourced to a file location; test files are used as behavioral evidence. Supports interactive, batch (unattended + sub-agents), and update (diff-and-patch) modes.

#### Refactoring

##### `csharp-split-type-to-partials` (`/csharp-split-type-to-partials`)

Refactors a C# type (class, record, struct) into partial files split by implemented interface and functional grouping (Factory, Operators). Mirrors the split in the corresponding xUnit test file and updates the `.csproj` with `DependentUpon` nesting for all partials. Only valid when the type directly implements or extends more than one type.

---


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
