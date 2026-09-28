---
name: dotnet-justmock
description: Guidance on Telerik JustMock, a .NET mocking framework with a free JustMock Lite edition and a commercial edition sold by Progress/Telerik (latest verified NuGet package 2025.4.1112.487). Covers Mock.Create<T>(), Arrange/Act/Assert syntax, mocking non-virtual members, and the profiler-based "Elevated Mocking" mode required to mock static/sealed classes and non-virtual members (verified environment-variable and profiler setup), plus mocking async Task/Task<T>-returning methods with ReturnsAsync/ThrowsAsync and how ConfigureAwait behaves in a mocked async chain. JustMock carries a non-standard license — research current terms independently before adopting it. Use when writing or reviewing JustMock-based unit tests, deciding whether elevated/profiler-based mocking is needed for a given target, mocking a static or sealed class, or mocking an async method.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Telerik JustMock

Guidance on Telerik JustMock, a mocking framework for .NET unit tests distinguished from ordinary
interface/virtual-member mocking frameworks by its profiler-based "Elevated Mocking" mode, which
can mock non-virtual members, static members, and sealed classes. Organized by task, not by
JustMock release — the `Mock.Create`/Arrange-Act-Assert API surface is stable across versions; each
reference file notes a version-sensitive fact inline where one exists.

JustMock Lite covers ordinary mocking (interfaces, virtual/abstract members, non-sealed classes)
without the profiler. The commercial edition adds the profiler-based Elevated Mocking mode (see
below) plus mocking non-public members and BCL types (`DateTime`, `File`, and similar) — a project
on JustMock Lite alone cannot mock static/sealed/non-virtual targets no matter how the test is
written. JustMock carries a non-standard license — research current edition, trial, and pricing
terms independently before assuming elevated mocking is available for a project.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Learning `Mock.Create<T>()`, `Mock.Arrange`, `Act`, `Mock.Assert`, `Arg.IsAny<T>()` | [references/core-concepts.md](references/core-concepts.md) |
| Mocking a static class/method, a sealed class, or a non-virtual member — enabling and configuring the profiler | [references/elevated-mocking-setup.md](references/elevated-mocking-setup.md) |
| Mocking a `Task`/`Task<T>`-returning method, or reasoning about `ConfigureAwait` in a mocked async chain | [references/mocking-async-methods.md](references/mocking-async-methods.md) |
| Testing code that consumes JustMock-mocked dependencies | [references/testing.md](references/testing.md) |

## Quick start

```csharp
using Telerik.JustMock;

public interface IOrderRepository
{
    Task<Order?> FindAsync(Guid id);
}

[Fact]
public async Task ProcessOrder_WhenOrderExists_CompletesSuccessfully()
{
    var repository = Mock.Create<IOrderRepository>();
    var order = new Order(Guid.NewGuid());

    Mock.Arrange(() => repository.FindAsync(order.Id)).ReturnsAsync(order);

    var result = await new OrderProcessor(repository).ProcessAsync(order.Id);

    Assert.True(result.Succeeded);
    Mock.Assert(() => repository.FindAsync(order.Id), Occurs.Once());
}
```

Ordinary interface/virtual-member mocking like the example above works identically on JustMock Lite
and the commercial edition, with no profiler involved. The profiler only becomes relevant the
moment the mock target is static, sealed, or non-virtual — see
[references/elevated-mocking-setup.md](references/elevated-mocking-setup.md).

## Out of scope

- Other .NET mocking frameworks (Moq, NSubstitute, FakeItEasy) — not documented here; this skill is
  JustMock-only.
- JustMock's non-mocking Telerik tooling (Test Studio, Fiddler, UI component libraries) — a
  separate product family from JustMock itself.
- CI/build-server profiler installation beyond the environment-variable mechanics in
  [references/elevated-mocking-setup.md](references/elevated-mocking-setup.md) — a specific CI
  provider's agent configuration is outside this skill's scope.
