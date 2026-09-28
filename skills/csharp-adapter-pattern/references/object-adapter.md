# Object Adapter

The object adapter is the idiomatic C# form: the adapter holds a reference to an adaptee instance
and implements the target interface by delegating to that instance, translating shapes as it goes.
It uses composition, not inheritance, so it works regardless of whether the adaptee is sealed,
already inherits from something else, or comes from a library you can't extend.

## Shape

```csharp
public interface IPaymentProcessor
{
    PaymentResult Charge(decimal amount, string currencyCode);
}

public sealed class LegacyBillingGateway
{
    public LegacyChargeReceipt SubmitCharge(int amountInCents, string currency) =>
        new LegacyChargeReceipt(Approved: true, ReceiptId: "R-1");
}

public sealed record LegacyChargeReceipt(bool Approved, string ReceiptId);
public sealed record PaymentResult(bool Success, string ReferenceId);

public sealed class LegacyBillingGatewayAdapter : IPaymentProcessor
{
    private readonly LegacyBillingGateway _gateway;

    public LegacyBillingGatewayAdapter(LegacyBillingGateway gateway) => _gateway = gateway;

    public PaymentResult Charge(decimal amount, string currencyCode)
    {
        var cents = (int)(amount * 100);
        LegacyChargeReceipt receipt = _gateway.SubmitCharge(cents, currencyCode);
        return new PaymentResult(receipt.Approved, receipt.ReceiptId);
    }
}
```

Three translations happen inside `Charge`, all invisible to the consumer: the money type changes
from `decimal` dollars to `int` cents, the method name changes from `SubmitCharge` to `Charge`, and
the return shape changes from `LegacyChargeReceipt` to `PaymentResult`.

## Constructing the adapter

Pass the adaptee instance through the adapter's constructor rather than constructing it inside the
adapter. This keeps the adapter itself easy to construct with a fake or stub adaptee, and keeps
adaptee lifetime and configuration (connection strings, credentials, timeouts) out of the adapter's
own concerns — the adapter's only job is translation.

```csharp
IPaymentProcessor processor = new LegacyBillingGatewayAdapter(new LegacyBillingGateway());
```

## Adapting more than one adaptee behind one target

An object adapter can hold references to several adaptees and combine them to satisfy one target
member, when the target's operation doesn't map to a single adaptee call:

```csharp
public sealed class CompositeShippingAdapter : IShippingQuote
{
    private readonly RateLookupClient _rates;
    private readonly CarrierAvailabilityClient _availability;

    public CompositeShippingAdapter(RateLookupClient rates, CarrierAvailabilityClient availability)
    {
        _rates = rates;
        _availability = availability;
    }

    public ShippingQuoteResult GetQuote(string postalCode)
    {
        var rate = _rates.LookupRate(postalCode);
        var available = _availability.IsServiceable(postalCode);
        return new ShippingQuoteResult(rate, available);
    }
}
```

## Translating errors, not just shapes

An adaptee often signals failure in a way that doesn't match the target's convention — a legacy
integer return code, a boolean flag, or an exception type specific to the adaptee's library. Do the
error translation inside the adapter too, so the consumer only ever handles the target's own error
convention:

```csharp
public PaymentResult Charge(decimal amount, string currencyCode)
{
    try
    {
        var cents = (int)(amount * 100);
        LegacyChargeReceipt receipt = _gateway.SubmitCharge(cents, currencyCode);
        return receipt.Approved
            ? new PaymentResult(Success: true, receipt.ReceiptId)
            : new PaymentResult(Success: false, ReferenceId: null!);
    }
    catch (LegacyGatewayTimeoutException ex)
    {
        throw new PaymentProcessingException("Payment gateway timed out.", ex);
    }
}
```

A consumer coded against `IPaymentProcessor` never needs to know `LegacyGatewayTimeoutException`
exists.
