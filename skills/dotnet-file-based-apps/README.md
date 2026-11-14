# .NET file-based apps

Helps an agent write, run, and grow single-file C# programs: `dotnet app.cs` with no `.csproj`, configured by `#:` directives. It targets the .NET 10 and .NET 11 SDKs and labels each feature with the SDK band that supports it, because several directives fail on older bands.

## When to reach for it

- You want a quick C# script, CLI tool or one-file web API without creating a project.
- A `.cs` file with `#:package` or `#!` lines fails to build, or `dotnet run app.cs` runs the wrong thing.
- A file-based web API builds clean but returns HTTP 500.
- A script has grown and you need to split it into files or turn it into a real project.
- You want `./tool.cs` to run as an executable on Linux or macOS.

## Using it

The skill is model-invoked: the agent loads it when the task matches. You can also call it by name with `/dotnet-file-based-apps`. It installs as part of this repository's skill set.

## What it covers

| Topic | Reference |
| --- | --- |
| Directive syntax, `#:package` / `#:sdk` / `#:property` / `#:project`, SDK-band matrix | [references/directives.md](references/directives.md) |
| `dotnet app.cs` vs `dotnet run`, arguments, build/publish/pack, stdin, watch, user secrets | [references/running-and-cli.md](references/running-and-cli.md) |
| Native AOT default and JSON, inherited `Directory.Build.props`/CPM/`global.json`, cache folders | [references/build-environment.md](references/build-environment.md) |
| `#:include`, `#:exclude`, `#:ref`, project and DLL references | [references/multi-file.md](references/multi-file.md) |
| Shebang scripts, BOM and line endings | [references/shebang-scripts.md](references/shebang-scripts.md) |
| When and how to run `dotnet project convert` | [references/convert-to-project.md](references/convert-to-project.md) |
| Verifying a file-based app | [references/testing.md](references/testing.md) |

`assets/examples/` holds four runnable samples. `research/` holds the cited source notes and the Linux test results behind each fact. The agent doesn't load them; they are there for maintainers re-checking a fact against a new SDK.

## Example prompts

- "Write me a C# script that counts lines in every .md file under docs/ and prints the top ten."
- "dotnet run tool.cs prints my web app's startup log instead of running the script. Why?"
- "Make a one-file minimal API with a /health endpoint and a /todos endpoint I can run with dotnet."
- "This file-based app is 800 lines now. Split it up, or should it be a project?"
- "Make tools/release.cs executable on our Linux build agents."
