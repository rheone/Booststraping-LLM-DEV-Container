---
name: dotnet-roslyn-syntax-trees
description: Guidance on parsing, traversing, constructing, and rewriting C# abstract syntax trees with the Roslyn Syntax API (Microsoft.CodeAnalysis.CSharp, verified current 5.9.0, MIT-licensed, targeting .NET 8.0/netstandard2.0) — CSharpSyntaxTree.ParseText, the SyntaxNode/SyntaxToken/SyntaxTrivia model, DescendantNodes/Ancestors queries, CSharpSyntaxWalker traversal, SyntaxFactory node construction, With*/ReplaceNode immutable edits, CSharpSyntaxRewriter transformations, resolving symbols and types through a Compilation's SemanticModel, and formatting output with NormalizeWhitespace or workspace-aware Formatter.Format. Use when parsing C# source into a syntax tree, walking or querying a tree for a construct, building or rewriting syntax nodes, resolving what an identifier or invocation refers to, or writing a standalone codemod/refactoring/source-inspection tool built directly on the Roslyn Syntax API.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Roslyn Syntax Trees

The Roslyn Syntax API parses C# source into an immutable tree of `SyntaxNode`, `SyntaxToken`, and
`SyntaxTrivia` values, lets you query and rewrite that tree, and — through a `Compilation`'s
`SemanticModel` — tells you what each piece of syntax actually resolves to. This organizes by task,
not by version: the Syntax API's shape (`SyntaxFactory`, `SyntaxNode.With*`,
`CSharpSyntaxWalker`/`CSharpSyntaxRewriter`) has been stable since Roslyn's public release, and each
reference file names a package-version-specific fact inline where one applies.

## Quick start

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);
CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

var methodNames = root.DescendantNodes()
    .OfType<MethodDeclarationSyntax>()
    .Select(m => m.Identifier.Text);
```

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Parsing source text, understanding the node/token/trivia model, spans and locations | [references/parsing-and-tree-structure.md](references/parsing-and-tree-structure.md) |
| Finding nodes with `DescendantNodes`/`Ancestors`, writing a `CSharpSyntaxWalker` | [references/traversing-and-querying.md](references/traversing-and-querying.md) |
| Building nodes with `SyntaxFactory`, editing with `With*`/`ReplaceNode`, writing a `CSharpSyntaxRewriter` | [references/constructing-and-rewriting.md](references/constructing-and-rewriting.md) |
| Resolving a symbol or type through a `Compilation`/`SemanticModel` | [references/semantic-model-and-symbols.md](references/semantic-model-and-symbols.md) |
| Reformatting generated/rewritten output, writing text back to a file or `Document` | [references/formatting-and-applying-edits.md](references/formatting-and-applying-edits.md) |
| Testing a parser/query/rewriter/semantic lookup | [references/testing.md](references/testing.md) |

## Out of scope

- Registering compiler diagnostics or IDE code fixes through the analyzer extensibility model
  (`DiagnosticAnalyzer`, `CodeFixProvider`, and their registration/packaging lifecycle) — a
  separate extensibility surface with its own action-registration model, incremental caching
  concerns, and NuGet packaging shape.
- Source generation (`IIncrementalGenerator`/`ISourceGenerator`) — a different pipeline that adds
  new source text to a compilation rather than parsing or rewriting the user's existing syntax
  tree, with its own incremental-caching model.
- IDE extensibility beyond the Syntax API itself (custom VS/VS Code UI, light bulbs, editor
  commands).
