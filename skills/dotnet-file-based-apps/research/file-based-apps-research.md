# .NET file-based apps (`dotnet run app.cs`) — research notes

Compiled 2026-11-13 to inform a future skill, `skills/dotnet-file-based-apps`, that teaches an agent to write,
run, and convert .NET file-based apps. Every claim is cited to a primary source: Microsoft Learn, the
dotnet/sdk spec and source (pinned to commit SHAs), the dotnet/roslyn IDE spec, the C# language
proposal, the dotnet/core release metadata, and the official .NET blog. Where a claim rests only on a local
test run, it is labeled **LOCAL-TEST**. Where nothing confirms it, it is labeled **UNVERIFIED**.

Short labels used below:

| Label | Source |
|---|---|
| **LEARN-FBA** | https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps (the reference page; "applies to .NET 10 SDK and later") |
| **LEARN-RUN** | https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run |
| **LEARN-CONVERT** | https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-project-convert (ms.date 10/25/2025) |
| **LEARN-TUT** | https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/file-based-programs (ms.date 01/12/2026) |
| **LEARN-PP** | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#file-based-apps |
| **LEARN-PPERR** | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/preprocessor-errors#incorrect-use-of-file-based-apps-directives |
| **LANG-SPEC** | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-14.0/ignored-directives (champion issue https://github.com/dotnet/csharplang/issues/8617) |
| **WN10** | https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk#file-based-apps-enhancements |
| **WN11** | https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk (ms.date 11/10/2026, "last updated for the general availability (GA) release") |
| **SPEC@main** | https://github.com/dotnet/sdk/blob/6966313592c2850f7faaa886616df32e6f26a95e/documentation/general/dotnet-run-file.md (2026-10-30) |
| **SPEC@10.0.1xx** | https://github.com/dotnet/sdk/blob/e74736c05a7a98f32973903fc454aff3c13314f3/documentation/general/dotnet-run-file.md (2025-11-07, .NET 10 GA band) |
| **SPEC@10.0.3xx** | https://github.com/dotnet/sdk/blob/9ac5f515dfdd4b6af911e90e5b789a19bc728a87/documentation/general/dotnet-run-file.md |
| **SPEC@10.0.4xx** | https://github.com/dotnet/sdk/blob/0ad928980968ef0a5099686dd3e64d9b8cfaa5dd/documentation/general/dotnet-run-file.md |
| **SPEC@11.0.1xx** | https://github.com/dotnet/sdk/blob/ba3725b9e0127d4419c2c710f1a814b9a1f81db9/documentation/general/dotnet-run-file.md (.NET 11 GA band) |
| **SRC11-VPB** | https://github.com/dotnet/sdk/blob/2b2b3b81a48ded73d2583c6eb1682ab6a5eb8790/src/Cli/Microsoft.DotNet.FileBasedPrograms/VirtualProjectBuilder.cs (release/11.0.1xx) |
| **SRC11-DIR** | https://github.com/dotnet/sdk/blob/2b2b3b81a48ded73d2583c6eb1682ab6a5eb8790/src/Cli/Microsoft.DotNet.FileBasedPrograms/FileLevelDirectiveHelpers.cs |
| **SRC10-VPB** | https://github.com/dotnet/sdk/blob/d2550324a13231151faee7064eac4e38d93edf0d/src/Cli/dotnet/Commands/Run/VirtualProjectBuildingCommand.cs (release/10.0.1xx) |
| **ROSLYN-IDE** | https://github.com/dotnet/roslyn/blob/c1a70075c740c7a818d668564e56939aa9fe7901/docs/features/file-based-programs-vscode.md |
| **BLOG-ANNOUNCE** | https://devblogs.microsoft.com/dotnet/announcing-dotnet-run-app/ (Damian Edwards, 2025-05-28) |
| **LOCAL-TEST** | Runs on this machine (Windows 11) on 2026-11-13 with SDKs 10.0.204, 10.0.303, 10.0.404, 11.0.100, each pinned with `global.json` `rollForward: disable`. Direct observation, not documentation. |

> Note on the spec: `dotnet-run-file.md` calls itself "a proposal" (SPEC@main, line 3), and each release
> branch carries its own copy. The copies drift from the shipped code in places (see §8). Treat the
> branch copy as the best statement of intent for that SDK band, and the source or LOCAL-TEST as the
> tie-breaker.

---

## 1. What it is, versions, status

| Fact | Value | Source |
|---|---|---|
| Definition | Build, run, and publish an app from one C# file with no `.csproj`. The SDK makes a "virtual project" in memory from the file's `#:` directives. | LEARN-FBA intro; SPEC@main "Implicit project file" |
| Design rule | The file has an implicit project equal to what `dotnet new console` creates, so behavior "can change between SDK versions if the `dotnet new console` template changes". | SPEC@main "Implicit project file"; SRC11-VPB `GetDefaultProperties` remark ("Kept in sync with the default `dotnet new console` project file") L85-96 |
| First preview | .NET 10 Preview 4 (runtime preview released 2025-05-13; blog 2025-05-28) | BLOG-ANNOUNCE ("released as part of .NET 10 Preview 4"); https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json |
| GA | .NET 10.0.0 / SDK **10.0.100**, released **2025-11-11**. Not marked preview on Learn. | releases.json above; LEARN-RUN example note "File-based app support was added in .NET SDK 10.0.100" |
| .NET 10 support | LTS, latest SDK 10.0.404 (2026-11-10), EOL 2028-11-14 | https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json |
| .NET 11 | GA **2026-11-10**, SDK **11.0.100**, STS, EOL 2028-11-14. Adds `#:include` (also in 10.0.300+), quoted directive values and item metadata, Native AOT output reuse, `dotnet format app.cs`, `dotnet reference --file`. | same index; WN11 "CLI workflow and developer productivity" |
| Minimum SDK per feature | See the matrix in §2.9. | — |
| C# language side | `#:` and `#!` are "ignored directives" from the C# 14 cycle; "These are parsed regardless of language version." | LANG-SPEC "Detailed design" |

---

## 2. Directives

### 2.1 Language rules (Roslyn, all SDKs)

| Rule | Detail | Source |
|---|---|---|
| Ignored by the compiler | "The C# compiler ignores any preprocessor directive that starts with `#:` or `#!`." | LEARN-PP |
| Placement | Must come before the first token in the file, and before any `#if`. Errors: **CS9297** (`#:` after first token), **CS9299** (`#:` after `#if`). | LANG-SPEC "Restrictions"; LEARN-PPERR |
| Only in file-based mode | **CS9298** "`#:` directives can be only used in file-based apps (`-features:FileBasedProgram`)". **CS9314** "`#!` directives can be only used in scripts or file-based apps". So a `.cs` file with `#:` lines inside a normal `.csproj` fails to compile. | LEARN-PPERR; SPEC@main "Directives for project metadata" ("For project-based programs, `#:` directives are an error") |
| Shebang position | **CS9378** "'#!' must be the first characters on the first line of the file". "not even a BOM marker can be in front of it". | LEARN-PPERR; LANG-SPEC "Detailed design" |
| Inside strings/disabled regions | `#:` text inside a raw string or a disabled `#if` region is not a directive. | LANG-SPEC "Restrictions" examples |
| SDK stops scanning at `#if` | SDK 11.0.1xx stops collecting directives at the first `#if` (PR #56256). | SPEC@11.0.1xx (line "stopping the search when it sees the first "C# token" or `#if`"); SRC11-DIR L101-105 |
| Unknown kinds | Any other `#:kind` is an error ("Unrecognized directive 'x'"), reserved for future use. | SPEC@main; SRC11-DIR L378-385; LOCAL-TEST (10.0.204: `#:include` -> "Unrecognized directive 'include'") |

### 2.2 General directive syntax

| Rule | Detail | Source |
|---|---|---|
| Shape | kind + whitespace + name + separator + value. Separator is `@` for `#:package`/`#:sdk`, `=` for `#:property`. | SPEC@main "Directives for project metadata"; SRC10-VPB `ParseOptionalTwoParts` L1776-1817 |
| Value required? | Required for `#:property`; optional for `#:package`/`#:sdk`; disallowed for `#:project`/`#:ref`/`#:include`. | SPEC@main |
| **Space is not a separator** | `#:property LangVersion preview` (the form in BLOG-ANNOUNCE) is an error on every GA SDK: "The directive should contain a name without special characters and an optional value separated by '=' like '#:property Name=Value'." Use `#:property LangVersion=preview`. | BLOG-ANNOUNCE (old form); LOCAL-TEST (10.0.204, 10.0.303, 10.0.404, 11.0.100 all reject) |
| Quoting (.NET 11) | A value can be wrapped in `"..."`; it is lexed as a C# regular string literal (escapes decoded). Verbatim `@"..."` and raw `"""` are not supported. Quotes must wrap the whole value (`A=B"C"` is an error). | SPEC@11.0.1xx; WN11 "Quoted values and item metadata in file-based directives" |
| Quoting on .NET 10 | Rejected: "Directives currently cannot contain double quotes (")." | LOCAL-TEST (10.0.204, 10.0.303, 10.0.404) |
| Item metadata (.NET 11) | Trailing `Name=Value` tokens on `#:package`, `#:project`, `#:ref` become MSBuild item metadata, e.g. `#:package System.CommandLine@2.0.1 Aliases=CommandLine`. A `#:package` with `@version` cannot also set `Version=` metadata. | WN11; SPEC@11.0.1xx |
| Metadata on `#:include` | Only on `main` (PR #56392, 2026-10-30), so expected in a later band, not 11.0.100. **UNVERIFIED** for any shipped SDK. | SPEC@main vs SPEC@11.0.1xx diff |
| Legacy unquoted whitespace | Still accepted ("legacy mode") but flagged by analyzer **CA2267** (warning, .NET 11) with a code fix to add quotes. | SPEC@11.0.1xx; https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2267 |
| Windows paths | Bare value keeps backslashes literally (`#:project C:\src\lib`). Inside quotes, escape them (`"C:\\src\\my lib"`) or use `/`. | SPEC@11.0.1xx |
| MSBuild expressions | Values are passed to MSBuild verbatim, so `$(...)` and property functions work. | SPEC@main; LEARN-FBA "Conditional property values" |
| `:` in property names | .NET 11 no longer allows `:` in a `#:property` name (`#:property Foo:Bar=value` is rejected). | WN11 "Other CLI improvements" item 9 |
| Duplicates (10.0.1xx-10.0.3xx) | Any duplicate kind+name (case-insensitive) is an error: "Duplicate directives are not supported". | SPEC@10.0.1xx; https://github.com/dotnet/sdk/blob/d2550324a13231151faee7064eac4e38d93edf0d/src/Cli/dotnet/Commands/CliCommandStrings.resx (`DuplicateDirective`) |
| Duplicates (10.0.4xx, 11) | `#:sdk`/`#:property`/`#:package` duplicates with the same unevaluated value are ignored; different value is an error. `#:project`/`#:ref`/`#:include`/`#:exclude` duplicates are allowed. | SPEC@10.0.4xx; SPEC@11.0.1xx; WN11 "Include DLLs in file-based apps" (calls it new in .NET 11) |

### 2.3 `#:package`

| Fact | Detail | Source |
|---|---|---|
| Forms | `#:package Newtonsoft.Json` (CPM only), `#:package Serilog@3.1.1`, `#:package Spectre.Console@*` (latest), `#:package System.CommandLine@2.0.0-*` (floating prerelease), `#:package Microsoft.AspNetCore.OpenApi@10.*-*` | LEARN-FBA; SPEC@main; LEARN-CONVERT example |
| Translation | `<PackageReference Include="{name}" Version="{version}" />` (no `Version` attribute when omitted). | SPEC@main; SRC11-VPB |
| No version, no CPM | Fails with **NU1015** "The following PackageReference item(s) do not have a version specified". Omitting the version does NOT mean "latest". (Open request: https://github.com/dotnet/sdk/issues/49779.) | LEARN-FBA note; https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nu1015-packagereference-version; LOCAL-TEST (all four SDKs) |
| With CPM (`Directory.Packages.props` in a parent dir) | Omit the version; it resolves from `<PackageVersion>`. If you write `@version` under CPM you get **NU1008**. The SDK does not rewrite to `VersionOverride`. | SPEC@main ("useful when central package management (CPM) is used"); https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1008; SRC11-VPB (no CPM handling); LOCAL-TEST (11.0.100: no version -> OK, `@2.14.1` -> NU1008) |
| CLI management | `dotnet package add <id> --file app.cs` / `dotnet package remove <id> --file app.cs` edit the `#:package` lines (present in 10.0.1xx). | SPEC@10.0.4xx "Other commands"; https://github.com/dotnet/sdk/blob/d2550324a13231151faee7064eac4e38d93edf0d/src/Cli/dotnet/Commands/Package/Add/PackageAddCommandParser.cs (`FileOption`) |

### 2.4 `#:sdk`

| Fact | Detail | Source |
|---|---|---|
| Default | `Microsoft.NET.Sdk` | LEARN-FBA |
| Forms | `#:sdk Microsoft.NET.Sdk.Web`, `#:sdk Aspire.AppHost.Sdk@13.0.2` (version after `@`) | LEARN-FBA |
| Multiple | First becomes `<Project Sdk="Name/Version">`; later ones become `<Sdk Name="" Version="" />`. | SPEC@main; LEARN-CONVERT "Conversion process" step 5 |
| Side effect on default items | With only the default SDK, `EnableDefaultEmbeddedResourceItems` and `EnableDefaultNoneItems` are `false`. Any other SDK (e.g. Web) keeps defaults, so `*.json` and `.resx` files in the folder get picked up. | SPEC@main "Multiple files"; SRC11-VPB L652-661; LEARN-FBA "Default included items" |

### 2.5 `#:property`

| Fact | Detail | Source |
|---|---|---|
| Form | `#:property Name=Value` -> `<Name>Value</Name>`. Value may be empty; name must be a valid XML/MSBuild name. | SPEC@main; SRC10-VPB Property.Parse |
| Common uses | `#:property PublishAot=false`, `#:property PackAsTool=false`, `#:property TargetFramework=net10.0`, `#:property LangVersion=preview`, `#:property OutputPath=./output` | LEARN-FBA |
| Env-var defaults | `#:property LogLevel=$([MSBuild]::ValueOrDefault('$(LOG_LEVEL)', 'Information'))` | LEARN-FBA "Conditional property values" |
| Forbidden | `#:property RestoreUseStaticGraphEvaluation=true` -> error "Static graph restore is not supported for file-based apps." | SRC11-DIR L906; SRC10-VPB |
| Precedence | `#:property` values are written after `Sdk.props` (which imports `Directory.Build.props`), so a directive overrides `Directory.Build.props`. The SDK's own defaults (PublishAot etc.) are written before the SDK import, so `Directory.Build.props` can override those. | SRC11-VPB L664 comment "Write default properties before importing SDKs so they can be overridden by SDKs (and implicit build files...)", L772 "Write custom properties"; LOCAL-TEST (`Directory.Build.props` `AssemblyTitle=FromProps`; file without directive printed `FromProps`, file with `#:property AssemblyTitle=FromDirective` printed `FromDirective`) |
| Not overridable | Virtual project forces `RestoreUseStaticGraphEvaluation=false` and appends `FileBasedProgram` to `Features`. | SRC11-VPB L784-788 |

### 2.6 `#:project`

| Fact | Detail | Source |
|---|---|---|
| Form | `#:project ../SharedLibrary/SharedLibrary.csproj` or a directory: `#:project ../MyLibrary` | LEARN-FBA; SPEC@main |
| Directory resolution | A directory must contain exactly one project file, else error (same rule as `dotnet reference add`). | SPEC@main |
| Since | .NET 10 GA ("Project referencing: Support for referencing projects via the `#:project` directive"). | WN10 |
| CLI | `dotnet reference add/list/remove --file app.cs <path>` manages `#:project` lines. **.NET 11 and later.** | LEARN-FBA note; WN11 "'dotnet reference' support for file-based apps" |
| Paths during convert | Relative paths are rebased to the new project dir. A `$(Var)` at the start of the path may be expanded during conversion. | SPEC@main |
| stdin case | With `dotnet run -`, relative paths resolve against a temp dir; use `#:project $(MSBuildStartupDirectory)/relative/path`. | SPEC@11.0.1xx "Integration into the existing dotnet run command" |

### 2.7 `#:include` / `#:exclude` (multi-file)

| Fact | Detail | Source |
|---|---|---|
| Availability | "available in .NET 11 and .NET SDK 10.0.300 and later". Not recognized on 10.0.1xx/10.0.2xx. | LEARN-FBA note; LOCAL-TEST (10.0.204 "Unrecognized directive 'include'"; 10.0.303, 10.0.404, 11.0.100 run OK, no flag needed) |
| Forms | `#:include helpers.cs`, `#:include models/customer.cs`, `#:include shared/**/*.cs`, `#:include $(MSBuildProjectName).*.cs` | LEARN-FBA |
| Item mapping | `.cs`->`Compile`, `.resx`->`EmbeddedResource`, `.json`->`None`, `.razor`->`Content`, `.dll`->`Reference`. Override with property `FileBasedProgramsItemMapping`. | LEARN-FBA; SPEC@11.0.1xx; SRC11-VPB L647 |
| `.dll` mapping | Learn: "available in .NET 11 and later" and WN11 says "without a feature flag". But the `.dll=Reference` mapping is already in release/10.0.4xx source and spec, and absent in 10.0.3xx. Runtime behavior on 10.0.4xx **UNVERIFIED**. | LEARN-FBA; WN11; SPEC@10.0.4xx; https://github.com/dotnet/sdk/blob/release/10.0.4xx/src/Cli/Microsoft.DotNet.FileBasedPrograms/FileLevelDirectiveHelpers.cs |
| Included `.cs` files | Add types/members/namespaces; "they cannot add top-level statements". | LEARN-FBA |
| Globs disable cache | "When you use glob patterns, file-based app build caching is currently disabled." | LEARN-FBA |
| Relative paths | Resolved relative to the file containing the directive. | SPEC@main |
| `#:exclude` | Same as `#:include` but emits `Remove=`. In spec and source; **not listed on LEARN-FBA**. | SPEC@main; SRC11-DIR L383 |
| Directives in included files | Directives in any `Compile` item are honored (so a helper file can carry its own `#:package`). On 10.0.3xx this was behind `ExperimentalFileBasedProgramEnableTransitiveDirectives=true`; no flag in 10.0.4xx/11. | SPEC@10.0.3xx; SPEC@10.0.4xx; SPEC@11.0.1xx |
| Shebang on entry point | With `#:include`, the entry file should start with `#!`. Analyzer **CA2266** (warning) flags a missing `#!`. | SPEC@main "Shebang"; https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2266; ROSLYN-IDE "`#!` requirement" |

### 2.8 `#:ref` (file-to-file library reference)

| Fact | Detail | Source |
|---|---|---|
| What | `#:ref ../lib/lib.cs` compiles `lib.cs` as a separate virtual project (`lib.cs.csproj`) and adds a `ProjectReference`. Transitive. `internal` members of the referenced file are not visible. | SPEC@11.0.1xx |
| Library files | The referenced file defaults to `OutputType=Exe`; a library file must declare `#:property OutputType=Library`. | SPEC@11.0.1xx |
| Availability | 10.0.1xx/10.0.2xx: unrecognized. 10.0.3xx/10.0.4xx: needs `ExperimentalFileBasedProgramEnableRefDirective=true` (error otherwise). 11.0.100: no flag (PR #56247). | LOCAL-TEST (all four SDKs); SPEC@10.0.3xx/10.0.4xx; https://github.com/dotnet/sdk/commit/a7f1cdfa81d79265d75e48e6b8f57c1a25a28126 |
| Docs status | **Not listed on LEARN-FBA** "Supported directives", but named in WN11 (metadata on `#:ref`) and CA2266. | LEARN-FBA; WN11 |
| CA2266 | 11.0.100 warns CA2266 on an entry file using `#:ref` without `#!`. | LOCAL-TEST |
| Convert | Each `#:ref` becomes a sibling library project, recursively. | SPEC@11.0.1xx |

### 2.9 Feature/version matrix

| Feature | 10.0.1xx (GA) | 10.0.2xx | 10.0.3xx | 10.0.4xx | 11.0.1xx (GA) | Source |
|---|---|---|---|---|---|---|
| `#:package` `#:sdk` `#:property` `#:project` | yes | yes | yes | yes | yes | SPEC@10.0.1xx; LOCAL-TEST |
| `#:include` / `#:exclude` | no | no | yes | yes | yes | LEARN-FBA; LOCAL-TEST |
| `#:include x.dll` | no | no | no | in source (**UNVERIFIED** at runtime) | yes | §2.7 |
| `#:ref` | no | no | flag | flag | yes | LOCAL-TEST |
| Directives from included files | n/a | n/a | flag | yes | yes | SPEC branches |
| Same-value duplicates allowed | no | no | no | yes | yes | SPEC branches |
| Quoted values / item metadata | no | no | no | no | yes | LOCAL-TEST; WN11 |
| `dotnet reference --file` | no | ? | ? | ? | yes | LEARN-FBA ("available in .NET 11"); 10.0.4xx spec lists `dotnet reference` sub-commands, so 10.0.4xx support is **UNVERIFIED** |
| `dotnet project convert --delete-source` | no | no | yes | yes | yes | Parser/definition files on each branch (e.g. https://github.com/dotnet/sdk/blob/release/10.0.3xx/src/Cli/Microsoft.DotNet.Cli.Definitions/Commands/Project/ProjectConvertCommandDefinition.cs) |
| Native AOT output reuse, `dotnet format app.cs` | no | no | no | no | yes | WN11 |
| Virtual project name | `app.csproj` | ? | `app.cs.csproj` | `app.cs.csproj` | `app.cs.csproj` | LOCAL-TEST (warning paths: `ver.csproj` on 10.0.204, `ver.cs.csproj` on 10.0.303+); SRC11-VPB `GetVirtualProjectPath` |

---

## 3. CLI surface

| Command | Behavior | Source |
|---|---|---|
| `dotnet run app.cs` | Builds and runs. Falls back to project mode if a project file is in the current directory: then `app.cs` is passed to that project as an argument. | LEARN-FBA note; SPEC@main "Integration..."; LOCAL-TEST (dir with `P.csproj`: `dotnet run script.cs` printed `PROJECT args=[script.cs]`) |
| `dotnet run --file app.cs` | Forces file mode even next to a `.csproj`. | LEARN-FBA; LOCAL-TEST (printed `SCRIPT`) |
| `dotnet app.cs` | Shorthand for `dotnet run --file app.cs`, so it also works next to a `.csproj`. Not used if the name is a DLL path, built-in command, or a NuGet tool (`dotnet watch` runs the tool; use `dotnet ./watch`). | SPEC@main; LOCAL-TEST (printed `SCRIPT`) |
| Target path | Must have `.cs` extension OR start with `#!` (so extensionless shebang files work). | SPEC@main "Target path"; SRC11-VPB `IsValidEntryPointPath` L175-205; WN10 "support for extensionless files" |
| Arguments | `dotnet run app.cs -- arg1 arg2`. Arguments after the path go to the app except those `dotnet run` recognizes, unless after `--`. | LEARN-FBA "Pass arguments"; SPEC@main; LOCAL-TEST (`-- a b` -> `args=[a,b]`) |
| Working directory | Not changed: `cd /x && dotnet run /y/file.cs` runs in `/x`. | SPEC@main |
| stdin | `echo '...' \| dotnet run -`. No launch profiles or other files are found; cwd is still the working dir. `dotnet -` is not supported. | LEARN-FBA "Pipe code from stdin"; SPEC@main; LOCAL-TEST (`dotnet run -` OK; `dotnet -` -> "dotnet-- does not exist") |
| `dotnet build app.cs` | Builds to `<temp>/dotnet/runfile/<name>-<hash>/bin/<config>/` by default; `--output` or `#:property OutputPath=...` to change. | LEARN-FBA "Build applications" (see §4 for the real Unix path) |
| `dotnet restore app.cs` | Explicit restore; `--no-restore` on build/run skips implicit restore. | LEARN-FBA |
| `dotnet clean app.cs` | Cleans one app's artifacts. | LEARN-FBA |
| `dotnet clean file-based-apps` | Removes cached artifact folders unused for `--days` (default 30); also `--dry-run`. Present in 10.0.1xx (the 10.0.1xx spec text calls it `dotnet clean-file-based-app-artifacts`, which is stale). | LEARN-FBA; https://github.com/dotnet/sdk/blob/release/10.0.1xx/src/Cli/dotnet/Commands/Clean/FileBasedAppArtifacts/CleanFileBasedAppArtifactsCommandParser.cs |
| `dotnet publish app.cs` | Native AOT by default; output in `./artifacts/<AppName>/` next to the file. | LEARN-FBA "Publish applications"; SPEC@main "Implicit project file" (`PublishDir`/`PackageOutputPath` = `./artifacts/`, unless artifacts output layout is on) |
| `dotnet pack app.cs` | Packs as a .NET tool (`PackAsTool=true` default). | LEARN-FBA "Package as tool" |
| `dotnet project convert app.cs` | See §6. | LEARN-CONVERT |
| `dotnet user-secrets set/list ... --file app.cs` | Works with a stable ID hashed from the full path. Earliest SDK for `--file` **UNVERIFIED**. | LEARN-FBA "User secrets" |
| `dotnet format app.cs` | .NET 11. | WN11 |
| `dotnet run` flags of note | `--no-build`, `--no-cache` ("Skip up to date checks and always build"), `--no-restore`, `-lp/--launch-profile`, `--no-launch-profile`, `-e KEY=VALUE`. | LEARN-RUN "Options" |
| Concurrency | Parallel runs of the same app can collide on outputs. Build once, then `dotnet run app.cs --no-build`. | LEARN-FBA "Build caching" note |
| `dotnet watch app.cs` | Not documented on Learn. The SDK repo has `test/dotnet-watch.Tests/HotReload/FileBasedAppTests.cs`, so some support exists. **UNVERIFIED** as a supported scenario. | https://github.com/dotnet/sdk/tree/main/test/dotnet-watch.Tests/HotReload |

---

## 4. Implicit behavior

### 4.1 Implicit project properties

| Property | Value | Survives convert? | Source |
|---|---|---|---|
| `OutputType` | `Exe` | yes | SRC11-VPB L90; SRC10-VPB L78-86 |
| `TargetFramework` | `net{SDK major}` — net10.0 on a 10.0.x SDK, net11.0 on 11.0.100 | yes | SRC10-VPB L81; LOCAL-TEST (`Environment.Version` 10.0.15 vs 11.0.0; converted csproj had `net11.0`) |
| `ImplicitUsings` | `enable` (the normal SDK implicit-using set for the chosen SDK; the exact namespace list was not re-checked for this note) | yes | SRC11-VPB L92 |
| `Nullable` | `enable` | yes | SRC11-VPB L93 |
| `PublishAot` | `true` | yes | SRC11-VPB L94; LEARN-FBA |
| `PackAsTool` | `true` (10.0.1xx spec omits it, but 10.0.1xx source has it) | yes | SRC10-VPB L85; SPEC@10.0.3xx |
| `UserSecretsId` | `<name>-<sha256 of path>` | yes | SPEC@main; LOCAL-TEST (converted csproj) |
| `AssemblyName`, `RootNamespace` | file name without extension | virtual only | SPEC@main; SRC11-VPB |
| `EnableDefaultCompileItems` | `false` + explicit `Compile` for the entry file | virtual only | SPEC@main "Multiple files" |
| `DisableDefaultItemsInProjectFolder` | `true` | virtual only | SPEC@main |
| `FileBasedProgram` | `true` (for SDK targets to detect file-based apps) | virtual only | SPEC@main |
| `EntryPointFilePath` | visible to analyzers via `CompilerVisibleProperty` | virtual only | SPEC@main |
| Runtime data | `AppContext.GetData("EntryPointFilePath")` and `"EntryPointFileDirectoryPath"` (not set for Publish/Pack) | virtual only | SPEC@main; WN10 "Runtime path access" |
| `ArtifactsPath` / `PublishDir` / `PackageOutputPath` | temp dir / `./artifacts/` | virtual only | SPEC@main |

### 4.2 Output and cache locations

| Fact | Detail | Source |
|---|---|---|
| Windows | `%TEMP%\dotnet\runfile\<name>-<sha256>\bin\debug\<name>.dll` | SRC11-VPB `GetTempSubdirectory` L152-165 (uses `Path.GetTempPath()` on Windows); LEARN-TUT sample output; LOCAL-TEST (`...\Temp\dotnet\runfile\api-1e21...\bin\debug\api.dll`) |
| Unix | Uses `Environment.SpecialFolder.LocalApplicationData`, not `/tmp`: macOS `~/Library/Application Support/dotnet/runfile/...`. Linux path (expected `~/.local/share/dotnet/runfile/...`) **UNVERIFIED** on Learn. LEARN-FBA's "`<temp>/dotnet/runfile`" wording is only right on Windows. | SRC11-VPB L152-165; LEARN-TUT ("Library/Application Support/dotnet/runfile/..."); https://learn.microsoft.com/en-us/dotnet/maui/macios/system-special-folders (macOS `LocalApplicationData` = `$HOME/Library/Application Support`) |
| Hash | SHA-256 of the entry file's full path (casing normalized). Moving the file = new cache folder. | SRC11-VPB `GetArtifactsPath` L105-113 |
| Permissions | Folder created `0700`; run fails if that is not possible. | SPEC@main "Build outputs" |
| Auto cleanup | Every 2 days, removes outputs unused for 30 days. Disable: `DOTNET_CLI_DISABLE_FILE_BASED_APP_ARTIFACTS_AUTOMATIC_CLEANUP=true`. | SPEC@main "Build outputs" |
| Artifacts layout | If the repo enables the SDK artifacts layout (`UseArtifactsOutput`), outputs go to the repo's artifacts dir (spec: since the branch that added `FileBasedAppArtifactsPath`; Learn notes it under .NET 11). | SPEC@main; LEARN-FBA "Reuse Native AOT build outputs" section; SRC11-VPB L626-639 |

### 4.3 Up-to-date check / caching

| Fact | Detail | Source |
|---|---|---|
| Levels | Skip build entirely; run only `csc` (reusing last args) when only `.cs` content changed and directives did not; full MSBuild otherwise. | SPEC@main "Optimizations" |
| Cache key | Source content, directives, SDK version, implicit build file existence/content. | LEARN-FBA "Cache behavior" |
| Known holes | Files imported by an implicit build file are not tracked; moving files does not invalidate the cache. | SPEC@main; LEARN-FBA |
| Force rebuild | `--no-cache`, or `dotnet build app.cs`, or `#:property FileBasedProgramCanSkipMSBuild=false`. Debug the decision with `DOTNET_CLI_CONTEXT_VERBOSE=true`. | SPEC@main "Optimizations"; LEARN-RUN `--no-cache` |
| Globs | `#:include` globs disable caching. | LEARN-FBA |
| AOT reuse (.NET 11) | The Native AOT CLI path reuses unchanged outputs for `dotnet run --file app.cs`, `dotnet run app.cs`, `dotnet app.cs`. | WN11 |

### 4.4 Implicit build files

| Fact | Detail | Source |
|---|---|---|
| Inherited from the file's dir and parents | `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `nuget.config`, `global.json`. | LEARN-FBA "Implicit build files" |
| `global.json` | Selects the SDK, which also selects the default TFM and which directives exist (§2.9). | LEARN-FBA; LOCAL-TEST |
| Advice | Do not put file-based apps inside a `.csproj` folder tree ("project file cones"); give scripts their own folder with its own `Directory.Build.props` if needed. | LEARN-FBA "Folder layout recommendations"; SPEC@main |

### 4.5 Launch profiles

| Fact | Detail | Source |
|---|---|---|
| Flat file | `<AppName>.run.json` next to the source (`app.cs` -> `app.run.json`), same schema as `launchSettings.json`. Present in 10.0.1xx. | LEARN-FBA "Launch profiles"; https://github.com/dotnet/sdk/blob/release/10.0.1xx/src/Cli/dotnet/Commands/Run/RunCommand.cs (`GetFlatLaunchSettingsPath`, `RunCommandWarningRunJsonNotUsed`) |
| Precedence | `Properties/launchSettings.json` wins over `app.run.json`, with a warning. Profile choice: `--launch-profile`, then `DOTNET_LAUNCH_PROFILE`, then first profile. | LEARN-FBA |
| .NET 11 change | "Using launch settings from..." now goes to stderr. | WN11 |

---

## 5. Limitations and gotchas

| Gotcha | Detail | Source |
|---|---|---|
| Native AOT is on by default | `PublishAot=true` means build reports AOT/trim warnings, and publish needs the AOT toolchain. Reflection-heavy packages: add `#:property PublishAot=false`. | SPEC@main "Other commands" ("building reports AOT warnings"); LEARN-FBA; WN10 |
| AOT pulls ILCompiler on restore | Even plain `dotnet run` restored `Microsoft.DotNet.ILCompiler`; older SDK bands surfaced **NU1903** vulnerability warnings for it (10.0.204 -> ILCompiler 10.0.8, 10.0.303 -> 10.0.11). | LOCAL-TEST only |
| Single file on 10.0.1xx/2xx | Only the entry file compiles. Other `.cs` files in the folder are ignored (no implicit `**/*.cs`). LEARN-TUT still says "you must write them in one source file", which is stale for 10.0.300+/11. | SPEC@10.0.1xx "Multiple C# files" caution; LEARN-TUT; LEARN-FBA `#:include` |
| Entry point | Top-level statements or a classic `Main`. Type declarations go after top-level statements. | LEARN-TUT |
| `.csproj` in same folder | `dotnet run app.cs` runs the project instead. Use `--file` or `dotnet app.cs`, but better, move the script out. IDE tooling may also misclassify it. | §3; SPEC@main |
| `#:` in project files | If a project's glob picks up a file-based app, it fails with CS9298. Exclude it (`<Compile Remove="scripts/**" />`) or move it. | LEARN-PPERR; ROSLYN-IDE "LSP handling" |
| Shebang line | Recommended: `#!/usr/bin/env -S dotnet --` (the `--` stops `dotnet` eating `--help` etc.). Fallback where `env -S` is missing: `#!/usr/bin/env dotnet`. Avoid `#!/usr/bin/env dotnet run` (may be passed as one argument). | LEARN-FBA "Shell execution"; LEARN-TUT; SPEC@main "Shebang support" |
| Shebang file format | LF line endings, no BOM; `chmod +x`. | LEARN-FBA note; LANG-SPEC |
| **This repo** | Root `.gitattributes` sets `*.cs text eol=crlf`. A shebang `.cs` file committed here checks out CRLF and breaks `./app.cs` on Linux. A skill that ships shebang samples needs a `.gitattributes` override (e.g. `*.cs eol=lf` for that folder) or extensionless/`.sh` wrappers. | `c:\Users\rheone\code\llm-dev-container\.gitattributes`; LEARN-FBA LF note |
| Windows | No shebang execution; the `#!` line is allowed and ignored. Run with `dotnet app.cs`. | LEARN-TUT "Unix shebang (`#!`) support" note |
| Old syntax in memory | Blog-era `#:property Name Value` (space) and `#!/usr/bin/dotnet run` are wrong now. | §2.2; BLOG-ANNOUNCE; SPEC@10.0.1xx shebang sample |
| Version-less `#:package` | Error unless CPM. Use `@*` for latest. | §2.3 |
| Stale caches | Edits to files imported by `Directory.Build.props` may not trigger a rebuild; use `--no-cache`. | §4.3 |
| Duplicate directives across files | Error on 10.0.1xx-3xx; same-value duplicates OK from 10.0.4xx. | §2.2 |
| Static graph restore | `RestoreUseStaticGraphEvaluation=true` (including from `Directory.Build.props`) is not supported. The virtual project forces it `false`; a directive setting it `true` errors. | SRC11-VPB L786; SRC11-DIR L906 |
| User secrets | ID is a hash of the full path, so moving or renaming the file loses access to stored secrets. | LEARN-FBA ("hash of the full file path") — the "moving loses secrets" consequence is inferred |
| Analyzers | CA2266 (missing `#!` with `#:include`/`#:ref`, warning, .NET 10 rule docs) and CA2267 (unquoted whitespace, .NET 11). Directory-level analyzer settings apply through `Directory.Build.props`/`.editorconfig`. | CA2266/CA2267 Learn pages; LEARN-FBA "Implicit build files" |
| VS Code | C# extension ("rich miscellaneous files"). A file is a file-based app if it has `#!`, or `#:` plus top-level statements. Opt-outs: `dotnet.projects.enableFileBasedPrograms`, `dotnet.projects.enableFileBasedProgramsWhenAmbiguous`. Workspace auto-discovery (`dotnet.fileBasedApps.enableAutomaticDiscovery`) only finds files that start with `#!`, skips folders that contain a `.csproj`, `bin`/`obj`/`artifacts`, and hidden folders. Minimum C# extension version for current behavior **UNVERIFIED** (blog said pre-release >= 2.79.8 in May 2025). | ROSLYN-IDE; BLOG-ANNOUNCE |
| Nested-glob risk | `#:include **.cs` in a big directory walks everything; a file-based app in a drive root hits MSB5029. | SPEC@main "Nested files errors" |

---

## 6. Conversion to a project

| Fact | Detail | Source |
|---|---|---|
| Command | `dotnet project convert <FILE> [--dry-run] [--force] [--interactive] [-o\|--output <DIR>]` (+ `--delete-source` from 10.0.300) | LEARN-CONVERT synopsis; §2.9 |
| Copy, not move | The original `.cs` is left in place; a copy goes into a new folder. LEARN-CONVERT says "Moves"; that is wrong. Source uses `File.Copy`; LEARN-FBA says "left untouched". `--delete-source` (or yes at the interactive prompt) deletes the originals. | https://github.com/dotnet/sdk/blob/d2550324a13231151faee7064eac4e38d93edf0d/src/Cli/dotnet/Commands/Project/Convert/ProjectConvertCommand.cs L121; https://github.com/dotnet/sdk/blob/2b2b3b81a48ded73d2583c6eb1682ab6a5eb8790/src/Cli/dotnet/Commands/Project/Convert/ProjectConvertCommand.cs L86-107; LEARN-FBA "Convert to project"; LOCAL-TEST (11.0.100: `tool.cs` remained) |
| Output dir | Default: folder named after the file, **next to the file** (`Path.ChangeExtension(file, null)`), not the cwd as LEARN-CONVERT says. Fails if the folder exists. | ProjectConvertCommand.cs (10.0.1xx) `DetermineOutputDirectory` L202-215; LEARN-FBA |
| File name | Keeps the input name (`app.cs` -> `app/app.cs`). BLOG-ANNOUNCE said `Program.cs`; that is outdated. | LEARN-CONVERT; LOCAL-TEST |
| What it writes | Directives removed from the copy (including `#!`); csproj with OutputType, TFM, ImplicitUsings, Nullable, PublishAot, PackAsTool, UserSecretsId, plus directive-derived items. A `#:property` that overrides a default replaces that default. | LOCAL-TEST output below; SPEC@main "Grow up" |
| Included files | `Compile`/`None`/`Content`/`EmbeddedResource` items are copied unless they have `ExcludeFromFileBasedAppConversion=true`. `#:ref` files become sibling library projects. | SPEC@main "Multiple files", `#:ref` |
| `--force` | Converts even when directives are malformed. | LEARN-CONVERT |
| When to convert | Spec: when "build customizations in a project file are needed". Practical triggers from the docs: needing multiple files on 10.0.1xx/2xx, test projects, solution membership, or anything the directives cannot express. Convert should not change program behavior. | SPEC@main "Grow up"; LEARN-TUT |
| After convert | `dotnet run path.cs` no longer works for it; use `dotnet run --project`. | SPEC@main "Enhancing integration..." |

Converted output observed on 11.0.100 (LOCAL-TEST) from a file with `#!/usr/bin/env -S dotnet --`,
`#:package Humanizer@2.14.1`, `#:property PublishAot=false`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackAsTool>true</PackAsTool>
    <UserSecretsId>tool-6095e408e48b11b23e8ec443e44afb22b0dde5490eaf4520636ba0a895bfbbb1</UserSecretsId>
    <PublishAot>false</PublishAot>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Humanizer" Version="2.14.1" />
  </ItemGroup>

</Project>
```

---

## 7. Examples

All examples below ran on this machine (LOCAL-TEST) unless marked otherwise. Syntax comes from
LEARN-FBA, LEARN-CONVERT, WN11.

Console, no directives (all SDKs):

```csharp
Console.WriteLine($"{Environment.Version} args=[{string.Join(",", args)}]");
```

`dotnet run ver.cs -- a b` -> `10.0.15 args=[a,b]` on 10.0.x SDKs, `11.0.0 args=[a,b]` on 11.0.100.

Package reference (all SDKs):

```csharp
#:package Humanizer@2.14.1
using Humanizer;
Console.WriteLine("file_based_app".Humanize());   // "file based app"
```

Web API (`dotnet build api.cs` succeeded on 11.0.100; not run as a server here):

```csharp
#:sdk Microsoft.NET.Sdk.Web
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "Hello from a file-based web app");
app.Run();
```

Shebang tool (LEARN-FBA "Shell execution"; Unix execution **not** tested here, Windows only):

```csharp
#!/usr/bin/env -S dotnet --
#:package Spectre.Console@*
using Spectre.Console;
AnsiConsole.MarkupLine("[green]Hello, World![/]");
```

```bash
chmod +x hello.cs   # LF endings, no BOM
./hello.cs --help   # --help goes to the app, not dotnet
```

Multi-file (10.0.300+ / 11):

```csharp
#!/usr/bin/env dotnet
#:include helpers.cs
Console.WriteLine(Helpers.Msg());
```

```csharp
// helpers.cs — no top-level statements allowed here
static class Helpers { public static string Msg() => "from helpers"; }
```

`#:ref` library (11.0.100 without flag; 10.0.3xx/4xx need
`#:property ExperimentalFileBasedProgramEnableRefDirective=true`, **UNVERIFIED** whether setting it as a
`#:property` in the entry file is enough):

```csharp
// lib.cs
#:property OutputType=Library
public static class Lib { public static string Hi() => "from lib"; }
```

```csharp
#!/usr/bin/env dotnet
#:ref lib.cs
Console.WriteLine(Lib.Hi());
```

.NET 11 quoted value and metadata (WN11 sample; quoted property ran on 11.0.100):

```csharp
#:property AssemblyTitle="My Title"
#:package System.CommandLine@2.0.1 Aliases=CommandLine
extern alias CommandLine;
```

Pin TFM when the machine's newest SDK is newer than you want (LEARN-FBA `#:property TargetFramework=net10.0`):

```csharp
#:property TargetFramework=net10.0
```

stdin (all GA SDKs):

```bash
echo 'Console.WriteLine("hello from stdin!");' | dotnet run -
```

---

## 8. Conflicts and stale statements in primary sources

| Statement | Where | Reality | Evidence |
|---|---|---|---|
| Convert "moves" code / "in the current directory" | LEARN-CONVERT | Copies; folder goes next to the source file | §6 |
| Convert moves code into `Program.cs`; `#:property LangVersion preview`; `#!/usr/bin/dotnet run` | BLOG-ANNOUNCE (preview 4) | File name kept; `=` required; use `#!/usr/bin/env -S dotnet --` | §2.2, §6 |
| "you must write them in one source file" | LEARN-TUT | False for 10.0.300+ (`#:include`) | §2.7 |
| Build output under `<temp>/dotnet/runfile` | LEARN-FBA | Windows only; Unix uses LocalApplicationData | §4.2 |
| `dotnet clean-file-based-app-artifacts` | SPEC@10.0.1xx | Shipped as `dotnet clean file-based-apps` | §3 |
| `.dll` include and same-value duplicates "new in .NET 11" | LEARN-FBA, WN11 | Already in release/10.0.4xx spec/source | §2.2, §2.7 |
| `dotnet run -e` described as new | WN11 ("A new `dotnet run -e` option") | LEARN-RUN says `-e` came in SDK 9.0.200 | LEARN-RUN "Environment variables" |
| Supported directive list | LEARN-FBA lists 5 kinds | Source also has `#:ref` and `#:exclude` | SRC11-DIR L378-385 |

---

## 9. Open questions / UNVERIFIED

Items 1–4, 6 and 9 were resolved on Linux the same day, and item 7 in part; see §9a. Still open:

5. `#:include` item metadata (main only) — which SDK band ships it.
7. The *earliest* SDK for `dotnet user-secrets --file` (works on 10.0.404 and 11.0.100; older bands not tested).
8. Minimum VS Code C# extension / C# Dev Kit versions for current file-based behavior, and the current default of `enableAutomaticDiscovery`.

## 9a. Linux verification (LOCAL-TEST-LINUX)

Runs on 2026-11-13 in Docker images `mcr.microsoft.com/dotnet/sdk:10.0` (SDK **10.0.404**) and `mcr.microsoft.com/dotnet/sdk:11.0` (SDK **11.0.100**), Debian, kernel 6.18 (WSL2), user `root`, `HOME=/root`, `XDG_DATA_HOME` unset. Probe scripts kept in the session scratchpad. Results identical on both SDKs unless noted.

| # | Question | Result |
|---|---|---|
| 1 | Linux cache path | `~/.local/share/dotnet/runfile/<name>-<sha256>/bin/debug/` (`/root/.local/share/dotnet/runfile/hello-5291…/bin/debug/`). |
| 2 | Shebang execution | `#!/usr/bin/env -S dotnet --` + `chmod +x`: `./sb.cs --help x` → app received `[--help,x]`. `#!/usr/bin/env dotnet` also works. Extensionless copy works. |
| 2 | BOM | `./bom.cs` fails: `#!/usr/bin/env: not found` (shell runs it as sh). `dotnet bom.cs` still works. |
| 2 | CRLF | With `env -S dotnet --` a CRLF file still ran. CRLF with `#!/usr/bin/env dotnet`: see probe2 below. LF remains the documented requirement. |
| 3 | `#:include out/lib.dll` | Works on 10.0.404 and 11.0.100. Raises CA2266 (no `#!`). |
| 3 | `dotnet reference add <csproj> --file app.cs` | Works on 10.0.404: writes `#:project Lib/Lib.csproj` at the top; app runs. |
| 4 | `#:ref` flag | On 10.0.404, `#:property ExperimentalFileBasedProgramEnableRefDirective=true` in the entry file enables `#:ref`; so does the env var of the same name. Without it: "This is an experimental feature, set MSBuild property 'ExperimentalFileBasedProgramEnableRefDirective' to 'true' to enable it." 11.0.100: no flag needed. |
| 6 | `dotnet watch w.cs` | Works on both. 10.0.404: "Project does not support Hot Reload: 'PublishAot' property is 'True'. Application will be restarted when updated. Set 'StartupHookSupport' project property to 'True' to enable Hot Reload." 11.0.100: Hot Reload enabled. |
| 7 | `dotnet user-secrets set/list --file` | Works on 10.0.404 and 11.0.100. |
| 9 | Web SDK default items | With `#:sdk Microsoft.NET.Sdk.Web`, `appsettings.json` next to the file is loaded (`Configuration["Greeting"]` = "from appsettings"). |
| — | `.csproj` collision | `dotnet run tool.cs` next to `P.csproj` prints: "Warning: 'tool.cs' appears to be a file-based app but was passed as an argument to the project '…/P.csproj'. To run it as a file-based app, use 'dotnet run --file tool.cs'. …" then runs the project. `--file` and `dotnet tool.cs` run the file. |
| — | Web API | `#:sdk Microsoft.NET.Sdk.Web` app served `GET /` and `GET /hi/{n}` with 200. |

---

## 10. Sources consulted

- Microsoft Learn: LEARN-FBA, LEARN-RUN, LEARN-CONVERT, LEARN-TUT, LEARN-PP, LEARN-PPERR, LANG-SPEC, WN10, WN11; https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2266 ; https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2267 ; NuGet NU1008/NU1015 pages linked above. Markdown sources read from https://github.com/dotnet/docs (main) on 2026-11-13.
- dotnet/sdk: spec copies at the five pinned SHAs above; source at release/10.0.1xx `d2550324a13231151faee7064eac4e38d93edf0d` and release/11.0.1xx `2b2b3b81a48ded73d2583c6eb1682ab6a5eb8790`; commits `a7f1cdf` (#56247, remove `#:ref` flag), `5d62f8f` (#56256, stop at `#if`), `6966313` (#56392, `#:include` metadata); issue https://github.com/dotnet/sdk/issues/49779.
- dotnet/roslyn: ROSLYN-IDE.
- dotnet/core release metadata: https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json and the 10.0/11.0 `releases.json`.
- .NET Blog: BLOG-ANNOUNCE (historical; superseded where noted).
- No third-party blogs were used.
