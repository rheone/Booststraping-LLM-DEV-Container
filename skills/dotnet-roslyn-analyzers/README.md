# Roslyn Analyzers

Guidance on writing `DiagnosticAnalyzer`s that report compile-time and IDE diagnostics against C#
code, and the `CodeFixProvider`s that pair with them to offer automated fixes.

## When to reach for it

- Writing a new `DiagnosticAnalyzer` and deciding whether to register a syntax, symbol, or
  operation action.
- Pairing a diagnostic with an automated `CodeFixProvider`.
- Setting up an analyzer project and packaging it as a NuGet package or `<Analyzer>` reference.
- Writing a `Microsoft.CodeAnalysis.Testing`-based test for an analyzer or a code fix.

## Using it

This skill is model-invoked: it fires automatically when you're authoring, registering, testing,
or packaging a Roslyn diagnostic analyzer or code fix. You can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| DiagnosticDescriptor fields, SupportedDiagnostics, id/category conventions, help links | [references/diagnostic-descriptors.md](references/diagnostic-descriptors.md) |
| RegisterSyntaxNodeAction, RegisterSymbolAction, RegisterOperationAction, RegisterCompilationStartAction | [references/registering-analyzer-actions.md](references/registering-analyzer-actions.md) |
| Choosing syntax-tree vs. symbol vs. operation analysis for a given check | [references/syntax-symbol-operation-analysis.md](references/syntax-symbol-operation-analysis.md) |
| CodeFixProvider, RegisterCodeFixesAsync, FixableDiagnosticIds, FixAllProvider | [references/code-fix-providers.md](references/code-fix-providers.md) |
| Analyzer .csproj shape, PrivateAssets="all", NuGet packaging | [references/project-setup-and-packaging.md](references/project-setup-and-packaging.md) |
| Testing an analyzer or code fix with Microsoft.CodeAnalysis.Testing | [references/testing.md](references/testing.md) |

## Example prompts

- "Write an analyzer that flags direct `Console.WriteLine` calls."
- "Add a code fix that replaces the flagged call with an injected `ILogger`."
- "Set up this analyzer project so it packages as a NuGet analyzer reference."
