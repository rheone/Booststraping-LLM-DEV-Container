# Semantic Model and Symbols

A `SyntaxTree` alone only tells you what the source *looks like*. To find out what an identifier
*refers to* — its declaring type, its resolved overload, whether it's even valid — you need a
`Compilation` and the `SemanticModel` it produces for that tree.

## Building a compilation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);

CSharpCompilation compilation = CSharpCompilation.Create("Analysis")
    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
    .AddSyntaxTrees(tree);

SemanticModel semanticModel = compilation.GetSemanticModel(tree);
```

A `Compilation` needs `MetadataReference`s for every assembly the source depends on — at minimum
the current runtime's core library (`typeof(object).Assembly.Location`), and one reference per
additional assembly (`System.Linq`, a project's own compiled output, a NuGet package's DLL) the
code under analysis actually calls into. A `SemanticModel` from a compilation missing a reference
still returns results, but any symbol from the missing assembly resolves as `null` or an
`IErrorTypeSymbol` — check for that before trusting a "not found" as a real finding rather than a
missing-reference artifact.

## Resolving symbols from syntax

```csharp
InvocationExpressionSyntax invocation = root.DescendantNodes()
    .OfType<InvocationExpressionSyntax>()
    .First();

SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
if (symbolInfo.Symbol is IMethodSymbol method)
{
    string fullName = $"{method.ContainingType}.{method.Name}";
}

TypeInfo typeInfo = semanticModel.GetTypeInfo(invocation);
ITypeSymbol? resultType = typeInfo.Type;
```

- **`GetSymbolInfo(node)`** — what declaration a reference (an identifier, an invocation, a
  constructor call) resolves to. Returns a `SymbolInfo` with `.Symbol` (the resolved symbol, or
  `null` if it didn't bind) and `.CandidateSymbols` (the overload candidates when resolution was
  ambiguous — check this on a `null` `.Symbol` before concluding the reference is simply invalid).
- **`GetTypeInfo(node)`** — the static type of an expression, including any implicit conversion
  (`.Type` is the expression's own type, `.ConvertedType` is the type it converts to at that usage
  site).
- **`GetDeclaredSymbol(node)`** — the symbol a *declaration* node introduces (a
  `ClassDeclarationSyntax`, `MethodDeclarationSyntax`, `ParameterSyntax`, or `VariableDeclaratorSyntax`
  each declare exactly one symbol; use this instead of `GetSymbolInfo` at a declaration site,
  which only resolves *references*).

## `ISymbol` vs. `SyntaxNode`

A symbol represents a semantic entity independent of any one syntax reference to it — the same
`IMethodSymbol` for `Order.Validate` is what every call site's `GetSymbolInfo` resolves to, and it
may have no single `SyntaxNode` at all (a symbol from a referenced assembly with no source, or a
compiler-synthesized member). Use `symbol.DeclaringSyntaxReferences` to get back to the
`SyntaxNode`(s) that declare a symbol defined in source — a partial class or partial method can
have more than one.

## Common pitfall: stale semantic models after a rewrite

A `SemanticModel` is bound to one specific `SyntaxTree` instance. After you produce a new root with
`ReplaceNode` or a `CSharpSyntaxRewriter` (see
[constructing-and-rewriting.md](constructing-and-rewriting.md)), the old `SemanticModel` no longer
matches the new tree — nodes you ask it about that came from the new root either throw or return
default/`null` results. Re-create the compilation (`compilation.ReplaceSyntaxTree(oldTree,
newTree)`) and re-request `GetSemanticModel(newTree)` before running any further semantic query
against edited syntax.
