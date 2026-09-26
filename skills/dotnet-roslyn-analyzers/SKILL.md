---
name: dotnet-roslyn-analyzers
description: Guidance on authoring and consuming Roslyn diagnostic analyzers for C# — DiagnosticAnalyzer, DiagnosticDescriptor/SupportedDiagnostics, registering RegisterSyntaxNodeAction/RegisterSymbolAction/RegisterOperationAction/RegisterCompilationStartAction callbacks, pairing an analyzer with a CodeFixProvider (RegisterCodeFixesAsync, FixableDiagnosticIds, FixAllProvider), analyzer project setup and NuGet packaging (verified current Microsoft.CodeAnalysis.Analyzers 5.6.0, analyzer/codefix packages referenced with PrivateAssets="all"), and testing analyzers and code fixes with the Microsoft.CodeAnalysis.Testing packages (verified current Microsoft.CodeAnalysis.CSharp.Analyzer.Testing 1.1.3). Use when writing a new DiagnosticAnalyzer, choosing which syntax/symbol/operation action to register, pairing a diagnostic with an automated fix, packaging an analyzer as a NuGet package or analyzer reference, or writing a Microsoft.CodeAnalysis.Testing-based test for an analyzer or code fix.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Roslyn Analyzers

Guidance on writing `DiagnosticAnalyzer`s that report compile-time and IDE diagnostics against C#
code, and the `CodeFixProvider`s that pair with them. Organized by task, not by Roslyn version —
the `DiagnosticAnalyzer`/`CodeFixProvider` API surface has been stable since its introduction; each
reference file notes a version-introduced fact inline where it matters.

An analyzer inspects code and reports `Diagnostic`s (warnings, errors, info messages shown in the
IDE and at build time); it never changes the compilation's output. A `CodeFixProvider` is the
separate, optional piece that offers an automated fix for one of an analyzer's diagnostic IDs.

## Quick start

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoConsoleWriteLineAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MYAN0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Avoid Console.WriteLine",
        messageFormat: "Use the injected ILogger instead of Console.WriteLine",
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod is { Name: "WriteLine", ContainingType.Name: "Console" })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
        }
    }
}
```

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Declaring `SupportedDiagnostics`, writing a `DiagnosticDescriptor` (id, category, severity, help link) | [references/diagnostic-descriptors.md](references/diagnostic-descriptors.md) |
| Choosing and registering a syntax/symbol/operation/compilation action | [references/registering-analyzer-actions.md](references/registering-analyzer-actions.md) |
| Deciding syntax-tree vs. symbol vs. operation analysis for a given check | [references/syntax-symbol-operation-analysis.md](references/syntax-symbol-operation-analysis.md) |
| Writing a `CodeFixProvider` paired with an analyzer's diagnostic | [references/code-fix-providers.md](references/code-fix-providers.md) |
| Setting up the analyzer project, `.csproj` shape, and packaging it as a NuGet package or `<Analyzer>` reference | [references/project-setup-and-packaging.md](references/project-setup-and-packaging.md) |
| Writing a test with `Microsoft.CodeAnalysis.Testing` for an analyzer or a code fix | [references/testing.md](references/testing.md) |

## Out of scope

- Generating new source code at compile time — an analyzer only reports diagnostics; it never adds
  or rewrites a compilation's emitted output. That is a different Roslyn extensibility mechanism
  entirely, with its own registration model, caching concerns, and packaging shape not covered
  here.
- General C# language-feature or BCL API guidance — this skill covers the Roslyn analyzer/code-fix
  authoring surface only, not the semantics of whatever C# construct a specific analyzer happens to
  inspect.
- IDE-specific extensibility (VS/VS Code extension APIs, custom light-bulb UI beyond
  `CodeFixProvider`) — out of scope beyond the standard `CodeFixProvider` registration.
