# Syntax, Symbol, and Operation Analysis

Three layers of information are available when writing a check, from least to most resolved.
Picking the right one keeps the analyzer simple and avoids re-deriving semantic information the
compiler has already computed.

## Syntax tree — the textual shape

The syntax tree is a literal, lossless representation of the source text: every brace, keyword,
and trivia (whitespace, comments) is present, but nothing about what an identifier *means* is
resolved yet. Two pieces of code that behave identically but are written differently (a
`ConfigureAwait(false)` call site written with or without a preceding blank line, an `if` with
versus without braces) are distinct syntax shapes.

Use `RegisterSyntaxNodeAction` for:

- Formatting/style rules (brace placement, statement-body shape).
- Checks about syntax that exists regardless of whether the code even compiles (a syntax tree
  exists even for code with semantic errors) — useful for very early, cheap checks.
- Anything about trivia (comments, `#region` placement) — trivia has no representation once you
  move to `IOperation` or `ISymbol`.

## Symbol — the declared, resolved shape

An `ISymbol` is a resolved declaration: an `INamedTypeSymbol`, `IMethodSymbol`,
`IPropertySymbol`, etc., with its full resolved shape (base types, implemented interfaces,
attributes, accessibility) available regardless of how many places or ways it's referenced.

Use `RegisterSymbolAction` for:

- Checks about a type/member's *declared* contract: "every class implementing `IHandler` must be
  `sealed`," "every public method must have an XML doc comment," "no field should be `public`."
- Checks that need to compare a declaration against its containing type/namespace/assembly
  metadata.
- Naming-convention checks, since a symbol's name is stable regardless of how it's referenced at
  each call site.

## Operation — the resolved behavior

`IOperation` is a semantic tree over what the code actually *does*, normalized across syntactic
variations that produce the same behavior. It resolves overload selection, implicit conversions,
and boxing that raw syntax doesn't expose.

Use `RegisterOperationAction` for:

- Checks about a specific API being called, regardless of how the call is spelled (extension method
  syntax vs. static method syntax that resolve to the same method).
- Checks that need to know an expression's resolved type after implicit conversion, or whether a
  value is boxed.
- Checks spanning behavior the syntax tree represents in multiple different shapes (loop forms,
  await patterns) that would otherwise need duplicated syntax-node handling per shape.

## When more than one layer looks plausible

Prefer the most resolved layer that still contains everything the check needs, since a
`RegisterOperationAction`/`RegisterSymbolAction` check written against fewer, semantically
normalized shapes tends to have fewer edge cases than the equivalent syntax-only check re-deriving
that normalization by hand. Drop to syntax only when the check is genuinely about the text itself
(formatting, trivia) rather than program meaning — reaching for `IOperation` there buys nothing and
throws away trivia information the check actually needs.
