---
name: dotnet-autofixture
description: Guidance on AutoFixture, a third-party library that generates test data automatically via reflection-based object construction (current stable release 4.18.1). Covers Fixture.Create<T>()/CreateMany<T>() and Build<T>() for anonymous object graphs, customizations (ICustomization, Fixture.Customize<T>, FromFactory), the general pattern for wiring a mocking library's auto-mock behavior into a Fixture (MockRelay-style customizations, Freeze<T>()), the [AutoData]/[InlineAutoData]-style theory-attribute pattern for parameterized tests (described generically as a data-attribute shape, not tied to a specific test framework), and common pitfalls (assertions that accidentally depend on arbitrary generated values, circular reference handling via ThrowingRecursionBehavior/OmitOnRecursionBehavior). Use when writing or reviewing test "Arrange" code that builds object graphs by hand, when a test needs anonymous data instead of hand-picked literals, or when deciding whether a value should be pinned explicitly versus auto-generated.
license: MIT
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# AutoFixture

Guidance on AutoFixture, a third-party library that generates test data automatically via
reflection-based object construction. Current stable release as of this writing: **4.18.1**.
Organized by concern/topic, not by version — each reference file notes a version-introduced fact
inline where relevant.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Generating an object graph or anonymous primitive for a test | `Fixture`, `Create<T>`, `CreateMany<T>`, `Build<T>().With(...)`/`.Without(...)` | [references/core-concepts.md](references/core-concepts.md) |
| Applying the same generation rule to a type across many tests | `ICustomization`, `Fixture.Customize<T>`, `FromFactory` | [references/customizations.md](references/customizations.md) |
| A dependency is an interface/abstract class `Create<T>()` can't construct | The general auto-mocking customization pattern, `Freeze<T>()` | [references/auto-mocking.md](references/auto-mocking.md) |
| Writing a parameterized test without hand-written inline data | The `[AutoData]`/`[InlineAutoData]`-style data-attribute pattern, frozen parameters | [references/theory-attributes.md](references/theory-attributes.md) |
| A test is flaky, passes for the wrong reason, or throws on a circular type | Pinning values a test depends on, `ThrowingRecursionBehavior`/`OmitOnRecursionBehavior` | [references/pitfalls.md](references/pitfalls.md) |
| Verifying a custom customization, auto-mock wiring, or data attribute you wrote | Smoke-testing your own extension code, not application code | [references/testing-your-test-infrastructure.md](references/testing-your-test-infrastructure.md) |

## Quick start

```csharp
var fixture = new Fixture();

var order = fixture.Create<Order>();               // fully populated, arbitrary but valid
var orders = fixture.CreateMany<Order>(3);          // three of them

var vipOrder = fixture.Build<Order>()
    .With(o => o.CustomerTier, CustomerTier.Vip)
    .Create();                                      // one member pinned, the rest generated
```

## Out of scope

- Any specific mocking library's own API (setup syntax, verification calls) — this skill covers
  only the general shape of wiring a mocking library's auto-mock behavior into a `Fixture`, not a
  specific library's configuration surface.
- Any specific test framework's theory/parameterized-test infrastructure (its base attribute types,
  discovery mechanism) — this skill covers the data-attribute pattern generically; confirm exact
  base types and extensibility points against whichever test framework a project uses.
- Performance tuning of generation for very large or very deep object graphs beyond the recursion
  behaviors covered in `references/pitfalls.md`.
