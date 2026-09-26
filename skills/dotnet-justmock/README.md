# Telerik JustMock

Telerik JustMock is a .NET mocking framework that can mock ordinary interfaces and virtual members
like any other mocking library, and (through a profiler-based "Elevated Mocking" mode) can also
mock static members, sealed classes, and non-virtual members that other frameworks can't touch. This
skill covers the `Mock.Create`/Arrange-Act-Assert API, enabling elevated mocking when a target needs
it, and mocking async methods.

> [!NOTE]
> JustMock carries a non-standard license. Research current edition, trial, and pricing terms
> independently before adopting it for a project.

## When to reach for it

- Writing an ordinary JustMock test with `Mock.Create<T>()` and Arrange/Act/Assert.
- A mock target is a static class, a sealed class, or a non-virtual member, and you need to decide
  whether elevated mocking is required and how to enable it.
- Mocking a `Task`/`Task<T>`-returning method and reasoning about how `ConfigureAwait` behaves in a
  mocked async chain.
- Deciding whether a given mocking need is even possible on JustMock Lite, or requires the
  commercial edition's profiler.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing or
reviewing JustMock-based unit tests. You can also invoke it directly as `/dotnet-justmock`.

## What it covers

| Topic | Reference |
| --- | --- |
| Mock.Create, Arrange, Act, Mock.Assert, Arg.IsAny | [references/core-concepts.md](references/core-concepts.md) |
| Enabling and configuring profiler-based Elevated Mocking | [references/elevated-mocking-setup.md](references/elevated-mocking-setup.md) |
| ReturnsAsync/ThrowsAsync and ConfigureAwait in mocked chains | [references/mocking-async-methods.md](references/mocking-async-methods.md) |
| Structuring tests around JustMock-mocked dependencies | [references/testing.md](references/testing.md) |

## Example prompts

- "Write a JustMock test that mocks IOrderRepository.FindAsync and asserts it was called once."
- "I need to mock a static method on a legacy class: does that require elevated mocking?"
- "Mock this async method so it throws, and verify the caller handles the exception correctly."
