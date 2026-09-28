# Testability

**Rule:** every skill you produce includes a testing reference — a file covering how to verify the
work the skill teaches, plus example use cases for the scenarios a reader hits most often. This is
not conditional on the skill's subject looking obviously testable: adapt the content to fit (see
below), but do not skip the file.

## What this looks like

Give the testing material its own file rather than folding a paragraph into the main reference —
testing a pattern's output is itself substantial enough content to earn a peer file. Name it for
what it does (`testing-with-builders.md`, `testing-a-source-generator.md`, `testing.md` for a
single-topic skill), and cover:

- **How to test it**: the concrete technique — what to assert on, what to mock or fake, what
  actually needs an integration test versus a unit test, and any gotcha specific to testing this
  kind of code (async timing, non-determinism, a generator's incremental caching, a DI container's
  resolution order).
- **The most likely scenarios**: worked examples for the two or three ways a reader actually uses
  this skill's subject in practice — not an exhaustive enumeration of every possible case, but the
  cases someone reaching for this skill is actually solving.

## Adapting this for a skill with no independent runtime behavior

A skill about a naming convention, a documentation-comment format, or a purely structural refactor
(partial-file splitting, code organization) has no runtime assertion to write — there's no method
call whose return value or side effect a unit test checks. The testing file still exists, but its
subject shifts from *runtime tests* to *verification*: how a reader confirms the convention was
actually followed. This looks like a checklist a reviewer or a lint/analyzer rule can apply
mechanically (does every partial file carry the right `DependentUpon` entry, does every public
member have the required doc-comment sections), plus the two or three most common ways the
convention gets violated in practice. The file's name and content adapt; its existence does not.

## Adapting this for a skill whose own subject is testing infrastructure

A skill about a test framework, a mocking library, or a database-reset tool for tests (the skill's
entire job is helping a reader write *other* tests) still needs its own testing file — its subject
is different, not absent. The reader-facing test infrastructure this skill teaches (a custom data
attribute, a shared fixture, a substitute-factory helper, a reset-configuration check) is itself
code that can be wrong, and *that* is what the testing file covers: how to verify the test
infrastructure itself behaves correctly, not how to test application code with it (that's the rest
of the skill's job). Name the file for that distinction (`testing-your-test-infrastructure.md`,
`testing-your-test-doubles.md`) rather than reusing a bare `testing.md`, which would misleadingly
suggest ordinary application-code testing guidance.

## One rule specific to testing-themed files about code-generating tools

When the skill's own subject *generates* code (a source generator, a scaffolding tool), its
testing file covers testing the generator's output — does it emit the expected source, does it
report the right diagnostics — never tests that merely validate this skill's own syntax examples
compile. Those are two different things: one tests code the reader's project produces at build
time, the other would just be re-testing the documentation.
