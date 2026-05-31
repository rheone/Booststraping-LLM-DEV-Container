---
name: rhinomocks-csharp
description: Write, update, and improve RhinoMocks mock setups in C# test projects. Covers the abstract-class interception trap and call verification. Use when writing or reviewing RhinoMocks usage in any C# test project regardless of test framework.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.0
---

# RhinoMocks C# Mocking Skill

> **Stub** — populate this file the first time a sweep runs against a project using RhinoMocks
> or a RhinoMocks-backed AutoMock container. The patterns below are the universal rules
> restated in RhinoMocks' API. RhinoMocks-specific patterns (record/replay model,
> `MockRepository`, `Expect`, `Stub`, `AssertWasCalled`, `AAA` style via `GenerateMock`)
> should be added here on first use.
>
> Note: RhinoMocks is largely unmaintained as of 2020. If the project uses it, consider
> flagging migration to NSubstitute or Moq as a sweep finding.
>
> These rules apply regardless of test framework (xUnit, NUnit, MSTest).

## Core Rules

- `MockRepository.GenerateMock<T>()` is for **dependencies**, not the **subject under test**.
- **Never mock the class under test** — `GenerateMock<T>()` and `GenerateStub<T>()` intercept virtual methods on `T`; if `T` is the class under test, the method under test never executes.
- If the method under test lives on an abstract base class, instantiate a **concrete subclass** that inherits the implementation without overriding it.
- Mock verification (`AssertWasCalled`) belongs in the Assert section.

## Anti-Pattern: Mocking the Class Under Test

`MockRepository.GenerateMock<T>()` and `MockRepository.GenerateStub<T>()` intercept virtual methods on `T`. If `T` is the class under test, the method under test never executes.

**Fixed — use a concrete subclass:**
```csharp
// Instead of: var sut = MockRepository.GenerateMock<AbstractProcessor>();
var sut = new ConcreteProcessor(); // inherits without overriding
var result = sut.Process("input");
Assert.Equal("expected", result);
```

## RhinoMocks API Landmarks (to expand on first use)

| Concern | RhinoMocks API |
|---------|----------------|
| Create mock (AAA style) | `MockRepository.GenerateMock<IFoo>()` |
| Create stub | `MockRepository.GenerateStub<IFoo>()` |
| Stub return value | `stub.Stub(x => x.Method(arg)).Return(value);` |
| Verify call (AAA style) | `mock.AssertWasCalled(x => x.Method(arg));` |
| Verify no call | `mock.AssertWasNotCalled(x => x.Method(Arg<T>.Is.Anything));` |
| Argument matcher | `Arg<T>.Is.Equal(value)`, `Arg<T>.Is.Anything` |


## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../csharp-test-sweep/SKILL.md) when it detects NSubstitute (or Moq / JustMock / RhinoMocks) in the project file. To run a full test suite sweep that delegates here automatically: