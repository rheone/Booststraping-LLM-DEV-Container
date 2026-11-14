# Directives

A **directive** is a `#:` line at the top of the **entry file** that configures the virtual project the SDK builds for it. The C# compiler ignores `#:` and `#!` lines; the SDK reads them. This file covers the syntax rules, each directive, and the **SDK band** that supports it. Check the band first with `dotnet --version` (a `global.json` in the file's folder or a parent can pin it).

## Syntax rules (every directive)

- **Placement.** Directives come before the first C# token and before any `#if`. A `#!` shebang, if present, is the very first line; directives follow it. Comments and blank lines between directives are fine.
  - `#:` after code → CS9297. `#:` after `#if` → CS9299.
- **Shape.** `#:kind name<separator>value`. The separator is `@` for `#:package` and `#:sdk`, and `=` for `#:property`. Whitespace is never a separator:

  ```csharp
  #:property LangVersion=preview      // correct
  #:property LangVersion preview      // error on every released SDK
  ```

- **Unknown kinds** are errors ("Unrecognized directive"). On an SDK band that lacks a directive, you get this error, not a silent no-op.
- **File-based mode only.** A `.cs` file with `#:` lines inside a normal `.csproj` fails with CS9298. Keep file-based apps out of a project's compile glob (move them, or add `<Compile Remove="scripts/**" />` to the project).
- **MSBuild expressions** pass through verbatim, so `$(...)` and property functions work in values.
- **Quoted values** (SDK 11 only): wrap a value in `"..."` when it contains spaces or must start/end with whitespace. The value is lexed as a C# regular string literal, so escape backslashes (`"C:\\src\\my lib"`) or use `/`. On SDK 10.0.x any `"` in a directive fails with "Directives currently cannot contain double quotes". On SDK 11, an unquoted value with inner whitespace still works but raises CA2267.
- **Duplicates.**
  - 10.0.1xx–10.0.3xx: any duplicate `kind + name` is an error.
  - 10.0.4xx and 11: a `#:sdk`/`#:property`/`#:package` duplicate with the same value is ignored; a different value is an error. `#:project`/`#:ref`/`#:include`/`#:exclude` may repeat.
- **SDK 11:** a `#:property` name cannot contain `:`. Directive scanning stops at the first `#if`.

## `#:package`

Adds a NuGet `PackageReference`.

```csharp
#:package Humanizer@2.14.1                      // exact version: the default form
#:package Spectre.Console@*                     // latest stable: only when the user asks for latest
#:package System.CommandLine@2.0.0-*            // floating prerelease
```

- **Write the exact current version.** Look it up when you write the directive (`dotnet package search <id> --exact-match`, or nuget.org). Every run then restores the same package.
- **A missing version is an error, not "latest".** `#:package Foo` with no version fails with NU1015, unless central package management is on.
- **Central package management (CPM).** When a `Directory.Packages.props` in the file's folder or a parent sets `ManagePackageVersionsCentrally`, omit the version: `#:package Foo`. Writing `@1.2.3` under CPM fails with NU1008. Check for `Directory.Packages.props` before you write any `#:package`.
- `dotnet package add <id> --file app.cs` and `dotnet package remove <id> --file app.cs` edit the `#:package` lines for you.
- **Item metadata** (SDK 11 only): trailing `Name=Value` tokens become item metadata, e.g. `#:package System.CommandLine@2.0.1 Aliases=CommandLine`. A `#:package` that uses `@version` cannot also set `Version=`.

## `#:sdk`

Selects the project SDK. Default: `Microsoft.NET.Sdk`.

```csharp
#:sdk Microsoft.NET.Sdk.Web
#:sdk Aspire.AppHost.Sdk@13.0.2
```

- The first `#:sdk` is the project SDK; each later one is an additional `<Sdk>` import.
- With the default SDK, the virtual project does not pick up `.json`, `.resx` or other non-code files in the folder. With any other SDK (such as Web), the default `None` and `EmbeddedResource` items stay on, so `appsettings.json` next to the file is included.

## `#:property`

Sets an MSBuild property: `#:property Name=Value` becomes `<Name>Value</Name>`.

```csharp
#:property PublishAot=false
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property LogLevel=$([MSBuild]::ValueOrDefault('$(LOG_LEVEL)', 'Information'))
```

- A `#:property` overrides `Directory.Build.props`. The SDK's own defaults (`PublishAot`, `PackAsTool`, and so on) are set before `Directory.Build.props` loads, so `Directory.Build.props` can override those. See [build-environment.md](build-environment.md).
- `#:property RestoreUseStaticGraphEvaluation=true` is an error. Static graph restore is not supported.

## `#:project`

References a project.

```csharp
#:project ../SharedLibrary/SharedLibrary.csproj
#:project ../SharedLibrary          // a folder that holds exactly one project file
```

- Relative paths resolve from the file that holds the directive.
- `dotnet reference add <path> --file app.cs` (and `list`/`remove`) manage these lines. See [multi-file.md](multi-file.md) for which SDK band supports it.

## `#:include`, `#:exclude`, `#:ref`

Multi-file directives, with the strictest SDK-band gates. See [multi-file.md](multi-file.md).

## SDK-band matrix

| Feature | 10.0.1xx–2xx | 10.0.3xx | 10.0.4xx | 11.0.1xx |
| --- | --- | --- | --- | --- |
| `#:package`, `#:sdk`, `#:property`, `#:project` | yes | yes | yes | yes |
| `#:include` / `#:exclude` | no | yes | yes | yes |
| `#:ref` | no | needs flag | needs flag | yes |
| Directives inside included files | — | needs flag | yes | yes |
| Same-value duplicate directives | error | error | ignored | ignored |
| Quoted values, item metadata | no | no | no | yes |

When the band is too old for a feature, either pin a newer SDK in `global.json`, or use the fallback in [multi-file.md](multi-file.md), or convert to a project ([convert-to-project.md](convert-to-project.md)).
