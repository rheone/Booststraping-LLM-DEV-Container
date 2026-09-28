# Theory-attribute pattern for parameterized tests

## The problem: hand-writing inline data loses AutoFixture's benefit

A parameterized test method that takes explicit inline values for every parameter
(`[Theory, InlineData(1, "a")]`-style, generically speaking) is back to hand-writing the exact
"Arrange" boilerplate AutoFixture exists to remove, the moment a parameter is a complex object
rather than a primitive literal a testing framework's inline-data attribute can express directly.

## The general pattern: a data attribute backed by a `Fixture`

The standard integration is a custom parameterized-test data-source attribute that, instead of (or
in addition to) literal inline values, asks a `Fixture` to auto-generate a value for each test
method parameter it doesn't otherwise supply:

```csharp
public class AutoDataAttribute : DataAttribute // base type from your test framework's own API
{
    private readonly Func<IFixture> _fixtureFactory;

    public AutoDataAttribute() : this(() => new Fixture()) { }

    protected AutoDataAttribute(Func<IFixture> fixtureFactory) => _fixtureFactory = fixtureFactory;

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var fixture = _fixtureFactory();
        var parameters = testMethod.GetParameters();
        yield return parameters.Select(p => fixture.Create(p.ParameterType)).ToArray();
    }
}
```

Applied to a test method, every parameter gets an auto-generated value with no inline data at all:

```csharp
[Theory, AutoData]
public void Total_SumsLineItems(Order order, decimal expectedDiscount)
{
    // "order" is a fully populated, auto-generated Order.
    // "expectedDiscount" is an auto-generated decimal.
}
```

## Mixing explicit and generated values: the `InlineAutoData` shape

A second attribute variant accepts explicit leading values (like an ordinary inline-data attribute)
and auto-generates whichever trailing parameters aren't covered by them — the pattern most tests
actually reach for, since a test usually wants to pin one or two meaningful values while leaving the
rest arbitrary:

```csharp
[Theory]
[InlineAutoData(0)]     // quantity = 0 is pinned; every other parameter is auto-generated
[InlineAutoData(-1)]    // quantity = -1 is pinned; every other parameter is auto-generated
public void Total_ThrowsForNonPositiveQuantity(int quantity, Order order)
{
    Assert.Throws<ArgumentOutOfRangeException>(() => order.CalculateTotal(quantity));
}
```

This turns what would otherwise be several near-duplicate test methods (one per edge case, each
hand-building an `Order`) into one parameterized method run once per pinned edge case, with the
non-edge-case parameters supplied automatically.

## Frozen/customized parameters via parameter attributes

A per-parameter attribute (conventionally named along the lines of `Frozen`) marks a specific
parameter so its generated value is also registered as what `Fixture.Freeze<T>()` would return for
that type for the rest of that test's object graph — useful for the auto-mocking pattern in
`references/auto-mocking.md`, where a test wants direct access to the same mock instance the system
under test's constructor received:

```csharp
[Theory, AutoData]
public void Place_SavesOrder([Frozen] IOrderRepository repository, OrderService sut, Order order)
{
    sut.Place(order);
    // repository is the same instance sut's constructor received
}
```

## Where this fits relative to a specific test framework

The attribute base type (`DataAttribute` above stands in for whatever a given test framework
actually exposes), its discovery mechanism, and its exact extensibility points depend on which test
framework a project uses. This file describes the shape of the pattern — a data-source attribute
backed by a fixture, an explicit-plus-generated variant, and a per-parameter freeze marker — rather
than a specific test framework's exact attribute names or base classes; confirm those against the
test framework actually in use before wiring the pattern in.
