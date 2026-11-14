---
name: dotnet-file-based-apps
description: Write, run, and convert .NET file-based apps, single `.cs` files run with `dotnet app.cs` or `dotnet run app.cs` and no `.csproj` (.NET 10+). Covers `#:package`, `#:sdk`, `#:property`, `#:project`, `#:include`/`#:exclude` and `#:ref` directives and which SDK band supports each; shebang scripts; the PublishAot default and its run-time JSON failures; cache location; Directory.Build.props/CPM inheritance; and `dotnet project convert`. Use when writing a C# script or one-file tool or API, when a `.cs` file starts with `#:` or `#!`, when `dotnet run file.cs` fails or runs the wrong project, or when moving a script to a full project.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# .NET file-based apps

A **file-based app** is one C# **entry file** that the SDK builds and runs with no project file. `#:` **directives** at the top of the file take the place of the `.csproj`. The feature shipped in .NET 10. Which directives work depends on the **SDK band** (10.0.1xx, 10.0.3xx, 10.0.4xx, 11.0.1xx), so check the band before you write.

When the user asks for a "C# script" or a one-file tool, write a file-based app. If the repo already uses `.csx` scripts (dotnet-script), follow the repo instead.

## Quick start

```csharp
#!/usr/bin/env -S dotnet --
#:package Humanizer@3.0.10

using Humanizer;

Console.WriteLine("file_based_app".Humanize());
return 0;
```

```bash
dotnet app.cs -- arg1 arg2
```

## Rules every file-based app needs

1. **Check the SDK band first.** Run `dotnet --version` in the file's folder (`global.json` can pin it). `#:include`/`#:exclude` need 10.0.300+. `#:ref` needs 11, or a flag on 10.0.3xx/4xx. Quoted directive values need 11.
2. **Directives come first and use separators, not spaces.** The shebang goes on line 1, then the `#:` lines, then code. Write `#:package Name@1.2.3` and `#:property Name=Value`. `#:property LangVersion preview` (with a space) is an error.
3. **Pin package versions.** Write the exact current version (look it up with `dotnet package search <id> --exact-match`). A missing version is error NU1015, not "latest". Under central package management (`Directory.Packages.props` in a parent folder), leave the version out, because `@version` is error NU1008.
4. **Run with `dotnet app.cs`.** `dotnet run app.cs` runs the folder's project instead when a `.csproj` sits in the current directory. `dotnet app.cs` and `dotnet run --file app.cs` always run the file.
5. **AOT is on by default, and it changes run-time behavior.** `PublishAot=true` turns off reflection-based JSON even under `dotnet run`. Serialize through a source-generated `JsonSerializerContext`. A Web SDK app that returns objects without one builds clean, then returns 500 on every request.
6. **Verify before you report done.** Build, read every warning, and run with real arguments ([references/testing.md](references/testing.md)).

## Pick your reference file

| You're doing this… | Reference file |
| --- | --- |
| Writing or fixing any `#:` line; SDK-band support matrix | [references/directives.md](references/directives.md) |
| Running, passing args, build/publish/pack, stdin, `watch`, user secrets, `.csproj` in the folder | [references/running-and-cli.md](references/running-and-cli.md) |
| AOT warnings or JSON failures, `Directory.Build.props`/`global.json`/CPM, cache folder, launch profiles | [references/build-environment.md](references/build-environment.md) |
| Splitting into several files, sharing a library file, referencing a project or DLL | [references/multi-file.md](references/multi-file.md) |
| Making `./tool.cs` executable on Linux/macOS, BOM and line endings | [references/shebang-scripts.md](references/shebang-scripts.md) |
| Deciding whether to convert to a project, and running `dotnet project convert` | [references/convert-to-project.md](references/convert-to-project.md) |
| Verifying a file-based app works | [references/testing.md](references/testing.md) |

## Examples

Tested on SDK 10.0.404 and 11.0.100 (Linux). The folder's `.gitattributes` keeps them LF.

- [assets/examples/hello.cs](assets/examples/hello.cs): console app with arguments
- [assets/examples/package-usage.cs](assets/examples/package-usage.cs): `#:package` with a pinned version
- [assets/examples/web-api.cs](assets/examples/web-api.cs): `#:sdk Microsoft.NET.Sdk.Web` with an AOT-safe JSON context
- [assets/examples/script.cs](assets/examples/script.cs): shebang script with exit codes
