---
name: moq-csharp
description: Write, update, and improve Moq mock setups in C# test projects. Covers the abstract-class interception trap, argument matchers, and call verification. Use when writing or reviewing Moq usage in any C# test project regardless of test framework.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.0
---

# Moq C# Mocking Skill

> **Stub** — populate this file the first time a sweep runs against a project using Moq.
> The patterns below are the universal rules restated in Moq's API. Moq-specific
> patterns (argument matchers, sequences, callbacks, `MockBehavior`, `SetupProperty`,
> `Capture`, `InSequence`) should be added here on first use.
>
> These rules apply regardless of test framework (xUnit, NUnit, MSTest).

## Core Rules

- `new Mock<T>()` is for **dependencies**, not the **subject under test**.
- **Never mock the class under test** — `new Mock<T>()` intercepts virtual methods on `T`, so if `T` is the class under test the method under test never executes.
- If the method under test lives on an abstract base class, instantiate a **concrete subclass** that inherits the implementation without overriding it.
- Mock verification (`mock.Verify`) belongs in the Assert section.

## Anti-Pattern: Mocking the Class Under Test

`new Mock<T>()` intercepts virtual methods on `T`. If `T` is the class under test, the method under test never executes.

**Broken:**
```csharp
var sut = new Mock<AbstractProcessor>();
sut.Setup(x => x.Process("input")).CallBase(); // easy to forget; defaults to returning null
var result = sut.Object.Process("input");
Assert.Equal("expected", result); // fails silently if CallBase omitted
```

**Fixed — use a concrete subclass:**
```csharp
var sut = new ConcreteProcessor(); // inherits AbstractProcessor.Process without overriding
var result = sut.Process("input");
Assert.Equal("expected", result);
```

**Rule**: `new Mock<T>()` is for *dependencies*, not the *subject under test*. Use `mock.Object` only when injecting the mock into another class.

## Moq API Landmarks (to expand on first use)

| Concern | Moq API |
|---------|---------|
| Create mock | `var mock = new Mock<IFoo>();` |
| Stub return value | `mock.Setup(x => x.Method(arg)).Returns(value);` |
| Verify call | `mock.Verify(x => x.Method(arg), Times.Once());` |
| Verify no call | `mock.Verify(x => x.Method(It.IsAny<T>()), Times.Never());` |
| Partial mock (call base) | `mock.Setup(x => x.Virtual()).CallBase();` |
| Argument matcher | `It.IsAny<T>()`, `It.Is<T>(v => ...)` |
| Strict mock (fail on unsetup) | `new Mock<IFoo>(MockBehavior.Strict)` |


## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../csharp-test-sweep/SKILL.md) when it detects NSubstitute (or Moq / JustMock / RhinoMocks) in the project file. To run a full test suite sweep that delegates here automatically: