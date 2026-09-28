# Code Fix Providers

A `CodeFixProvider` is a separate class from the analyzer it pairs with — it offers an automated
fix an IDE surfaces as a light-bulb action for one or more of that analyzer's diagnostic ids. An
analyzer never requires a paired fix provider; many diagnostics (ones with no single obviously
correct rewrite) ship analyzer-only.

## Anatomy

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBracesCodeFixProvider))]
[Shared]
public sealed class AddBracesCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [NoConsoleWriteLineAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var diagnostic = context.Diagnostics[0];
        var node = root!.FindNode(diagnostic.Location.SourceSpan);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with ILogger call",
                createChangedDocument: ct => ReplaceWithLoggerCallAsync(context.Document, node, ct),
                equivalenceKey: "ReplaceWithLoggerCall"),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithLoggerCallAsync(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        // ...construct and replace `node` with the new invocation expression...
        return editor.GetChangedDocument();
    }
}
```

- **`[ExportCodeFixProvider(LanguageNames.CSharp, Name = ...)]`** plus **`[Shared]`** — required for
  MEF discovery; without both attributes, the IDE never finds the provider even if it's in the same
  assembly as the analyzer.
- **`FixableDiagnosticIds`** — the set of diagnostic ids this provider can fix. Roslyn only invokes
  `RegisterCodeFixesAsync` for a diagnostic whose id appears here.
- **`RegisterCodeFixesAsync`** — inspects `context.Diagnostics` (usually just the one diagnostic at
  the requested location) and calls `context.RegisterCodeFix` for each fix it can offer. A single
  diagnostic can have more than one registered fix; the IDE presents them as alternative light-bulb
  actions.
- **`equivalenceKey`** — a stable string identifying this specific fix *kind*, used by
  `FixAllProvider` to group equivalent fixes together when a user chooses "Fix all occurrences in
  document/project/solution." Omit it and "Fix All" support silently degrades to fixing one
  instance at a time.

## `GetFixAllProvider`

`WellKnownFixAllProviders.BatchFixer` handles the common case — it reruns the same
`CodeFixContext`-based fix logic against every matching diagnostic and merges the resulting
document changes. Only implement a custom `FixAllProvider` when the batch fixer's naive
per-diagnostic-then-merge strategy produces conflicting edits for this specific fix (rare, and
worth confirming with a test case that actually exercises "fix all" before reaching for a custom
implementation).

## Using `DocumentEditor` vs. raw syntax rewriting

`Microsoft.CodeAnalysis.Editing.DocumentEditor` (used above) tracks node identity across edits and
lets you call `ReplaceNode`/`InsertAfter`/`RemoveNode` without manually re-deriving spans after each
change — prefer it over hand-rolled `SyntaxNode.ReplaceNode` chains for anything beyond a single,
isolated node replacement, since manually chaining raw `SyntaxNode` replacements requires
re-fetching stale node references after every edit.

## Preserving trivia

A fix that removes or replaces a node should carry over meaningful leading/trailing trivia
(comments, blank-line spacing) from the original node onto its replacement — dropping trivia
silently strips comments sitting on the line being fixed, which reads as data loss to whoever
receives the fix.
