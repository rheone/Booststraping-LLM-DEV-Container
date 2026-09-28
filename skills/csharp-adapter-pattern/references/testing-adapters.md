# Testing Adapters

The Adapter pattern splits testing into two distinct concerns: testing a consumer that depends on
the target interface, and testing an adapter's own translation logic. Keep these separate — a
consumer's tests should never need a real adaptee.

## Testing a consumer that depends on the target interface

Because the consumer depends on the target interface, not on the adapter or the adaptee, you give
it a fake or a test double of the target interface directly. No adapter needs to exist in the test
at all.

```csharp
public sealed class FakePaymentProcessor : IPaymentProcessor
{
    public PaymentResult? LastResultToReturn { get; set; }
    public (decimal Amount, string CurrencyCode)? LastCall { get; private set; }

    public PaymentResult Charge(decimal amount, string currencyCode)
    {
        LastCall = (amount, currencyCode);
        return LastResultToReturn ?? new PaymentResult(Success: true, ReferenceId: "test-ref");
    }
}

[Fact]
public void CheckoutService_charges_the_order_total()
{
    var fakeProcessor = new FakePaymentProcessor();
    var checkout = new CheckoutService(fakeProcessor);

    checkout.CompleteOrder(orderTotal: 42.50m, currencyCode: "USD");

    Assert.Equal((42.50m, "USD"), fakeProcessor.LastCall);
}
```

This is the direct payoff of the pattern's narrow target interface: the fake implements only the
handful of members the consumer actually calls, never the adaptee's full surface.

## Testing an adapter's own translation logic

An adapter's tests are unit tests of pure translation: given a known input and a known adaptee
response, does the adapter produce the correct target-shaped output, and does it translate error
conditions correctly? Replace the adaptee with a fake or a minimal stub that returns fixed
responses — you are testing the adapter's mapping code, not the adaptee's real behavior.

```csharp
public sealed class StubLegacyBillingGateway : LegacyBillingGateway
{
    public LegacyChargeReceipt ReceiptToReturn { get; set; } = new(Approved: true, ReceiptId: "R-1");
    public override LegacyChargeReceipt SubmitCharge(int amountInCents, string currency) =>
        ReceiptToReturn;
}

[Fact]
public void Adapter_converts_dollars_to_cents_before_calling_the_gateway()
{
    var stubGateway = new StubLegacyBillingGateway();
    var adapter = new LegacyBillingGatewayAdapter(stubGateway);

    var result = adapter.Charge(amount: 19.99m, currencyCode: "USD");

    Assert.True(result.Success);
    Assert.Equal("R-1", result.ReferenceId);
}
```

When the adaptee is sealed or otherwise can't be stubbed by subclassing, extract a small interface
around only the adaptee members the adapter calls, and have the real adaptee implement it alongside
its existing API — this is a test-only seam, not a second target interface, and it exists purely so
the adapter's constructor can accept a fake in tests while taking the real adaptee type in
production.

## Error-path coverage

Because translating an adaptee's failure signal into the target's error convention is exactly the
kind of logic that's easy to get subtly wrong (an off-by-one on a status code, a swallowed
exception), give the adapter's error-translation paths the same test coverage as its success path:
one test per distinct failure mode the adaptee can report, asserting the target interface's
corresponding failure shape or exception type comes out.

## What doesn't need a test here

Don't write a test that only re-asserts a straight pass-through with no translation (a method that
calls the adaptee with the same arguments and returns its result unchanged) — there's no logic
there to break. Reserve adapter tests for members that actually transform shapes, combine multiple
adaptee calls, or translate errors.
