---
name: nunit-csharp
description: Write, update, and improve NUnit unit tests in C# projects. Covers naming conventions, AAA structure, parameterized tests, mocking, coverage gap detection, fixture patterns, and NUnit-specific patterns. Use when writing or reviewing C# unit tests in NUnit projects.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.0
---

# NUnit C# Testing Skill

> **Stub** — populate this file the first time a sweep runs against an NUnit project.
> The rules below are the known framework-agnostic defaults. NUnit-specific patterns
> (e.g. `[TestCase]`, `[TestCaseSource]`, `Assert.That`, `[SetUp]`/`[TearDown]`,
> `[OneTimeSetUp]`, `[TestFixture]`, constraint model) should be added here on first use.

## Core Rules (Framework-Agnostic)

- Test method names: `{Member}_{Scenario}_{Expectation}_Test`
- Every test follows Arrange / Act / Assert with section comments
- Each parameterized test covers: happy path, null/empty/whitespace inputs, boundary values
- Exception tests use `Assert.Throws<T>` — never try/catch
- No shared static mutable state between tests
- Never substitute the class under test — use a concrete subclass instead. See the companion mocking skill for the detected library (e.g. [`nsubstitute-csharp`](../nsubstitute-csharp/SKILL.md), [`moq-csharp`](../moq-csharp/SKILL.md)), [`justmock-csharp`](../moq-csharp/SKILL.md)), or 
[`rhinomock-csharp/SKILL.md))

## NUnit API Landmarks (to expand on first use)

| Concern | NUnit attribute / API |
|---------|-----------------------|
| Test method | `[Test]` |
| Parameterized test | `[TestCase(...)]`, `[TestCaseSource(nameof(...))]` |
| Setup / teardown | `[SetUp]`, `[TearDown]`, `[OneTimeSetUp]`, `[OneTimeTearDown]` |
| Test class | `[TestFixture]` (optional in modern NUnit) |
| Ignore with reason | `[Ignore("reason")]` |
| Assertion style | `Assert.That(actual, Is.EqualTo(expected))` |
| Exception assertion | `Assert.Throws<T>(() => ...)` |
| Async test | Returns `Task`; `Assert.ThrowsAsync<T>` |


## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../csharp-test-sweep/SKILL.md) when it detects NUnit in the project file. To run a full sweep that delegates here automatically