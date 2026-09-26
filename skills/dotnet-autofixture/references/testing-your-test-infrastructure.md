# Testing your test infrastructure

AutoFixture's own job is generating test data for *other* tests — a custom `ICustomization`, a
custom theory-attribute variant, or a hand-written recursion/behavior configuration you build on top
of it is itself code that can be wrong. This file covers verifying that code, not how to use
AutoFixture inside an ordinary application-code test (the rest of this skill's reference files cover
that; this file covers checking your own customizations and attributes actually do what they claim).

## Verify a customization actually applies its configuration

Test a custom `ICustomization` by applying it to a fresh `Fixture` and asserting on what it produces
— not by re-testing AutoFixture's own reflection-based generation, only the specific override the
customization adds:

```csharp
[Fact]
public void OrderCustomization_SetsNewOrdersToDraftStatus()
{
    var fixture = new Fixture().Customize(new OrderCustomization());

    var order = fixture.Create<Order>();

    Assert.Equal(OrderStatus.Draft, order.Status);
    Assert.Null(order.ShippedAt);
}
```

This catches the customization's most likely failure mode: applying `.With(...)`/`.Without(...)` to
the wrong member, or to a member expression that no longer compiles/matches after the target type's
shape changes.

## Verify a custom auto-mocking customization actually mocks

If a project wires its own auto-mocking customization (see `references/auto-mocking.md`) rather than
depending entirely on a ready-made integration package, verify both halves of what it claims: that
an interface resolves to a mock instead of throwing, and that a concrete type's interface-typed
constructor parameters are filled with mocks too:

```csharp
[Fact]
public void AutoMockCustomization_ResolvesInterfacesAsMocks()
{
    var fixture = new Fixture().Customize(new AutoMockCustomization());

    var repository = fixture.Create<IOrderRepository>();

    Assert.NotNull(repository);
    // Assert it's actually a mock via your mocking library's own type-check/API,
    // e.g. confirming it's a dynamically generated proxy, not a real implementation.
}

[Fact]
public void AutoMockCustomization_SuppliesMocksToConstructorDependencies()
{
    var fixture = new Fixture().Customize(new AutoMockCustomization());

    var sut = fixture.Create<OrderService>(); // constructor takes IOrderRepository, IClock

    Assert.NotNull(sut);
    // no exception means every interface-typed constructor parameter resolved successfully
}
```

## Verify a custom `[AutoData]`-style attribute supplies every parameter

For a hand-written theory-attribute variant (see `references/theory-attributes.md`), test its
`GetData` method directly against a representative test method signature rather than only
discovering problems indirectly through whatever test happens to use the attribute first:

```csharp
[Fact]
public void AutoDataAttribute_SuppliesAValueForEveryParameter()
{
    var attribute = new AutoDataAttribute();
    var method = typeof(SampleTests).GetMethod(nameof(SampleTests.MethodWithTwoParameters));

    var data = attribute.GetData(method).Single();

    Assert.Equal(2, data.Length);
    Assert.All(data, value => Assert.NotNull(value));
}
```

## What not to duplicate here

Don't use this file's patterns to re-verify that `Fixture.Create<T>()` itself performs correct
reflection-based construction, or that a mocking library correctly generates mocks — both are the
respective library's own correctness, not something this skill's guidance needs to re-prove. Scope
these tests to the customization, attribute, or behavior configuration code a project has actually
written on top of them.
