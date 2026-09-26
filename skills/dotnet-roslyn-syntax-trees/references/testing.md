# Testing Syntax-Tree Code

Code that parses, queries, rewrites, or reformats C# source is ordinary code with deterministic
inputs and outputs — you test it the same way you test any pure transformation: known source text
in, known source text (or a known set of extracted facts) out. You don't need a compiler, a
running process, or a real project on disk for most of it.

## How to test it

- **Parsing and querying**: assert directly on the tree shape. Parse a small, self-contained
  snippet with `CSharpSyntaxTree.ParseText`, run your traversal, and assert on the count, names, or
  `Kind()` values of what it finds. No mocking is involved — `SyntaxTree`/`SyntaxNode` are plain
  data, not services with side effects to substitute.
- **Rewriting**: assert on the *emitted text*, not just that a `SyntaxNode` reference changed.
  Compare `newRoot.NormalizeWhitespace().ToFullString()` against an expected string (or, more
  robustly, re-parse the output and assert on its structure with `IsEquivalentTo` against a
  separately-parsed expected tree) — a rewriter that returns a structurally correct but
  trivia-mangled tree passes a symbol-level assertion while producing broken-looking output a
  human reviewer would reject.
- **Anything using a `SemanticModel`**: build the smallest `CSharpCompilation` that actually
  resolves the symbols under test — usually just `corlib`/`System.Runtime` plus the snippet itself.
  A missing reference doesn't throw; it makes a symbol resolve to `null` or an error type, which
  silently turns a real assertion into a false pass or a confusing failure far from its cause. See
  [semantic-model-and-symbols.md](semantic-model-and-symbols.md) for what a missing reference looks
  like at the API level.
- **Non-determinism check**: parsing and syntax-tree construction are fully deterministic for a
  given input string — the same source text always produces the same tree shape. If a test result
  varies between runs, the cause is almost always test state leaking between cases (a shared,
  mutated `CSharpCompilation` or `Workspace` instance), not the Roslyn APIs themselves; give each
  test its own tree/compilation instance rather than reusing one across test methods.
- **Golden-file comparisons for larger rewrites**: for a rewriter or generator whose output is a
  whole file rather than a short snippet, store the expected output as a checked-in fixture file
  and diff the actual result against it, rather than inlining a large expected string literal in
  the test body — this keeps the fixture reviewable as a normal source-code diff when the expected
  output legitimately changes.

## Most likely scenarios

**1. Unit-testing a query/collector against a snippet**

```csharp
[Fact]
public void Finds_all_public_methods()
{
    SyntaxTree tree = CSharpSyntaxTree.ParseText("""
        public class Order
        {
            public void Validate() { }
            private void Log() { }
        }
        """);

    var collector = new PublicMethodCollector();
    collector.Visit(tree.GetRoot());

    Assert.Single(collector.Methods);
    Assert.Equal("Validate", collector.Methods[0].Identifier.Text);
}
```

**2. Asserting a rewriter's output text**

```csharp
[Fact]
public void Rewrites_ConsoleWriteLine_to_logger_call()
{
    SyntaxTree tree = CSharpSyntaxTree.ParseText(
        "class C { void M() { Console.WriteLine(\"hi\"); } }");

    SyntaxNode rewritten = new ConsoleWriteLineToLoggerRewriter()
        .Visit(tree.GetRoot())
        .NormalizeWhitespace();

    Assert.Contains("_logger.LogInformation", rewritten.ToFullString());
    Assert.DoesNotContain("Console.WriteLine", rewritten.ToFullString());
}
```

**3. Asserting a symbol resolves through a minimal compilation**

```csharp
[Fact]
public void Resolves_invocation_to_expected_method_symbol()
{
    SyntaxTree tree = CSharpSyntaxTree.ParseText(
        "class C { void M() { System.Console.WriteLine(\"hi\"); } }");

    CSharpCompilation compilation = CSharpCompilation.Create("Test")
        .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
        .AddSyntaxTrees(tree);

    SemanticModel model = compilation.GetSemanticModel(tree);
    InvocationExpressionSyntax invocation = tree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();

    IMethodSymbol? method = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

    Assert.NotNull(method);
    Assert.Equal("WriteLine", method!.Name);
}
```
