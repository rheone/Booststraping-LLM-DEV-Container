# Parsing and Tree Structure

You parse C# source text into a `SyntaxTree` and read it as an immutable tree of `SyntaxNode`,
`SyntaxToken`, and `SyntaxTrivia` values.

## Parsing source text

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode, path: "Order.cs");
CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();
```

`ParseText` never throws on invalid syntax. A malformed input still produces a tree; the parser
inserts missing-token placeholders and skipped-token trivia so the tree stays structurally
complete. Check `root.ContainsDiagnostics` or enumerate `tree.GetDiagnostics()` to find parse
errors instead of expecting an exception.

## The three node kinds

- **`SyntaxNode`** — a non-terminal construct: a class declaration, a method body, a binary
  expression. Every node exposes `Kind()` (a `SyntaxKind` enum value), `Parent`, `ChildNodes()`,
  and `ChildTokens()`.
- **`SyntaxToken`** — a terminal symbol: an identifier, a keyword, a literal, punctuation. A token
  carries `Text` (as written) and `ValueText`/`Value` (the interpreted value — e.g. an escaped
  string literal's `Text` includes the quotes and escapes, `ValueText` does not).
  `SyntaxToken` is a `struct`, not a `SyntaxNode` subtype — you access it through a node's
  properties (e.g. `classDeclaration.Identifier`), not through `ChildNodes()`.
- **`SyntaxTrivia`** — whitespace, newlines, comments, and preprocessor directives, attached to a
  token's `LeadingTrivia`/`TrailingTrivia`. Trivia is never part of `ChildNodes()`/`ChildTokens()`
  traversal; it rides along with whichever token it precedes or follows.

## Trivia ownership convention

Roslyn attaches trivia to tokens using an end-of-line rule: trivia on the same line as the
preceding token (including a trailing comment) is trailing trivia on that token, and everything
from the next newline up to the following token — including that token's own leading blank lines
and indentation — is leading trivia on the following token. A leading `///` doc comment is leading
trivia on the member it documents, not trailing trivia on whatever came before it. Get this
backward and a trivia-preserving rewrite silently drops or duplicates comments.

## Immutability and identity

Every `SyntaxNode`, `SyntaxToken`, and `SyntaxTree` is immutable. Every `With*` method (see
[constructing-and-rewriting.md](constructing-and-rewriting.md)) returns a new tree; it never
mutates the one you called it on. Two nodes from the same parse are reference-comparable with
`IsEquivalentTo` for structural equality (ignoring trivia and annotations) — use that instead of
`==`, which compares the same node instance, not tree shape.

## Spans and locations

- `node.Span` — the `TextSpan` covering the node's tokens only, excluding leading/trailing trivia.
- `node.FullSpan` — the span including all trivia.
- `node.GetLocation()` — a `Location` you can pass to `Diagnostic.Create` or use for
  `GetLineSpan()` to get a 1-based line/column for error messages or tooling output.

## Common pitfall: `SyntaxKind` vs. C# type checks

Prefer a pattern match on the node's C# type (`if (node is MethodDeclarationSyntax method)`) over
`node.Kind() == SyntaxKind.MethodDeclaration` when you also need the node's members — the pattern
match gives you both the check and the typed reference in one step. Reserve a bare `Kind()`
comparison for a `switch` over many kinds where allocating a typed pattern per branch would be
noise.
