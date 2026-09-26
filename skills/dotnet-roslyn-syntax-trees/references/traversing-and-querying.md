# Traversing and Querying

You locate nodes in a parsed tree either by walking it with a visitor or by querying it directly
with the `SyntaxNode` extension methods.

## Direct query methods

Every `SyntaxNode` exposes LINQ-friendly traversal methods; filter them with `OfType<T>()` rather
than checking `Kind()` in a `Where` clause:

```csharp
IEnumerable<InvocationExpressionSyntax> invocations = root
    .DescendantNodes()
    .OfType<InvocationExpressionSyntax>();

IEnumerable<ClassDeclarationSyntax> publicClasses = root
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .Where(c => c.Modifiers.Any(SyntaxKind.PublicKeyword));
```

- `DescendantNodes()` / `DescendantNodesAndSelf()` — every node below (and optionally including)
  this one, depth-first.
- `DescendantTokens()` — every token below this node, in source order.
- `DescendantTrivia()` — every trivia entry below this node; pass `descendIntoTrivia: true` to also
  reach trivia nested inside structured trivia (e.g. inside a `#region` or an XML doc comment).
- `Ancestors()` / `AncestorsAndSelf()` — walk upward from a node to the tree root; use
  `FirstAncestorOrSelf<T>()` to find the nearest enclosing node of a given type (the containing
  method, class, or namespace).
- `ChildNodes()` — direct children only, no recursion; use this over `DescendantNodes()` when you
  already know you're one level away and want to avoid over-matching a nested construct of the same
  kind (a local function nested inside the method you're inspecting, for example).

Pass a `descendIntoChildren` predicate to `DescendantNodes(Func<SyntaxNode, bool>)` to prune a
subtree during the walk — for example, to collect invocations but skip the bodies of local
functions and lambdas entirely, rather than filtering them out after the fact.

## Visitor traversal with `CSharpSyntaxWalker`

Use a walker when you need to collect data across many different node kinds in one pass, or when
you need to track state (nesting depth, the enclosing method) as you go:

```csharp
public sealed class PublicMethodCollector : CSharpSyntaxWalker
{
    public List<MethodDeclarationSyntax> Methods { get; } = [];

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            Methods.Add(node);
        }

        base.VisitMethodDeclaration(node); // descend into nested local functions/lambdas
    }
}

var collector = new PublicMethodCollector();
collector.Visit(root);
```

Every `Visit*` override that wants to keep descending into that node's children must call the
`base.Visit*` method itself — `CSharpSyntaxWalker` does not auto-recurse underneath an override.
Omitting the `base` call is the most common way a walker silently stops short of nested
declarations (a method inside a local function, a class inside a class).

`CSharpSyntaxWalker`'s constructor takes a `SyntaxWalkerDepth` (`Node`, `Token`, `Trivia`,
`StructuredTrivia`) that controls whether `VisitToken`/`VisitTrivia` overrides fire at all; the
default (`Node`) never calls them, so override `VisitToken` only after passing `SyntaxWalkerDepth
.Token` or deeper to the base constructor.

## Choosing between the two

- A one-shot filter for a single node type — direct query methods (`OfType<T>()`), no walker
  needed.
- Multiple node kinds, or state that must accumulate across the walk (nesting depth, a running
  symbol table, a "am I inside a lambda" flag) — a `CSharpSyntaxWalker` subclass.
- A query that also needs semantic information (is this identifier a method group, what type does
  this expression have) — combine either traversal style with a `SemanticModel`; see
  [semantic-model-and-symbols.md](semantic-model-and-symbols.md).
