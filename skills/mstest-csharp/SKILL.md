---
name: mstest-csharp
description: Write, update, and improve MSTest unit tests in C# projects. Covers naming conventions, AAA structure, parameterized tests, mocking, coverage gap detection, fixture patterns, and MSTest-specific patterns. Use when writing or reviewing C# unit tests in MSTest projects.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.0
---

# MSTest C# Testing Skill

> **Stub** — populate this file the first time a sweep runs against an MSTest project.
> The rules below are the known framework-agnostic defaults. MSTest-specific patterns
> (e.g. `[DataTestMethod]`, `[DataRow]`, `[DynamicData]`, `[TestInitialize]`/`[TestCleanup]`,
> `[ClassInitialize]`, `[TestClass]`, `Assert.ThrowsException`) should be added here on first use.

## Core Rules (Framework-Agnostic)

- Test method names: `{Member}_{Scenario}_{Expectation}_Test`
- Every test follows Arrange / Act / Assert with section comments
- Each parameterized test covers: happy path, null/empty/whitespace inputs, boundary values
- Exception tests use `Assert.ThrowsException<T>` — never try/catch
- No shared static mutable state between tests
- Never substitute the class under test — use a concrete subclass instead. See the companion mocking skill for the detected library (e.g. [`nsubstitute-csharp`](../nsubstitute-csharp/SKILL.md), [`moq-csharp`](../moq-csharp/SKILL.md)), [`justmock-csharp`](../moq-csharp/SKILL.md)), or 
[`rhinomock-csharp/SKILL.md))

## MSTest API Landmarks (to expand on first use)

| Concern | MSTest attribute / API |
|---------|------------------------|
| Test method | `[TestMethod]` |
| Parameterized test | `[DataTestMethod]` + `[DataRow(...)]` or `[DynamicData(nameof(...))]` |
| Setup / teardown | `[TestInitialize]`, `[TestCleanup]`, `[ClassInitialize]`, `[ClassCleanup]` |
| Test class | `[TestClass]` |
| Ignore with reason | `[Ignore]` (no inline reason — add a comment) |
| Assertion style | `Assert.AreEqual(expected, actual)` |
| Exception assertion | `Assert.ThrowsException<T>(() => ...)` |
| Async test | Returns `Task`; `await Assert.ThrowsExceptionAsync<T>` |

## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../csharp-test-sweep/SKILL.md) when it detects MSTest in the project file. To run a full sweep that delegates here automatically