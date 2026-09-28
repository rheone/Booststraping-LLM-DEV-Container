# Roslyn Syntax Trees

This skill covers parsing C# source into a syntax tree, querying and walking it, building or
rewriting syntax nodes, and resolving symbols and types through Roslyn's semantic model — the
Microsoft.CodeAnalysis Syntax API used to build codemods, refactoring tools, and source-inspection
utilities directly against C# source.

## When to reach for it

- Parsing a `.cs` file or a source string into a tree you can inspect programmatically.
- Finding every occurrence of a construct (a method call, a class with a certain attribute, a
  particular statement shape) across a file or project.
- Writing a tool that rewrites C# source mechanically — renaming a member, replacing one API call
  with another, restructuring a statement — while preserving comments and formatting.
- Figuring out what an identifier or invocation actually refers to (its declaring type, its
  resolved overload) rather than just what it looks like syntactically.
- Deciding between a raw text edit and building the change as a `SyntaxNode` so it composes safely
  with other structural transformations.

## Using it

This skill fires automatically when your request involves parsing, querying, rewriting, or
semantically resolving C# syntax with the Roslyn APIs. You can also invoke it directly with
`/dotnet-roslyn-syntax-trees`.

## What it covers

| Topic | Reference |
| --- | --- |
| Parsing, the node/token/trivia model, spans and locations | [references/parsing-and-tree-structure.md](references/parsing-and-tree-structure.md) |
| `DescendantNodes`/`Ancestors` queries and `CSharpSyntaxWalker` | [references/traversing-and-querying.md](references/traversing-and-querying.md) |
| `SyntaxFactory`, `With*`/`ReplaceNode` edits, `CSharpSyntaxRewriter` | [references/constructing-and-rewriting.md](references/constructing-and-rewriting.md) |
| `Compilation`/`SemanticModel`, resolving symbols and types | [references/semantic-model-and-symbols.md](references/semantic-model-and-symbols.md) |
| `NormalizeWhitespace`, `Formatter.Format`, writing text back out | [references/formatting-and-applying-edits.md](references/formatting-and-applying-edits.md) |
| Testing parsers, queries, rewriters, and semantic lookups | [references/testing.md](references/testing.md) |

## Example prompts

- "Parse this C# file and list every public method that doesn't have an XML doc comment."
- "Write a rewriter that replaces every `Console.WriteLine` call with a logger call, keeping
  comments intact."
- "Given this invocation expression, what method does it actually resolve to?"
