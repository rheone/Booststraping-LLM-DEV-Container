# Testing

`Microsoft.CodeAnalysis.Testing` (verified current stable release: the
`Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` package at 1.1.3, and the companion
`Microsoft.CodeAnalysis.CSharp.CodeFix.Testing` package, published by the Roslyn
team) is the standard harness for testing analyzers and code fixes without hand-assembling a
`Compilation` and driver yourself. Pick the verifier-framework flavor matching your test project's
test framework: `...Analyzer.Testing.XUnit`, `...Analyzer.Testing.MSTest`, or
`...Analyzer.Testing.NUnit` (and the `CodeFix.Testing.*` equivalents for fix tests).

## Testing an analyzer

`CSharpAnalyzerTest<TAnalyzer, TVerifier>` takes source text with diagnostics marked using
`{|ID:...|}` span syntax (or, for the xUnit verifier flavor, the simpler
`CSharpAnalyzerVerifier<TAnalyzer, TVerifier>.Diagnostic(...)` builder):

```csharp
[Fact]
public async Task Analyzer_FlagsConsoleWriteLine()
{
    const string source = """
        class C
        {
            void M() => {|MYAN0001:System.Console.WriteLine("hi")|};
        }
        """;

    await new CSharpAnalyzerTest<NoConsoleWriteLineAnalyzer, DefaultVerifier>
    {
        TestCode = source,
    }.RunAsync();
}
```

The `{|MYAN0001:...|}` span is both the assertion and the location — the test fails if the
analyzer reports zero, a different id, or a different span than the one marked. Test both a
triggering case and at least one non-triggering case (code that looks similar but shouldn't be
flagged) — an analyzer with only positive test cases can't tell you whether it's also silently
over-firing on adjacent, legitimate code.

## Testing a code fix

`CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier>` adds an expected post-fix source alongside the
pre-fix source:

```csharp
[Fact]
public async Task CodeFix_ReplacesWriteLineWithLoggerCall()
{
    const string before = """
        class C
        {
            void M() => {|MYAN0001:System.Console.WriteLine("hi")|};
        }
        """;
    const string after = """
        class C
        {
            void M() => _logger.LogInformation("hi");
        }
        """;

    await new CSharpCodeFixTest<NoConsoleWriteLineAnalyzer, AddBracesCodeFixProvider, DefaultVerifier>
    {
        TestCode = before,
        FixedCode = after,
    }.RunAsync();
}
```

This exercises the whole pipeline — the analyzer reports the diagnostic, and the fix provider's
`RegisterCodeFixesAsync` runs and produces exactly the `FixedCode` text. A mismatch (fix produces
different formatting, misses a using directive, leaves stray whitespace) fails the test with a
diff, so keep `FixedCode` byte-for-byte what the fix should actually produce, including
using-directive placement if the fix is supposed to add one.

## Testing "Fix All"

Set `TestCode`/`FixedCode` to a source containing multiple instances of the diagnostic and set
`NumberOfFixAllIterationsPerCodeAction` if the fix needs more than one editing pass to reach a
stable fixed-point; the default `CSharpCodeFixTest` already runs its fix through the configured
`FixAllProvider` when the source contains more than one diagnostic instance, so a multi-instance
test case doubles as your "Fix All" coverage without extra harness setup.

## Adding reference assemblies

If the analyzer's check depends on a specific BCL type or a NuGet package's type being resolvable
(checking for an attribute from a specific package, for instance), add it via
`TestState.AdditionalReferences` — a test failing with "type or namespace not found" from inside
the test's own compiled source almost always means a missing reference here, not an analyzer bug.

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| The analyzer reports (or correctly doesn't report) a diagnostic at the right location | `CSharpAnalyzerTest` with a positive and a negative case |
| A registered fix transforms flagged code into the exact expected result | `CSharpCodeFixTest` with `TestCode`/`FixedCode` |
| The fix behaves correctly when multiple instances exist in one document | The same `CSharpCodeFixTest`, with multiple diagnostic instances in `TestCode` |

Cover every distinct triggering shape the analyzer's logic branches on (not just one happy-path
example) — a `RegisterOperationAction`-based check with several `if` branches on operation shape
needs a test case per branch, the same way ordinary business logic does.
