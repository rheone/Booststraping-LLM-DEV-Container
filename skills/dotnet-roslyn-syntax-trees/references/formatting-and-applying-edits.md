# Formatting and Applying Edits

Once you've built or rewritten a tree, you need it laid out with sensible whitespace and, usually,
written back to a `.cs` file or an in-memory project.

## `NormalizeWhitespace()` — standalone, no workspace needed

```csharp
SyntaxNode formatted = generatedNode.NormalizeWhitespace();
string sourceText = formatted.ToFullString();
```

`NormalizeWhitespace()` applies a fixed, minimal formatting style (consistent indentation, one
statement per line, standard brace placement) to a node and everything below it, replacing all
existing trivia. It needs no `Workspace` or project context, which makes it the right choice for
small generated fragments (a single new method, a field declaration) and for command-line tools
that parse and rewrite files without ever opening a solution. It does not honor an `.editorconfig`
or any project-specific style — it always produces the same fixed style.

## `Formatter.Format` — workspace-aware, `.editorconfig`-sensitive

```csharp
using Microsoft.CodeAnalysis.Formatting;

SyntaxNode formatted = Formatter.Format(root, workspace);
```

`Formatter.Format` (from the `Microsoft.CodeAnalysis.Workspaces` / `Microsoft.CodeAnalysis.CSharp
.Workspaces` packages) reads formatting options from a `Workspace` — including a loaded project's
`.editorconfig` — so its output matches the target codebase's actual style instead of a fixed
default. It needs a `Workspace` instance; an `AdhocWorkspace` (no solution/project loaded) still
works and falls back to Roslyn's built-in default options when no `.editorconfig` is in scope.

Use `NormalizeWhitespace()` for a self-contained snippet or tool with no project to read style
from; use `Formatter.Format` when you're editing inside a `Workspace`/`Document` context that
already has one, so the result matches the surrounding file instead of introducing a second style.

## Getting text back out of a tree

```csharp
string code = root.ToFullString();            // full text, all trivia included
SourceText text = root.GetText(Encoding.UTF8); // a SourceText, for further Roslyn API calls
File.WriteAllText(path, code, Encoding.UTF8);
```

`ToString()` on a node also returns source text but excludes the node's own leading/trailing
trivia at the boundary (an inner node's internal trivia is unaffected) — prefer `ToFullString()`
whenever you're writing a complete file or top-level node back out, so you don't silently drop a
leading `using` block's trivia or a trailing newline.

## Applying an edit inside a `Workspace`/`Document`

When you're editing a file that's part of an open `Solution`/`Project` (an analyzer's code fix, a
refactoring tool that operates across a whole project) rather than a standalone parsed string,
route the edit through the `Document`, not the raw tree, so the workspace's change tracking and
any downstream `Formatter.Format` options stay consistent:

```csharp
Document document = project.Documents.First(d => d.Name == "Order.cs");
SyntaxNode oldRoot = await document.GetSyntaxRootAsync();
SyntaxNode newRoot = oldRoot.ReplaceNode(oldMethod, newMethod);

Document newDocument = document.WithSyntaxRoot(newRoot);
Solution newSolution = newDocument.Project.Solution;
```

`document.WithSyntaxRoot` (and every `With*` on `Document`/`Project`/`Solution`) is immutable the
same way `SyntaxNode.With*` is — it returns a new `Document`/`Solution`, leaving the one you called
it on untouched. Thread the returned `Solution` forward through the rest of your edit sequence
instead of reusing the original.
