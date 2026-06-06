---
name: justmock-csharp
description: Write, update, and improve Telerik JustMock mock setups in C# test projects. Covers the abstract-class interception trap, free vs elevated mode, and call verification. Use when writing or reviewing JustMock usage in any C# test project regardless of test framework.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.0
---

# Telerik JustMock C# Mocking Skill

> **Stub** — populate this file the first time a sweep runs against a project using JustMock.
> The patterns below are the universal rules restated in JustMock's API. JustMock-specific
> patterns (profiler-based mocking of non-virtual members, `Mock.Arrange`, `Mock.Assert`,
> `Behavior` modes, `Mock.Create` vs `Mock.CreateLike`, elevated trust mode) should be
> added here on first use.
>
> Note: JustMock has two modes — free (virtual members only, same constraints as other
> frameworks) and elevated/commercial (can mock non-virtual, static, and sealed members
> via the profiler). Document which mode the project uses.
>
> These rules apply regardless of test framework (xUnit, NUnit, MSTest).

## Core Rules

- `Mock.Create<T>()` is for **dependencies**, not the **subject under test**.
- **Never mock the class under test** — in free mode, `Mock.Create<T>()` intercepts virtual methods; in elevated mode it can intercept any method. Either way, the method under test never executes real logic.
- If the method under test lives on an abstract base class, instantiate a **concrete subclass** that inherits the implementation without overriding it.
- Mock verification (`Mock.Assert`) belongs in the Assert section.

## Anti-Pattern: Mocking the Class Under Test

In **free mode**, `Mock.Create<T>()` intercepts virtual methods on `T`. If `T` is the class under test, the method under test never executes. In **elevated mode**, even non-virtual methods can be intercepted — making this trap harder to spot.

**Fixed — use a concrete subclass (free mode):**
```csharp
// Instead of: var sut = Mock.Create<AbstractProcessor>();
var sut = new ConcreteProcessor(); // inherits without overriding
var result = sut.Process("input");
Assert.Equal("expected", result);
```

**Elevated mode note**: if `Mock.Create<ConcreteProcessor>()` is used in elevated mode, all methods (virtual or not) are interceptable. Always verify the actual implementation runs by checking the test result against expected behavior, not just call counts.

## JustMock API Landmarks (to expand on first use)

| Concern | JustMock API |
|---------|--------------|
| Create mock | `var mock = Mock.Create<IFoo>();` |
| Stub return value | `Mock.Arrange(() => mock.Method(arg)).Returns(value);` |
| Verify call | `Mock.Assert(() => mock.Method(arg), Occurs.Once());` |
| Verify no call | `Mock.Assert(() => mock.Method(Arg.AnyString), Occurs.Never());` |
| Argument matcher | `Arg.IsAny<T>()`, `Arg.Matches<T>(v => ...)` |
| Behavior mode | `Mock.Create<IFoo>(Behavior.Strict)` |


## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../csharp-test-sweep/SKILL.md) when it detects NSubstitute (or Moq / JustMock / RhinoMocks) in the project file. To run a full test suite sweep that delegates here automatically: