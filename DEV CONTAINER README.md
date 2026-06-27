# Development Container

Reusable cross-platform development container for modern .NET development and AI-assisted workflows.

Goals:

- Consistent developer onboarding
- Reproducible tooling across machines
- Persistent AI agent configuration
- Fast rebuilds via layer and package caching
- Clean separation of shared vs personal configuration

---

## Included Tooling

| Category | Tools |
|---|---|
| Runtime | .NET 10 SDK, Python, Node.js, PowerShell |
| AI Agents | Claude Code, OpenCode |
| AI Agent Plugins | csharp-lsp, pyright-lsp (Claude Code); built-in csharp + pyright (OpenCode) |
| Formatters | CSharpier, Prettier |
| Shell | Zsh, Oh My Zsh, Oh My Posh |
| .NET CLI Tools | dotnet-ef, dotnet-script, dotnet-trace, dotnet-counters, dotnet-dump, dotnet-outdated |
| Terminal utilities | ripgrep, fd, fzf, zoxide, bat, lazygit, delta, jq, tree, vim |
| Git | Git LFS, GitHub CLI, GitHub Copilot CLI |

---

## Architecture

| File | Responsibility |
|---|---|
| `devcontainer.json` | VS Code and Dev Container configuration |
| `docker-compose.yml` | Shared container runtime configuration |
| `docker-compose.local.yml` | Developer-specific mounts (gitignored) |
| `docker-compose.local.example.yml` | Template for the above |
| `Dockerfile` | Image definition and tool installation |
| `post-create.sh` | One-time setup after container creation |
| `post-start.sh` | Runs on every container start |
| `dotfiles/.editorconfig` | Formatting rules deployed to the container user |
| `dotfiles/opencode.jsonc` | OpenCode configuration deployed to the container user |
| `dotfiles/powershell-profile.ps1` | PowerShell profile for the container |

---

## Repository Layout

```text
.devcontainer/
├── devcontainer.json
├── Dockerfile
├── docker-compose.yml
├── docker-compose.local.yml           ← gitignored, created by you
├── docker-compose.local.example.yml   ← committed template
├── post-create.sh
├── post-start.sh
├── .gitignore
├── .gitattributes
├── .editorconfig
└── dotfiles/
    ├── .editorconfig
    ├── opencode.jsonc
    └── powershell-profile.ps1
```

---

## Devcontainer Features

Features are installed into the image at build time via the `features` key in `devcontainer.json`.

| Feature | Purpose |
|---|---|
| `common-utils` | curl, wget, and other common shell utilities |
| `dotnet` | .NET SDK (primary runtime) |
| `node` | Node.js runtime and npm |
| `python` | Python runtime and pip |
| `powershell` | PowerShell cross-platform shell |
| `github-cli` | `gh` CLI for GitHub operations |
| `copilot-cli` | GitHub Copilot shell suggestions (`gh copilot suggest`) |
| `claude-code` | Claude Code CLI and agent tooling |
| `opencode` | OpenCode CLI and agent tooling |
| `dotnet-csharpier` | CSharpier opinionated C# formatter |
| `prettier` | Prettier formatter for JS, TS, JSON, Markdown, YAML |
| `zsh` | Zsh shell |
| `ohmyposh` | Oh My Posh prompt theming engine |
| `delta` | Enhanced git diff pager with syntax highlighting |
| `lazygit` | Terminal UI for git (commits, branches, rebasing) |
| `zoxide` | Smart directory navigation (`z`, `zi`) |

---

## VS Code Extensions

Extensions are installed into the VS Code Server inside the container via the `customizations.vscode.extensions` key.

### AI & Agents

| Extension | Purpose |
|---|---|
| `anthropic.claude-code` | Claude Code AI coding agent integration |
| `github.copilot-chat` | GitHub Copilot conversational AI assistant |
| `sst-dev.opencode` | OpenCode AI agent integration |

### .NET / C#

| Extension | Purpose |
|---|---|
| `ms-dotnettools.csdevkit` | .NET solution management, project navigation, test runner |
| `ms-dotnettools.csharp` | C# language support, IntelliSense, debugging |
| `csharpier.csharpier-vscode` | CSharpier formatter integration |

### Python

| Extension | Purpose |
|---|---|
| `ms-python.python` | Python language support and tooling |
| `ms-python.vscode-pylance` | Pyright-based Python language server |

### DevOps & Containers

| Extension | Purpose |
|---|---|
| `ms-azuretools.vscode-docker` | Docker and container tooling |
| `github.vscode-github-actions` | GitHub Actions workflow syntax and validation |

### Shell & Scripting

| Extension | Purpose |
|---|---|
| `ms-vscode.powershell` | PowerShell language support and integrated terminal |

### Formatting & Linting

| Extension | Purpose |
|---|---|
| `esbenp.prettier-vscode` | Prettier formatter for web and config files |
| `davidanson.vscode-markdownlint` | Markdown linting and style enforcement |
| `editorconfig.editorconfig` | EditorConfig support for consistent formatting |
| `streetsidesoftware.code-spell-checker` | Spell checking for comments, docs, and Markdown |

### Productivity

| Extension | Purpose |
|---|---|
| `eamodio.gitlens` | Git blame, history, and repository insights |
| `gruntfuggly.todo-tree` | Aggregates TODO, FIXME, HACK, NOTE comments |
| `humao.rest-client` | REST API testing via `.http` files |
| `oderwat.indent-rainbow` | Alternating indentation colors |
| `tyriar.sort-lines` | Alphabetically sort selected lines |
| `yzhang.markdown-all-in-one` | Markdown shortcuts and TOC generation |

---

## .NET Global Tools

Installed by `post-create.sh` via `dotnet tool install -g`. Idempotent — updates if already present.

| Tool | Purpose |
|---|---|
| `dotnet-ef` | Entity Framework Core migrations and scaffolding |
| `dotnet-format` | Opinionated C# code style and analyzer fixer |
| `dotnet-monitor` | Production diagnostics — captures traces, dumps, GC info from running processes |
| `dotnet-outdated-tool` | Identifies outdated NuGet package references |
| `dotnet-script` | Run C# scripts (`.csx`) directly from the terminal |
| `dotnet-trace` | Collect runtime performance traces |
| `dotnet-counters` | Live performance counter monitoring |
| `dotnet-dump` | Capture and analyze memory dumps |
| `csharp-ls` | C# language server (used by the Claude Code `csharp-lsp` plugin) |

---

## AI Agents

### Claude Code

Claude Code is pre-configured with two LSP plugins installed from the official Anthropic marketplace (`claude-plugins-official`):

| Plugin | Purpose |
|---|---|
| `csharp-lsp` | C# language server diagnostics fed into the agent loop |
| `pyright-lsp` | Python language server diagnostics fed into the agent loop |

Plugin state (marketplace metadata, installed plugins, cache) is stored in the `claude-runtime` named Docker volume so it persists across rebuilds independently of auth and skills data.

The `claude-data` volume holds auth tokens, settings, and history. Skills from the workspace `.claude-skills` directory are copied into the volume on first create.

### OpenCode

OpenCode is configured via `~/.config/opencode/opencode.jsonc` (deployed from `dotfiles/opencode.jsonc` by `post-create.sh`).

LSP is enabled with `"lsp": true`, which activates all built-in language servers:

| Built-in Server | Extensions |
|---|---|
| `csharp` | `.cs`, `.csx` (requires .NET SDK) |
| `python` / pyright | `.py` |
| `bash` | `.sh`, `.bash`, `.zsh` |
| `typescript` | `.ts`, `.tsx`, `.js`, `.jsx` |
| `fsharp` | `.fs`, `.fsi`, `.fsx` |
| ... and more | See [opencode.ai/docs/lsp](https://opencode.ai/docs/lsp/) |

LSP servers start lazily when a matching file is opened.

---

## Pre-commit Hooks

The container installs the [pre-commit](https://pre-commit.com) framework and deploys a `.pre-commit-config.yaml` to the workspace root. To activate:

```bash
cd /workspace && pre-commit install
```

Once installed, the following hooks run on every `git commit`:

| Hook | What it checks | Configuration |
|---|---|---|
| `trailing-whitespace` | Removes trailing whitespace from all files | — |
| `end-of-file-fixer` | Ensures files end with a trailing newline | — |
| `check-yaml` | Validates YAML syntax | — |
| `check-added-large-files` | Rejects files >512 KB | `maxkb: 512` |
| `codespell` | Spots common misspellings in source code | Ignore list in config |
| `markdownlint` | Lints Markdown files | Disables `MD013` (line length) |
| `csharpier` | Re-formats C# files with CSharpier | System-installed `dotnet csharpier` |

To update hook versions to the latest available:

```bash
pre-commit autoupdate
```

To run on all files without committing (useful for CI or first-time setup):

```bash
pre-commit run --all-files
```

---

## Lifecycle Scripts

### `post-create.sh`

Runs once after the container is first created. Responsibilities:

- Configures the git config directory
- Installs Oh My Zsh and sets Zsh as the default shell
- Configures Oh My Posh for Zsh and appends to `.zshrc`
- Installs .NET global tools (`dotnet-ef`, `dotnet-format`, `dotnet-monitor`, `dotnet-script`, `dotnet-trace`, `dotnet-counters`, `dotnet-dump`, `dotnet-outdated`, `csharp-ls`)
- Installs the Python language server (`pyright`) via npm
- Creates and chowns the `claude-data` and `claude-runtime` volume directories
- Redirects volatile Claude state (plugins, marketplaces, cache) via symlinks into the `claude-runtime` volume
- Copies workspace `.claude-skills` into the `claude-data` volume (no-clobber)
- Registers the official Anthropic plugin marketplace and installs `csharp-lsp` and `pyright-lsp`
- Deploys `opencode.jsonc` to `~/.config/opencode/`
- Configures Zsh completions (dotnet, git) and shell aliases
- Installs `pre-commit` framework and deploys `.pre-commit-config.yaml` to the workspace
- Restores NuGet packages for any `.sln` files found in the workspace
- Configures the PowerShell profile with Oh My Posh

### `post-start.sh`

Runs every time the container starts. Responsibilities:

- Marks the workspace as a trusted git directory (`safe.directory`)
- Sets `core.autocrlf false` for consistent line ending handling
- Suppresses detached HEAD warnings (`advice.detachedHead false`)

---

## Persistent Storage

### Named Docker Volumes

| Volume | Mount Point | Contents |
|---|---|---|
| `claude-data` | `/home/vscode/.claude` | Claude Code auth, settings, history |
| `claude-runtime` | `/home/vscode/.claude-runtime` | Plugins, marketplace metadata, cache |
| `nuget-cache` | `/root/.nuget/packages` | NuGet package cache |

Named volumes persist across container rebuilds. `claude-data` and `claude-runtime` are kept separate so plugin state can be wiped independently of credentials and skills.

### Host Bind Mounts

Configured in `docker-compose.local.yml` with paths supplied via environment variables:

| Host Path | Container Path | Mode | Purpose |
|---|---|---|---|
| `$CLAUDE_DIR/auth` | `/home/vscode/.claude/auth` | read-write | Claude auth tokens from host |
| `$CLAUDE_DIR/skills` | `/home/vscode/.claude/skills` | read-only | Personal skills from host |
| `$GITCONFIG_PATH` | `/home/vscode/.gitconfig` | read-only | Git identity |
| `$SSH_DIR` | `/home/vscode/.ssh` | read-only | SSH keys for git |

---

## Initial Developer Setup

### Step 1 — Docker Desktop (Windows only)

Enable file sharing for the drive containing your home directory.

**Docker Desktop → Settings → Resources → File Sharing**

Add `C:\Users` (or your home drive root) and apply. Without this, bind mounts will silently fail.

---

### Step 2 — Copy the Local Override File

```bash
cp .devcontainer/docker-compose.local.example.yml .devcontainer/docker-compose.local.yml
```

This file is gitignored and must never be committed.

---

### Step 3 — Set Environment Variables

#### Windows (PowerShell) — persistent user environment variables

```powershell
[System.Environment]::SetEnvironmentVariable("CLAUDE_DIR",     "$HOME\.claude",    "User")
[System.Environment]::SetEnvironmentVariable("GITCONFIG_PATH", "$HOME\.gitconfig",  "User")
[System.Environment]::SetEnvironmentVariable("SSH_DIR",        "$HOME\.ssh",        "User")
```

Verify they were set:

```powershell
[System.Environment]::GetEnvironmentVariable("CLAUDE_DIR",     "User")
[System.Environment]::GetEnvironmentVariable("GITCONFIG_PATH", "User")
[System.Environment]::GetEnvironmentVariable("SSH_DIR",        "User")
```

After setting them, fully restart VS Code and Docker Desktop.

> **Note:** PowerShell profile variables (`$env:VAR = ...`) are not sufficient. VS Code does not source your PowerShell profile at launch. Use `[System.Environment]::SetEnvironmentVariable` with `"User"` scope, or use the `.env` file alternative below.

#### Linux / macOS

Add to `~/.bashrc`, `~/.zshrc`, or equivalent:

```bash
export CLAUDE_DIR="$HOME/.claude"
export GITCONFIG_PATH="$HOME/.gitconfig"
export SSH_DIR="$HOME/.ssh"
```

Then reload: `source ~/.zshrc`

#### Alternative — `.env` file

Create `.devcontainer/.env` (gitignored):

```env
CLAUDE_DIR=C:\Users\YourName\.claude
GITCONFIG_PATH=C:\Users\YourName\.gitconfig
SSH_DIR=C:\Users\YourName\.ssh
```

This bypasses the system environment variable requirement on all platforms.

---

### Step 4 — Verify Paths Exist on the Host

```powershell
# Windows
Test-Path $env:CLAUDE_DIR
Test-Path $env:SSH_DIR
Test-Path $env:GITCONFIG_PATH
```

```bash
# Linux / macOS
ls ~/.claude ~/.ssh ~/.gitconfig
```

If `~/.claude` does not exist yet, create it:

```powershell
# Windows
mkdir $HOME\.claude
```

```bash
# Linux / macOS
mkdir -p ~/.claude
```

---

### Step 5 — Open in VS Code

Open the repository in VS Code and select **Reopen in Container**, or run from the Command Palette (`Ctrl+Shift+P`):

```
Dev Containers: Rebuild and Reopen in Container
```

---

## Browsing Claude Configuration Inside the Container

The Claude configuration directory is mounted at `/home/vscode/.claude`. It is not shown in the VS Code file explorer by default since the workspace root is `/workspace`.

**Terminal:**

```bash
ls -la ~/.claude
```

**Open as a second VS Code window:**

```bash
code ~/.claude
```

**Add to the current workspace:** File → Add Folder to Workspace → `/home/vscode/.claude`

**Symlink into the workspace** (shows in the file explorer):

```bash
ln -s ~/.claude /workspace/.claude
```

If you do this, add `.claude` to your project's `.gitignore`.

---

## Troubleshooting

### Container starts then immediately exits

Almost always caused by a failing bind mount. If Docker Desktop does not have file sharing enabled, the container will crash silently.

Confirm **Docker Desktop → Settings → Resources → File Sharing** includes `C:\Users`, then fully restart Docker Desktop before retrying.

To isolate the failing mount, comment out volumes one at a time in `docker-compose.local.yml` and rebuild. You can also test the image directly without any compose mounts:

```powershell
docker run --rm -it --user vscode llm-dev-container_devcontainer-devcontainer /bin/bash
```

If that shell opens successfully, the image is healthy and the issue is a mount.

---

### Environment variables not found (`variable is not set`)

VS Code does not inherit PowerShell profile variables. Set them as Windows user environment variables:

```powershell
[System.Environment]::SetEnvironmentVariable("CLAUDE_DIR", "$HOME\.claude", "User")
```

Then fully restart VS Code. Alternatively use the `.env` file approach.

---

### SSH keys not available inside the container

Verify `SSH_DIR` is set and points to a directory that exists:

```powershell
[System.Environment]::GetEnvironmentVariable("SSH_DIR", "User")
Test-Path ([System.Environment]::GetEnvironmentVariable("SSH_DIR", "User"))
```

---

### Git warns about unsafe directory ownership

Handled automatically by `post-create.sh` and `post-start.sh`. If it appears anyway, run manually:

```bash
git config --global --add safe.directory $(pwd)
```

---

### CSharpier not found on PATH

The `dotnet-csharpier` feature installs CSharpier to `/root/.dotnet/tools`, which is not on the PATH for the `vscode` user. Add it temporarily:

```bash
export PATH="$PATH:/root/.dotnet/tools"
```

To make it permanent, add it to `~/.zshrc` or handle it in `post-start.sh`.

---

### Claude Code plugins fail to install

If the marketplace or plugins fail during `post-create.sh`, you can re-run them manually from the terminal inside the container:

```bash
claude plugin marketplace add anthropics/claude-plugins-official
claude plugin marketplace update claude-plugins-official
claude plugin install csharp-lsp@claude-plugins-official
claude plugin install pyright-lsp@claude-plugins-official
```

---

### postCreateCommand fails with exit code 1

Enable debug logging to see which line is failing:

```json
"postCreateCommand": "bash .devcontainer/post-create.sh 2>&1 | tee /workspace/.devcontainer/post-create.log; exit ${PIPESTATUS[0]}"
```

After the failure, open `.devcontainer/post-create.log` in VS Code for a clean output.

---

## Notes

### Line Endings

Line endings are standardized through a combination of `.editorconfig` (per-directory), VS Code settings (`files.eol: "\n"`), `.gitattributes` (per-repository), and `git config core.autocrlf false` (set in `post-start.sh`). Shell scripts must have LF endings — CRLF causes bash to fail with `$'\r': command not found`.

### `devcontainer-lock.json` — Pinning Feature Versions

The lock file pins exact versions of all devcontainer features for reproducible builds. To update pinned versions (e.g. after a new SDK release), delete the lock file and rebuild:

```bash
rm .devcontainer/devcontainer-lock.json
# Then: Dev Containers: Rebuild and Reopen in Container
```

The lock file is auto-regenerated on rebuild. Check it in to keep builds deterministic.

### Non-root User

The container runs as the `vscode` user (non-root). The `postCreateCommand` also runs as `vscode`, so operations on named Docker volumes require `sudo` if the volume was initialized with root ownership.

### `docker-compose.local.yml` Must Not Be Committed

This file holds personal host paths. It is listed in `.devcontainer/.gitignore`. If it appears as a tracked file, remove it:

```bash
git rm --cached .devcontainer/docker-compose.local.yml
```

---

## Philosophy

This setup prioritizes reproducibility and portability while keeping personal configuration (AI tooling, SSH keys, git identity) out of the repository. Shared tooling is defined once and inherited by all developers. Personal configuration is supplied at runtime through mounts and never baked into the image.
