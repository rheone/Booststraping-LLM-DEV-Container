# Project Setup and Packaging

## Base project shape

An analyzer project is an ordinary `netstandard2.0`-targeting class library — `netstandard2.0`
remains the safest target because it's the lowest common denominator every current Visual
Studio/Roslyn host version can load, regardless of which .NET version the *consuming* project
targets:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="5.6.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

- **`PrivateAssets="all"`** on every Roslyn SDK package reference — an analyzer's own dependencies
  must never flow transitively into a consuming project's compile-time references; without this, a
  project merely referencing your analyzer package would also get `Microsoft.CodeAnalysis.CSharp`
  as a visible dependency.
- **`IncludeBuildOutput = false`** — an analyzer assembly ships inside the package's `analyzers/`
  folder, not as a normal `lib/` compile reference; this flag stops the SDK from also placing it
  under `lib/`.
- **`Microsoft.CodeAnalysis.Analyzers`** (current verified stable release: 5.6.0) is
  Microsoft's own analyzer that lints *your* analyzer/code-fix implementation itself (catches
  missing `[Shared]`, non-deterministic `SupportedDiagnostics`, and similar authoring mistakes) —
  add it to every analyzer project, not just to projects that consume analyzers.

## Referencing the analyzer from another project directly (no NuGet package)

For an analyzer scoped to one solution, reference it as an `<Analyzer>` item instead of packaging
it:

```xml
<ItemGroup>
  <Analyzer Include="..\MyAnalyzers\bin\$(Configuration)\netstandard2.0\MyAnalyzers.dll" />
</ItemGroup>
```

A plain `<ProjectReference>` to an analyzer project does **not** make the analyzer run against the
referencing project — an analyzer needs the `<Analyzer>` item (or the NuGet packaging shape below,
which wires this up automatically) to actually participate in that project's compilation.

## Packaging as a NuGet package

The package's `analyzers/dotnet/cs/` folder is what the SDK-style NuGet consumption model scans for
analyzer assemblies to load:

```xml
<ItemGroup>
  <None Include="$(OutputPath)\$(AssemblyName).dll"
        Pack="true"
        PackagePath="analyzers/dotnet/cs"
        Visible="false" />
</ItemGroup>
```

Anything under `analyzers/dotnet/cs` in the resulting `.nupkg` is picked up automatically by any
project that adds a `PackageReference` to it — no `<Analyzer>` item needed on the consumer's side.
Ship the code-fix provider assembly in the same package/folder as its paired analyzer; splitting
them across separate packages works but adds a coordination burden (a consumer could end up with
the analyzer but not the fix, or a mismatched version of each) with no offsetting benefit for the
common case of one analyzer package per suite of related rules.

## Consumer-side `PackageReference`

From the consuming project's side, an analyzer package reference also needs
`PrivateAssets="all"` (or `IncludeAssets="analyzers;buildtransitive"` for finer control) so the
analyzer doesn't itself become a transitive compile-time dependency of anything referencing that
consuming project:

```xml
<PackageReference Include="MyAnalyzers" Version="1.0.0" PrivateAssets="all" />
```
