# Build environment

The SDK builds a file-based app from a virtual project that matches a `dotnet new console` project, plus a few file-based defaults. This file covers those defaults, what the app inherits from its folder, and where build output goes.

## Implicit properties

| Property | Value |
| --- | --- |
| `OutputType` | `Exe` |
| `TargetFramework` | `net<SDK major>`: `net10.0` on a 10.0 SDK, `net11.0` on an 11.0 SDK |
| `ImplicitUsings`, `Nullable` | `enable` |
| `PublishAot` | `true` |
| `PackAsTool` | `true` |
| `UserSecretsId` | `<name>-<sha256 of the file path>` |
| `AssemblyName`, `RootNamespace` | the file name without `.cs` |

To change one, use `#:property` (for example `#:property TargetFramework=net10.0` to stay on .NET 10 when the machine's newest SDK is 11).

## Native AOT is on, even for `dotnet run`

`PublishAot=true` affects more than `dotnet publish`. It also turns on the trim and AOT analyzers, and it turns off reflection-based `System.Text.Json` at run time. The effects:

- **IL2026 / IL3050 warnings on build mean the code fails now, not only at publish.** For example, `JsonSerializer.Serialize(obj)` in a console app warns at build, then throws "Reflection-based serialization has been disabled" when it runs.
- **Web SDK apps fail with no warning.** A minimal API endpoint that returns an object (`app.MapGet("/x", () => new Greeting(...))`) builds clean, then returns HTTP 500 for every request: "JsonTypeInfo metadata for type 'Greeting' was not provided".

Fix JSON by keeping AOT and adding a source-generated context that lists every type you serialize:

```csharp
#:sdk Microsoft.NET.Sdk.Web
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJson.Default));
var app = builder.Build();
app.MapGet("/hello/{name}", (string name) => new Greeting($"Hello, {name}!"));
app.Run();

record Greeting(string Message);

[JsonSerializable(typeof(Greeting))]
partial class AppJson : JsonSerializerContext;
```

In console code, pass the context directly: `JsonSerializer.Serialize(value, AppJson.Default.Greeting)`.

**Opt out of AOT only when a package causes it.** When the build shows IL2xxx/IL3xxx warnings that come from a package's API (not from JSON you can source-generate), add the opt-out with a comment that names the cause:

```csharp
#:package SomeReflectionHeavyLib@1.2.3
#:property PublishAot=false   // SomeReflectionHeavyLib uses reflection (IL2026 on build)
```

A package with no trim annotations (CsvHelper 33, for example) produces no warning and runs normally under `dotnet run`. Leave AOT on for those.

Other AOT effects:

- `dotnet publish app.cs` compiles to native code and needs the platform toolchain. On Linux, without `clang` or `gcc` it fails: "Platform linker ('clang' or 'gcc') not found in PATH". Install the [Native AOT prerequisites](https://aka.ms/nativeaot-prerequisites), or publish with `#:property PublishAot=false`.
- On 10.0, `dotnet watch app.cs` can't hot-reload an AOT app, so it restarts the app on each change instead. Add `#:property StartupHookSupport=true` to get hot reload. On 11, hot reload works with no change.
- Restore downloads the ILCompiler package even for `dotnet run`.

## Inherited files

The virtual project imports these from the entry file's folder and every parent folder, the same as a real project:

- `Directory.Build.props` / `Directory.Build.targets`
- `Directory.Packages.props` (central package management: changes how you write `#:package`, see [directives.md](directives.md))
- `NuGet.config`
- `global.json`. This selects the SDK, which also selects the default `TargetFramework` and which directives exist.

Precedence: the SDK defaults above load first, then `Directory.Build.props`, then your `#:property` lines. So `Directory.Build.props` can override `PublishAot`, and a `#:property` overrides `Directory.Build.props`.

Before you write a file-based app inside an existing repo, look for these files in its parent folders. A repo-wide `TreatWarningsAsErrors` turns the AOT warnings above into build errors. `RestoreUseStaticGraphEvaluation=true` is not supported for file-based apps.

**Folder layout.** Give scripts their own folder (for example `scripts/`), outside any project's folder tree. A `.csproj` in the same folder changes what `dotnet run app.cs` does ([running-and-cli.md](running-and-cli.md)). A project whose glob reaches the script fails to compile with CS9298. Put a `Directory.Build.props` in the scripts folder if the scripts need settings that differ from the repo's.

## Build output and cache

| OS | Location |
| --- | --- |
| Linux | `~/.local/share/dotnet/runfile/<name>-<sha256>/` |
| macOS | `~/Library/Application Support/dotnet/runfile/<name>-<sha256>/` |
| Windows | `%TEMP%\dotnet\runfile\<name>-<sha256>\` |

- The hash comes from the entry file's full path. Moving or renaming the file starts a new cache folder, and loses access to user secrets stored under the old ID.
- `dotnet publish app.cs` writes to `./artifacts/<name>/` next to the file. Add `artifacts/` to `.gitignore`.
- The SDK skips the build when nothing changed. It rebuilds when the source, the directives, the SDK version or an implicit build file changes. It does not track files imported by `Directory.Build.props`, and a `#:include` glob turns caching off. Force a rebuild with `dotnet run app.cs --no-cache` or `dotnet build app.cs`.
- Two parallel runs of the same file can collide on outputs. Build once, then start each run with `--no-build`.
- `dotnet clean file-based-apps` removes cache folders unused for 30 days (`--days N`, `--dry-run`). The SDK also does this on its own every two days.

## Launch profiles

`app.run.json` next to `app.cs` holds launch profiles in the `launchSettings.json` schema. If a `Properties/launchSettings.json` also exists, it wins, with a warning. Select one with `--launch-profile <name>`, or skip both with `--no-launch-profile`.
