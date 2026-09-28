# AutoFixture

AutoFixture is a third-party library that builds fully populated object graphs for tests via
reflection, so you stop hand-writing "Arrange" boilerplate for values a test doesn't actually care
about. This skill covers generating objects and anonymous primitives, customizing how a type gets
built, and wiring a mocking library's auto-mock behavior into a `Fixture`.

## When to reach for it

- Building an object graph for a test's Arrange step without hand-picking every constructor
  argument.
- The same generation rule for a type needs to apply consistently across many tests.
- A dependency is an interface or abstract class that `Create<T>()` can't construct on its own.
- Writing a parameterized test and wanting generated data instead of fully hand-written inline
  values.
- A test is flaky or passes for the wrong reason because it accidentally depends on an
  auto-generated value, or a circular reference throws during generation.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing test
"Arrange" code that would otherwise build object graphs by hand. You can also invoke it directly as
`/dotnet-autofixture`.

## What it covers

| Topic | Reference |
| --- | --- |
| Fixture, Create, CreateMany, Build().With()/.Without() | [references/core-concepts.md](references/core-concepts.md) |
| ICustomization, Fixture.Customize, FromFactory | [references/customizations.md](references/customizations.md) |
| Wiring a mocking library's auto-mock behavior into a Fixture | [references/auto-mocking.md](references/auto-mocking.md) |
| The AutoData/InlineAutoData-style data-attribute pattern | [references/theory-attributes.md](references/theory-attributes.md) |
| Flaky assertions and circular-reference handling | [references/pitfalls.md](references/pitfalls.md) |
| Verifying a custom fixture/customization behaves correctly | [references/testing-your-test-infrastructure.md](references/testing-your-test-infrastructure.md) |

## Example prompts

- "Generate a fully populated Order for this test, but pin the CustomerTier to Vip."
- "Write a customization that always generates valid email addresses for the Email property."
- "This test throws on a circular reference during generation: how do I handle that?"
