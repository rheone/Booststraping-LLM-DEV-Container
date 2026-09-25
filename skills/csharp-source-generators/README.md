# C# Source Generators

Reference for authoring Roslyn source generators — `ISourceGenerator` and `IIncrementalGenerator`,
the pipeline API, diagnostics, incremental caching, and project setup/packaging. The routing table
is in [SKILL.md](SKILL.md).

This skill is organized by **.NET SDK / Roslyn package version**, not by C# language version — see
the version axis note at the top of [SKILL.md](SKILL.md) for why.

```text
references/                                        version-gated core API, oldest to newest
  pre-net5-external-codegen.md                        below .NET 5 SDK — T4 templates or external MSBuild-driven codegen
  net5-isourcegenerator.md                             .NET 5 SDK (Roslyn 3.8) — ISourceGenerator, GeneratorExecutionContext, ISyntaxReceiver
  net6-iincrementalgenerator.md                        .NET 6 SDK (Roslyn 4.0) — IIncrementalGenerator, the pipeline API
  net7-forattributewithmetadataname.md                 .NET 7 SDK (Roslyn 4.3) — ForAttributeWithMetadataName, WithTrackingName
  net8-interceptors-preview.md                         .NET 8 SDK (Roslyn 4.8) — interceptors, preview, opt-in
  net9-interceptors-stable.md                          .NET 9 SDK (Roslyn 4.12) — interceptors stable, GetInterceptableLocation
  net10-embedded-attribute-definitions.md              .NET 10 SDK (Roslyn 4.14) — AddEmbeddedAttributeDefinition

specialized/                                       cross-cutting patterns, applicable across versions
  testing-a-source-generator.md                        CSharpGeneratorDriver, snapshot testing, asserting incremental caching
  incremental-pipeline-and-equatable-models.md         cache correctness, ISymbol/SyntaxNode leakage, EquatableArray<T>
  diagnostics-from-a-generator.md                      DiagnosticDescriptor, severity, mapping diagnostics to user source
  generator-project-setup-and-packaging.md             IsRoslynComponent, EnforceExtendedAnalyzerRules, multi-targeting, NuGet packaging
  generics-in-generated-code.md                        reading/emitting generic type parameters and constraints
```

## Version coverage

| .NET SDK | Roslyn package | GA | Source-generator-relevant additions |
| --- | --- | --- | --- |
| Below .NET 5 | — | — | No generator API; T4 templates or an external, MSBuild-driven codegen tool |
| .NET 5 | 3.8 | November 2020 | `ISourceGenerator`, `GeneratorExecutionContext`, `ISyntaxReceiver`/`ISyntaxContextReceiver`, `AddSource`, `OutputItemType="Analyzer"` project setup |
| .NET 6 | 4.0 | November 2021 | `IIncrementalGenerator`, `IncrementalGeneratorInitializationContext`, the pipeline API (`CreateSyntaxProvider`, `Select`/`Where`/`Collect`/`Combine`), `RegisterSourceOutput`/`RegisterPostInitializationOutput` |
| .NET 7 | 4.3 | November 2022 | `ForAttributeWithMetadataName`, `WithTrackingName` for incrementality testing |
| .NET 8 | 4.8 | November 2023 | Interceptors (preview only; `InterceptorsPreviewNamespaces` opt-in; raw file/line/column `[InterceptsLocation]`) |
| .NET 9 | 4.12 | November 2024 | Interceptors stabilize (no opt-in needed); `GetInterceptableLocation`/`InterceptableLocation` replace the raw position triple |
| .NET 10 | 4.14 | November 2025 | `AddEmbeddedAttributeDefinition` — fixes marker-attribute `CS0436` duplication across `InternalsVisibleTo`-linked projects |
| .NET 11 | — | RC as of Sept 2026; GA expected Nov 2026 | No generator-API-specific change found (verified via search) — C# 15's language features (native unions, collection-expression arguments) don't touch the generator API surface |

<!-- Keep this table's rows in sync with SKILL.md's routing table -- same tiers, same order.
     On a maintenance pass, re-check .NET 11's RC/GA status before appending a new row. -->
