# Testing

JustMock is itself a testing tool, so "testing" here means structuring the tests you write *with*
JustMock so they stay reliable, fast, and clear about what they verify.

## Structuring a test around Arrange/Act/Assert

Keep the three phases visually and logically separate — it makes a failing test's cause obvious at
a glance (a wrong arrangement vs. wrong production logic vs. a missed interaction):

```csharp
[Fact]
public async Task ProcessPayment_WhenGatewayDeclines_ReturnsFailureResult()
{
    // Arrange
    var gateway = Mock.Create<IPaymentGateway>();
    Mock.Arrange(() => gateway.ChargeAsync(Arg.IsAny<decimal>())).ReturnsAsync(false);
    var processor = new PaymentProcessor(gateway);

    // Act
    var result = await processor.ProcessAsync(49.99m);

    // Assert
    Assert.False(result.Succeeded);
    Mock.Assert(() => gateway.ChargeAsync(49.99m), Occurs.Once());
}
```

## Testing a static/sealed dependency (requires Elevated Mocking)

Once the profiler is active for the process (see
[elevated-mocking-setup.md](elevated-mocking-setup.md)), a static class mocks the same way an
interface member does:

```csharp
public static class ExchangeRateProvider
{
    public static decimal GetRate(string currencyPair) => /* live lookup */ default;
}

[Fact]
public void ConvertAmount_UsesProvidedExchangeRate()
{
    Mock.Arrange(() => ExchangeRateProvider.GetRate("USD/EUR")).Returns(0.92m);

    decimal converted = CurrencyConverter.Convert(100m, "USD/EUR");

    Assert.Equal(92m, converted);
}
```

Static mocks arranged with `Mock.Arrange` apply process-wide for the duration of the test — verify
the test framework isolates or resets static mock state between tests (JustMock resets arrangements
between test methods when used through its standard test-adapter integration) so one test's static
arrangement can't leak into and silently affect another test's assertions.

## Testing an async dependency's failure path

Combine `ThrowsAsync` with the test framework's async-exception assertion to verify the system under
test translates a downstream failure correctly rather than letting it propagate unhandled or swallow
it silently:

```csharp
[Fact]
public async Task ProcessPayment_WhenGatewayTimesOut_ThrowsPaymentProcessingException()
{
    var gateway = Mock.Create<IPaymentGateway>();
    Mock.Arrange(() => gateway.ChargeAsync(Arg.IsAny<decimal>()))
        .ThrowsAsync(new TimeoutException("gateway timeout"));

    var processor = new PaymentProcessor(gateway);

    await Assert.ThrowsAsync<PaymentProcessingException>(() => processor.ProcessAsync(49.99m));
}
```

## Verifying call order across multiple dependencies

`Mock.Assert` alone verifies *that* a call happened; verifying call *order* across two or more mocks
needs `InOrder` on an assertion, or a manually tracked call log if the ordering constraint spans
different mock objects:

```csharp
Mock.Arrange(() => validator.Validate(Arg.IsAny<Order>())).Returns(true).InOrder();
Mock.Arrange(() => repository.Save(Arg.IsAny<Order>())).InOrder();

processor.Submit(order);

Mock.Assert(validator);
Mock.Assert(repository);
```

Reserve order verification for cases where order is an actual, documented behavioral requirement
(validate-then-save, not the reverse) — asserting incidental call order that isn't actually a
contract makes tests brittle against harmless refactors.

## Most likely scenarios

| Scenario | Approach |
| --- | --- |
| A handler/service depending on an interface | `Mock.Create<TInterface>()`, no profiler needed |
| A handler depending on a static utility class or a sealed third-party type | Elevated Mocking (profiler active), same Arrange/Act/Assert syntax |
| A handler awaiting an async dependency, testing the success path | `ReturnsAsync(value)` |
| A handler awaiting an async dependency, testing a failure/exception path | `ThrowsAsync(exception)` |
| Verifying the handler called a dependency with the right arguments, the right number of times | `Mock.Assert(() => dependency.Member(...), Occurs.Once())` |
