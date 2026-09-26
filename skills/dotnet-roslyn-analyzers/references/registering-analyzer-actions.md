# Registering Analyzer Actions

`DiagnosticAnalyzer.Initialize(AnalysisContext context)` runs once per analyzer instance and is
where every callback registration happens. Nothing about analysis runs unless it's registered here.

## Baseline `Initialize` setup

```csharp
public override void Initialize(AnalysisContext context)
{
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);
}
```

- **`ConfigureGeneratedCodeAnalysis`** — opt out of running against generated code
  (`GeneratedCodeAnalysisFlags.None`) unless the analyzer specifically needs to inspect
  generator-emitted files; most rules about hand-written code style or API usage should skip
  generated files to avoid false positives against code the team doesn't control.
- **`EnableConcurrentExecution`** — allows Roslyn to run this analyzer's callbacks across multiple
  threads for different syntax trees; safe to call as long as callback methods don't mutate shared
  state without synchronization. Nearly always safe and worth calling since it materially improves
  large-solution analysis time.

## `RegisterSyntaxNodeAction`

Fires once per syntax node matching the given `SyntaxKind`(s), operating on the raw syntax tree —
the literal textual shape of the code, before semantic binding resolves what any identifier refers
to:

```csharp
context.RegisterSyntaxNodeAction(context =>
{
    var ifStatement = (IfStatementSyntax)context.Node;
    if (ifStatement.Else is null && ifStatement.Statement is not BlockSyntax)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.GetLocation()));
    }
}, SyntaxKind.IfStatement);
```

Reach for this when the check is purely structural/textual — brace style, statement shape,
attribute presence on a declaration — and doesn't need to know what a symbol actually resolves to.

## `RegisterSymbolAction`

Fires once per declared symbol of the given `SymbolKind`(s), after name binding — the analyzer sees
a resolved `ISymbol`, not raw syntax:

```csharp
context.RegisterSymbolAction(context =>
{
    var namedType = (INamedTypeSymbol)context.Symbol;
    if (namedType.TypeKind == TypeKind.Interface
        && !namedType.GetAttributes().Any(a => a.AttributeClass?.Name == "ServiceContractAttribute"))
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, namedType.Locations[0]));
    }
}, SymbolKind.NamedType);
```

Reach for this when the check is about a type/method/property/field's declared shape or its
relationship to other symbols (implements an interface, has a specific attribute, matches a naming
rule) rather than about a specific expression's syntax.

## `RegisterOperationAction`

Fires once per `IOperation` node of the given `OperationKind`(s) — a semantic layer that normalizes
different syntactic forms of the same underlying behavior (e.g. a `foreach` loop and a manual
`while` over an enumerator can both surface related `IOperation` shapes where a syntax-only check
would have to handle each form separately):

```csharp
context.RegisterOperationAction(context =>
{
    var invocation = (IInvocationOperation)context.Operation;
    if (invocation.TargetMethod is { Name: "WriteLine", ContainingType.Name: "Console" })
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
    }
}, OperationKind.Invocation);
```

Reach for this when the check is about program *behavior* (a specific method being called, a
specific kind of conversion happening, an await expression's awaited type) rather than the
declaration shape `RegisterSymbolAction` covers or the raw text shape `RegisterSyntaxNodeAction`
covers. See [syntax-symbol-operation-analysis.md](syntax-symbol-operation-analysis.md) for the
fuller decision guide.

## `RegisterCompilationStartAction` for per-compilation state

When a check needs to accumulate state across multiple syntax trees before reporting anything (for
example, collecting every type implementing a marker interface across the whole compilation, then
flagging ones missing a required companion type), register a `RegisterCompilationStartAction` and
do the finer-grained registration *inside* it, against the `CompilationStartAnalysisContext` it
hands you:

```csharp
context.RegisterCompilationStartAction(startContext =>
{
    var knownTypes = new ConcurrentBag<INamedTypeSymbol>();
    startContext.RegisterSymbolAction(c => knownTypes.Add((INamedTypeSymbol)c.Symbol), SymbolKind.NamedType);
    startContext.RegisterCompilationEndAction(endContext =>
    {
        // inspect the fully collected `knownTypes` here
    });
});
```

This is the correct place for any state that needs to survive across the whole compilation rather
than resetting per syntax tree or per symbol — state stored directly on the analyzer instance
fields is unsafe once `EnableConcurrentExecution` is on, since the same analyzer instance is shared
and invoked concurrently.
