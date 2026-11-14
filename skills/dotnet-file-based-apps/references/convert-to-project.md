# Converting to a project

`dotnet project convert app.cs` turns a file-based app into a normal project that has a `.csproj`. The program behaves the same after conversion. Only how you build and run it changes.

## When to convert

Convert when at least one of these is true:

- The code needs a test project (unit or integration tests that reference it).
- It needs a second entry point, or it becomes a library that other code references.
- It needs project features that directives cannot express: designers, multi-targeting, solution membership, or custom MSBuild targets.
- A team maintains it as an application rather than as a script.
- The SDK band in use lacks a directive you need (for example `#:include` on 10.0.1xx/2xx), and pinning a newer SDK is not an option.

These are not reasons to convert by themselves:

- The file is long, or you want to split it. On SDK 10.0.300+ you split it with `#:include` and stay file-based ([multi-file.md](multi-file.md)).
- It uses packages, a Web SDK or properties. Directives cover all of these.

When a trigger applies, tell the user and say which one. Run the conversion when the user agrees, or when the task already asks for a project.

## The command

```bash
dotnet project convert app.cs                  # creates ./app/app.csproj + ./app/app.cs
dotnet project convert app.cs -o src/App       # choose the output folder
dotnet project convert app.cs --dry-run        # show what it would write
dotnet project convert app.cs --delete-source  # SDK 10.0.300+: delete the original afterwards
```

What it does:

- **It copies the file and leaves the original in place**, unless you pass `--delete-source` (SDK 10.0.300+) or answer yes at the interactive prompt. After conversion, delete or archive the original yourself so two copies don't drift apart.
- **The output folder goes next to the source file**, named after it (`tools/app.cs` → `tools/app/`), whatever the current directory is. The command fails if that folder already exists.
- **The file keeps its name** (`app/app.cs`, not `Program.cs`).
- It removes every `#:` and `#!` line from the copy.
- It writes the virtual project's defaults into the `.csproj`: `OutputType`, `TargetFramework`, `ImplicitUsings`, `Nullable`, `PublishAot`, `PackAsTool` and `UserSecretsId`. A `#:property` that sets one of these replaces the default. Each `#:package`, `#:project` and `#:sdk` becomes the matching project element.
- It rebases relative `#:project` paths to the new folder.
- It copies files brought in by `#:include`. Each `#:ref` file becomes a sibling library project.
- `--force` converts even when a directive is malformed.

Example output on SDK 11.0.100 for a file with `#:package Humanizer@2.14.1` and `#:property PublishAot=false`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackAsTool>true</PackAsTool>
    <UserSecretsId>tool-6095e4…</UserSecretsId>
    <PublishAot>false</PublishAot>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Humanizer" Version="2.14.1" />
  </ItemGroup>
</Project>
```

## After converting

- Review the defaults it copied and remove any the project doesn't need. `PackAsTool=true` makes `dotnet pack` produce a .NET tool. `PublishAot=true` keeps the Native AOT publish path and its warnings.
- Build and run it with `dotnet run --project app/`, not `dotnet run app/app.cs`.
- If the project lives under a repo that already has `Directory.Build.props` or `Directory.Packages.props`, the same files still apply. Under CPM, move each `Version` into `Directory.Packages.props`.
- `UserSecretsId` keeps the hash of the old file path, so secrets set with `--file` carry over.
