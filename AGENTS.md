# AGENTS.md — llm-dev-container

This is a **meta-repository** — it packages a reusable `.devcontainer` and a collection of AI agent skills for C#/.NET development. It is NOT a .NET project itself (no `.sln`, no `.csproj`).

## Repository structure

```
├── devcontainer/.devcontainer/   # Drop-in dev container configuration for users' projects
├── skills/                       # AI agent skills (SKILL.md per directory)
├── commands/init                 # Scaffolds .NET projects inside the container
├── agents/                       # Reserved (empty)
├── plugins/                      # Reserved (empty)
```

## Key architectural facts

- **`devcontainer/.devcontainer/`** is the actual reusable devcontainer. Users copy this folder into their own projects. It has its own `AGENTS.md` (for when it lives in a user's project as `.devcontainer/`).
- **`devcontainer/AGENTS.md`** is stale for the root repo — it describes an empty workspace state that applies after a user copies `.devcontainer/` into their own project, NOT the root of this repo.
- **`commands/init`** is a bash script (`/init` inside the container) that scaffolds new .NET projects (webapi, blazor, classlib, xunit, mstest). It is the intended entrypoint when working inside the container with no project yet.

## Container setup for end users

- Requires Docker Desktop (Windows/macOS) or Docker Engine (Linux) with file sharing enabled for host drives.
- Three env vars must be set before opening in VS Code: `CLAUDE_DIR`, `GITCONFIG_PATH`, `SSH_DIR` — see `DEV CONTAINER README.md` for platform-specific commands.
- `docker-compose.local.yml` is gitignored — copy from `.example.yml` and fill in paths. Never commit it.
- Container user is `vscode` (non-root). You may need `sudo` for some operations on named volumes.

## Editorconfig conflict

There are **two `.editorconfig` files that disagree**:

| Setting | Root `.editorconfig` | `devcontainer/.devcontainer/dotfiles/.editorconfig` |
|---------|---------------------|-----------------------------------------------------|
| JSON/YAML indent | **3 spaces** | 2 spaces |
| PowerShell indent | 3 spaces | 4 spaces |

The **root `.editorconfig`** is authoritative for this repo. The dotfiles version is what gets deployed *inside* the container for the user's workspace.

## Skills

Skills live under `skills/` — each has a `SKILL.md`. Install via:

```bash
npx skills add rheone/Booststraping-LLM-DEV-Container
```

Companion skills (test frameworks + mocking libs) are nested under `skills/csharp-test-sweep/skills/`.

## Formatting

- **Root `.editorconfig`**: JSON/YAML 3-space indent, C# 4-space, LF endings, UTF-8, trailing newline.
- **`.gitattributes`**: LF for most files; CRLF for `.bat`, `.cmd`, `.cs`, `.ps1`.
- **Pre-commit hooks** deployed by `post-create.sh`: trailing-whitespace, end-of-file-fixer, check-yaml, check-added-large-files (512KB), codespell, markdownlint (disables MD013), csharpier.
  - Activate: `pre-commit install` (runs in `/workspace` inside container).
  - Run all: `pre-commit run --all-files`.

## AI agent tools inside container

| Agent | Config | LSP |
|-------|--------|-----|
| Claude Code | Named volumes (`claude-data`, `claude-runtime`) + plugins: `csharp-lsp`, `pyright-lsp` | csharp, pyright |
| OpenCode | `~/.config/opencode/opencode.jsonc` (deployed from dotfiles) | All built-in servers enabled (`"lsp": true`) |

## Container lifecycle

- **`post-create.sh`** runs once on first create: installs Zsh/Oh My Zsh, Oh My Posh, .NET global tools (9 tools), language servers, Claude Code plugins, pre-commit framework, restores NuGet for `.sln` files.
- **`post-start.sh`** runs every start: marks workspace as safe git directory, sets `core.autocrlf false`, suppresses detached HEAD warnings.
