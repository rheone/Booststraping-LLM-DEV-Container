# Constructing and Rewriting

You build new syntax with `SyntaxFactory`, modify existing syntax through `With*` methods, and
apply a systematic transformation across a whole tree with `CSharpSyntaxRewriter`.

## Building nodes with `SyntaxFactory`

`SyntaxFactory` is a static class with one factory method per node/token/trivia kind. Prefer
parsing a small snippet over hand-assembling deeply nested factory calls when the shape is fixed
and only a few pieces vary:

```csharp
// Hand-built: verbose, but lets you plug in typed subexpressions.
FieldDeclarationSyntax field = SyntaxFactory.FieldDeclaration(
        SyntaxFactory.VariableDeclaration(
            SyntaxFactory.ParseTypeName("string"),
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator("_name"))))
    .WithModifiers(SyntaxFactory.TokenList(
        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

// Parsed: shorter when the shape is a known literal string, still a real SyntaxNode afterward.
MemberDeclarationSyntax parsedField = SyntaxFactory.ParseMemberDeclaration(
    "private readonly string _name;");
```

`SyntaxFactory.ParseExpression`, `ParseStatement`, `ParseMemberDeclaration`, and
`ParseCompilationUnit` each parse a standalone fragment without needing a full source file —
reach for these whenever you're generating a small, textually-known piece rather than composing it
node by node.

## Modifying with `With*` methods

Every settable part of a node has a corresponding `With<PropertyName>` method that returns a new
node with that one part replaced, leaving everything else — including trivia — untouched:

```csharp
MethodDeclarationSyntax renamed = originalMethod
    .WithIdentifier(SyntaxFactory.Identifier("ProcessAsync"))
    .WithModifiers(originalMethod.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
```

Because nodes are immutable, a `With*` call on a child does not update its parent — you get a new
detached node. To fold an edit back into a full tree, replace the node in place:

```csharp
SyntaxNode newRoot = root.ReplaceNode(originalMethod, renamed);
```

`ReplaceNode`/`ReplaceNodes`/`ReplaceToken`/`InsertNodesBefore`/`InsertNodesAfter`/`RemoveNode` all
work the same way: they return a new root reflecting the edit, tracking down through every
ancestor between the target and the root you called them on.

## Systematic rewrites with `CSharpSyntaxRewriter`

Use a rewriter instead of manual `ReplaceNode` calls when the same transformation applies at every
matching site across a tree — a rewriter walks the whole tree once and rebuilds only the branches
that actually changed:

```csharp
public sealed class ConsoleWriteLineToLoggerRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: "Console" },
                Name.Identifier.Text: "WriteLine",
            })
        {
            return SyntaxFactory.ParseExpression("_logger.LogInformation(...)")
                .WithTriviaFrom(node);
        }

        return base.VisitInvocationExpression(node);
    }
}

SyntaxNode rewrittenRoot = new ConsoleWriteLineToLoggerRewriter().Visit(root);
```

- Every override that returns a replacement **must** carry the original node's trivia forward with
  `.WithTriviaFrom(node)` (or by copying `GetLeadingTrivia()`/`GetTrailingTrivia()` individually) —
  otherwise the replacement silently drops the comment or blank-line spacing that was attached to
  the node it replaces.
- Return `null` from a `Visit*` override to delete the node entirely (valid for a statement or
  member; not valid where the grammar requires a node, such as an expression operand).
- Call `base.Visit*` (as the example does in its fallback branch) to keep descending into children
  that don't match — a rewriter that never calls `base` only ever looks at the root's immediate
  child, not the whole tree.

## Preserving formatting when generating new code

New nodes built entirely from `SyntaxFactory` calls have no trivia at all by default, and a rewrite
that mixes generated nodes with parsed ones ends up with inconsistent whitespace. Call
`.NormalizeWhitespace()` on the finished node or root to apply consistent, minimal formatting before
handing it back to a compilation or writing it to disk — see
[formatting-and-applying-edits.md](formatting-and-applying-edits.md) for the difference between
`NormalizeWhitespace()` and workspace-aware `Formatter.Format`.
