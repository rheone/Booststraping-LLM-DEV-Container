# Running and the CLI

## Which command runs the file

| Command | What it runs |
| --- | --- |
| `dotnet app.cs` | The file, always. Use this by default. |
| `dotnet run --file app.cs` | The file, always. Use it when you need `dotnet run` options. |
| `dotnet run app.cs` | The file, **unless a project file is in the current directory.** Then it runs that project and passes `app.cs` to it as an argument, with the warning "'app.cs' appears to be a file-based app but was passed as an argument to the project". |
| `./app.cs` | The file, on Unix, when it has a shebang and `chmod +x`. See [shebang-scripts.md](shebang-scripts.md). |
| `echo '<code>' \| dotnet run -` | Code from stdin. It finds no launch profile or other files. Relative `#:project` paths resolve against a temp folder, so write them as `$(MSBuildStartupDirectory)/path`. |

- `dotnet app.cs` doesn't work when the name matches a built-in command or an installed tool (`dotnet watch` runs the tool). Write `dotnet ./watch.cs` or use `--file` in that case.
- The target must end in `.cs` or start with `#!`, so extensionless shebang files also work.
- The working directory stays where you ran the command. `cd /x && dotnet /y/app.cs` runs with `/x` as the current directory. To find the script's own folder, read `AppContext.GetData("EntryPointFileDirectoryPath")` (not set for published apps).

## Passing arguments

```bash
dotnet app.cs -- --verbose input.txt     # args = ["--verbose", "input.txt"]
dotnet run app.cs -- --verbose input.txt
```

Put `--` before the app's arguments. Without it, `dotnet run` consumes any argument it recognizes (`-c`, `-v`, `--help`, and so on).

## Build, publish, pack

| Command | Result |
| --- | --- |
| `dotnet build app.cs` | Builds into the cache folder ([build-environment.md](build-environment.md)). `-o <dir>` or `#:property OutputPath=...` to change. |
| `dotnet run app.cs --no-build` | Runs the last build. Use it after one build when you start several runs. |
| `dotnet run app.cs --no-cache` | Skips the up-to-date check and rebuilds. |
| `dotnet restore app.cs` | Restores only. `--no-restore` on build/run skips the implicit restore. |
| `dotnet publish app.cs` | Native AOT publish to `./artifacts/<name>/`. Needs the native toolchain. |
| `dotnet pack app.cs` | Packs as a .NET tool (`PackAsTool=true`). |
| `dotnet clean app.cs` | Cleans this app's output. |
| `dotnet clean file-based-apps` | Removes stale cache folders (`--days`, `--dry-run`). |
| `dotnet format app.cs` | SDK 11. |
| `dotnet project convert app.cs` | See [convert-to-project.md](convert-to-project.md). |

`dotnet run` also takes `-e KEY=VALUE`, `--launch-profile <name>` and `--no-launch-profile`.

## Commands that edit directives

These take `--file app.cs` and rewrite the directive lines for you:

```bash
dotnet package add Humanizer --version 3.0.10 --file app.cs    # writes #:package Humanizer@3.0.10
dotnet package remove Humanizer --file app.cs
dotnet reference add ../Shared/Shared.csproj --file app.cs      # writes #:project ...
dotnet user-secrets set ApiKey abc123 --file app.cs
dotnet user-secrets list --file app.cs
```

`dotnet reference … --file` and `dotnet user-secrets … --file` were tested on 10.0.404 and 11.0.100. Older 10.0 bands were not tested. If they fail there, write the `#:project` line by hand. For secrets, convert, or read the values from environment variables.

The user-secrets ID is a hash of the file's full path. Moving the file loses access to its secrets.

## `dotnet watch`

```bash
dotnet watch app.cs
```

This rebuilds and restarts the app when the file changes. Tested on 10.0.404 and 11.0.100. On 10.0 it can't hot-reload a file-based app because AOT is on, so it restarts the app instead (add `#:property StartupHookSupport=true` for hot reload). On 11, hot reload works.

## Exit codes

Top-level statements can `return <int>;`, and `dotnet app.cs` exits with that code. Scripts that other tools call should return non-zero on failure and write errors to `Console.Error`.
