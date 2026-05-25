# Bootstrapping LLM Dev Container

A drop-in, reproducible development environment for C# / .NET projects with first-class support for AI-assisted workflows. The repository packages a "_ready-to-use_" `.devcontainer` configuration and a small collection of homegrown skills for use with AI coding agents such as [Claude Code](https://docs.anthropic.com/en/docs/claude-code/overview) or [Open Code](https://opencode.ai/).

This is a work in progress. Enjoy! Or don't I don't care -[Robert](https://rheone.com)

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

A fully configured development container built around modern .NET development. It provides a consistent, reproducible environment that works the same across Windows, macOS, and Linux. Hopefully fewwer "works on my machine" problems.

**Included tooling at a glance:**

| Category | Tools |
|---|---|
| Runtime | .NET 10 SDK, Python, PowerShell |
| AI | Claude Code, OpenCode |
| Formatters | CSharpier, Prettier |
| Terminal utilities | ripgrep, fd, fzf, zoxide, bat, jq, tree, vim |
| Git | Git LFS |

> For full architecture details, setup steps, and troubleshooting see **[DEV CONTAINER README.md](./DEV%20CONTAINER%20README.md)**.

### `skills/`

A set of homegrown skills for AI coding agents. Skills are reusable instruction sets that guide an AI agent's behaviour for specific tasks — think of them as lightweight, composable building blocks that live alongside your project. The skills in this repository are tailored to the C# / .NET development workflow that the dev container supports.

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows / macOS) or Docker Engine (Linux)
- [Visual Studio Code](https://code.visualstudio.com/) with the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
  - Or your favorite dev container host (though it was buildt with VS Code in mind, your millage may varry)

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
    ├── docker-compose.local.example.yml  ← This is just an example, you will need to create your own `docker-compose.local.ym`
    ├── post-create.sh
    ├── post-start.sh
    └── dotfiles/
```

### 2. Create your local override file

The container uses a gitignored local compose override for personal host paths. Copy the example template to get started:

```bash
cp .devcontainer/docker-compose.local.example.yml .devcontainer/docker-compose.local.yml
```

> **Do not commit this file.** It holds personal paths and is already listed in `.devcontainer/.gitignore`.

### 3. Set the required environment variables

The local override file references three host directories. Set them as persistent user-level variables so VS Code can pick them up.

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

Alternatively, create a `.devcontainer/.env` file (also gitignored) if you'd rather avoid a shell restart:

```
CLAUDE_DIR=C:\Users\{YourName}\.claude
GITCONFIG_PATH=C:\Users\YourName\.{YourName}
SSH_DIR=C:\Users\{YourName}\.ssh
```

### 4. Open in VS Code

Open your project in VS Code. When prompted, select **Reopen in Container** — or run it manually from the Command Palette (`Ctrl+Shift+P`):

```
Dev Containers: Rebuild and Reopen in Container
```

The container will build, run the post-create script, and land you in a fully configured environment.

---

## Design Philosophy

This setup is built around a few core ideas:

**Reproducibility.** All shared tooling is defined once in the container image and inherited by every developer on the project. There is nothing to install manually.

**Portability.** Personal configuration — AI agent state, SSH keys, git identity — is supplied at runtime through bind mounts and never baked into the image. The repository stays clean.

**Persistence.** The NuGet package cache survives container rebuilds via a named Docker volume. The Claude configuration directory is bind-mounted from the host so skills, memory, and agent state persist across rebuilds too.

---

## Further Reading

- [DEV CONTAINER README.md](./DEV%20CONTAINER%20README.md) — full architecture reference, per-platform setup instructions, persistent storage details, and a troubleshooting guide
- [Dev Containers specification](https://containers.dev/) — upstream documentation for the `.devcontainer` format
- [Claude Code documentation](https://docs.anthropic.com/en/docs/claude-code/overview) — documentation for the AI coding agent included in the container
