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

| Category       | Tools                                        |
| -------------- | -------------------------------------------- |
| Runtime        | .NET 10 SDK, Python, PowerShell              |
| AI             | Claude Code, OpenCode                        |
| Formatters     | CSharpier, Prettier                          |
| Terminal utils | ripgrep, fd, fzf, zoxide, bat, jq, tree, vim |
| Git            | Git LFS                                      |

---

## Architecture

| File                               | Responsibility                          |
| ---------------------------------- | --------------------------------------- |
| `devcontainer.json`                | VS Code and Dev Container configuration |
| `docker-compose.yml`               | Shared container runtime configuration  |
| `docker-compose.local.yml`         | Developer-specific mounts (gitignored)  |
| `docker-compose.local.example.yml` | Template for the above                  |
| `Dockerfile`                       | Image definition and tool installation  |
| `post-create.sh`                   | One-time setup after container creation |
| `post-start.sh`                    | Runs on every container start           |
| `dotfiles/.editorconfig`           | Repository formatting rules             |
| `dotfiles/powershell-profile.ps1`  | PowerShell profile for the container    |

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
├── README.md
└── dotfiles/
    ├── .editorconfig
    └── powershell-profile.ps1
```

---

## Initial Developer Setup

### Step 1 — Docker Desktop (Windows only)

Enable file sharing for the drive containing your home directory.

**Docker Desktop → Settings → Resources → File Sharing**

Add `C:\Users` (or your home drive root) and apply. Without this, bind mounts will silently fail and the container will crash on startup.

---

### Step 2 — Copy the Local Override File

```bash
# From the repo root
cp .devcontainer/docker-compose.local.example.yml .devcontainer/docker-compose.local.yml
```

This file is gitignored and must never be committed. It holds your personal paths and secrets.

---

### Step 3 — Set Environment Variables

The local compose override references three environment variables to locate host directories. These must be set as persistent user-level variables — shell profile variables alone are not inherited by VS Code on Windows.

#### Windows (PowerShell) — Persistent user environment variables

Run once in PowerShell. These survive reboots and are picked up by VS Code automatically.

```powershell
[System.Environment]::SetEnvironmentVariable("CLAUDE_DIR",     "$HOME\.claude",    "User")
[System.Environment]::SetEnvironmentVariable("GITCONFIG_PATH", "$HOME\.gitconfig",  "User")
[System.Environment]::SetEnvironmentVariable("SSH_DIR",        "$HOME\.ssh",        "User")
```

Verify they were set correctly:

```powershell
[System.Environment]::GetEnvironmentVariable("CLAUDE_DIR",     "User")
[System.Environment]::GetEnvironmentVariable("GITCONFIG_PATH", "User")
[System.Environment]::GetEnvironmentVariable("SSH_DIR",        "User")
```

After setting them, fully restart VS Code (and Docker Desktop if it was already running).

> **Note:** PowerShell profile variables (`$env:VAR = ...`) are not sufficient. VS Code does not source your profile at launch and will not see them. Use `[System.Environment]::SetEnvironmentVariable` with `"User"` scope.

#### Linux / macOS — Shell profile

Add to `~/.bashrc`, `~/.zshrc`, or equivalent:

```bash
export CLAUDE_DIR="$HOME/.claude"
export GITCONFIG_PATH="$HOME/.gitconfig"
export SSH_DIR="$HOME/.ssh"
```

Then reload:

```bash
source ~/.zshrc   # or ~/.bashrc
```

#### Alternative — `.env` file

Docker Compose automatically reads a `.env` file from the same directory as the compose file. This bypasses the environment variable requirement entirely and works on all platforms without a restart.

Create `.devcontainer/.env`:

```env
CLAUDE_DIR=C:\Users\YourName\.claude
GITCONFIG_PATH=C:\Users\YourName\.gitconfig
SSH_DIR=C:\Users\YourName\.ssh
```

This file is gitignored. It is a good fallback if the system environment variable approach causes issues.

---

### Step 4 — Verify Paths Exist on the Host

Before opening the container, confirm the directories being mounted actually exist:

```powershell
# Windows PowerShell
Test-Path $env:CLAUDE_DIR
Test-Path $env:SSH_DIR
Test-Path $env:GITCONFIG_PATH
```

```bash
# Linux / macOS
ls ~/.claude ~/.ssh ~/.gitconfig
```

If `~/.claude` does not exist yet (i.e. you have not run Claude Code locally), create it:

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

Open the repository in VS Code and when prompted select **Reopen in Container**, or run from the Command Palette (`Ctrl+Shift+P`):

```
Dev Containers: Rebuild and Reopen in Container
```

---

## Browsing Claude Configuration Inside the Container

The Claude configuration directory is mounted at `/home/vscode/.claude`. It is not shown in the VS Code file explorer by default since the workspace root is `/workspace`.

Options for accessing it:

**Terminal:**

```bash
ls -la ~/.claude
```

**Open as a second VS Code window:**

```bash
code ~/.claude
```

**Add to the current workspace** (File → Add Folder to Workspace → `/home/vscode/.claude`).

**Symlink into the workspace** (shows up in the file explorer):

```bash
ln -s ~/.claude /workspace/.claude
```

If you do this, add `.claude` to your project's `.gitignore`.

---

## Lifecycle Scripts

### `post-create.sh`

Runs once after the container is first created. Responsible for:

- Marking the workspace as a trusted git directory
- Restoring NuGet packages for any `.sln` files found in the workspace

### `post-start.sh`

Runs every time the container starts. Responsible for:

- Re-asserting git safe directory trust
- Setting `core.autocrlf false` for consistent line ending handling
- Suppressing detached HEAD warnings

---

## Persistent Storage

### NuGet Cache

NuGet packages are stored in a named Docker volume (`nuget-cache`) rather than inside the container. This means packages survive container rebuilds and do not need to be re-downloaded each time.

### Claude Configuration

The host `.claude` directory is bind-mounted into the container. Skills, memory, and agent state persist across container rebuilds because they live on the host, not inside the image.

---

## Troubleshooting

### Container starts then immediately exits

This is almost always caused by one of two things:

**A bind mount is failing.** If Docker Desktop does not have file sharing enabled for your home directory drive, the container will crash silently on startup. Confirm **Docker Desktop → Settings → Resources → File Sharing** includes `C:\Users` (or equivalent), then restart Docker Desktop fully before retrying.

**To isolate the failing mount**, comment out volumes one at a time in `docker-compose.local.yml` and rebuild until the container stays up. Then re-enable them to find the offender.

You can also test the image directly without any compose mounts:

```powershell
docker run --rm -it --user vscode llm-dev-container_devcontainer-devcontainer /bin/bash
```

If that shell opens successfully, the image is healthy and the issue is a mount.

---

### Environment variables not found (`variable is not set`)

VS Code does not inherit PowerShell profile variables. Set them as Windows user environment variables instead:

```powershell
[System.Environment]::SetEnvironmentVariable("CLAUDE_DIR", "$HOME\.claude", "User")
```

Then fully restart VS Code. If you need to avoid a restart, use the `.env` file approach described in Step 3.

---

### SSH keys not available inside the container

Verify `SSH_DIR` is set and points to a directory that exists:

```powershell
[System.Environment]::GetEnvironmentVariable("SSH_DIR", "User")
Test-Path ([System.Environment]::GetEnvironmentVariable("SSH_DIR", "User"))
```

---

### Git warns about unsafe directory ownership

This is handled automatically by `post-create.sh` and `post-start.sh`. If you see it anyway, run manually inside the container:

```bash
git config --global --add safe.directory $(pwd)
```

---

### CSharpier not found on PATH after container build

The `dotnet-csharpier` feature installs CSharpier to `/root/.dotnet/tools`, which is not automatically on the PATH for the `vscode` user. If `csharpier` is not found in the terminal, add it:

```bash
export PATH="$PATH:/root/.dotnet/tools"
```

To make it permanent, add it to `~/.bashrc` inside the container or handle it in `post-start.sh`.

---

## Notes

### Line Endings

Line endings are standardized through a combination of `.editorconfig`, VS Code settings (`files.eol: "\n"`), and `git config core.autocrlf false` (set in `post-start.sh`). The `.gitattributes` file at the repo root controls per-file-type normalization for committed content.

### Non-root User

The container runs as the `vscode` user (non-root). This avoids file permission issues with bind-mounted directories that are owned by a regular user on the host. The `updateRemoteUserUID` setting in `devcontainer.json` synchronizes the container UID with the host user UID where supported.

### `docker-compose.local.yml` Must Not Be Committed

This file holds your personal host paths. It is listed in `.devcontainer/.gitignore`. If it appears as a tracked file, remove it from tracking:

```bash
git rm --cached .devcontainer/docker-compose.local.yml
```

---

## Philosophy

This setup prioritizes reproducibility and portability while keeping personal configuration (AI tooling, SSH keys, git identity) out of the repository. Shared tooling is defined once and inherited by all developers. Personal configuration is supplied at runtime through mounts, never baked into the image.
