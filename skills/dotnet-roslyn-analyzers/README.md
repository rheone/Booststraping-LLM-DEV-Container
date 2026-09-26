# Roslyn Analyzers

Guidance on authoring `DiagnosticAnalyzer`s and paired `CodeFixProvider`s for C# — the routing
table (by task, not Roslyn version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Roslyn version

| File | Covers |
| --- | --- |
| `diagnostic-descriptors.md` | `DiagnosticDescriptor` fields, `SupportedDiagnostics`, id/category conventions, help links |
| `registering-analyzer-actions.md` | `RegisterSyntaxNodeAction`, `RegisterSymbolAction`, `RegisterOperationAction`, `RegisterCompilationStartAction`, concurrent execution |
| `syntax-symbol-operation-analysis.md` | Syntax-tree vs. symbol vs. operation analysis; when each fits |
| `code-fix-providers.md` | `CodeFixProvider`, `RegisterCodeFixesAsync`, `FixableDiagnosticIds`, `FixAllProvider` |
| `project-setup-and-packaging.md` | Analyzer `.csproj` shape, `PrivateAssets="all"`, NuGet packaging, `analyzers` folder convention |
| `testing.md` | `Microsoft.CodeAnalysis.Testing`, `CSharpAnalyzerTest`, `CSharpCodeFixTest` |

## Scope

Roslyn diagnostic analyzers and their paired code fixes only — reporting `Diagnostic`s against a
compilation and offering automated fixes for them. Out of scope: compile-time source generation
(a different extensibility mechanism with its own registration model and packaging shape), general
C#/BCL guidance beyond the analyzer-authoring surface, and IDE extension UI beyond the standard
`CodeFixProvider` registration.

Each reference file notes a version-introduced fact inline (e.g. which Roslyn SDK version added a
given API); version is not the file-splitting axis for this skill (see [SKILL.md](SKILL.md) for
why).
