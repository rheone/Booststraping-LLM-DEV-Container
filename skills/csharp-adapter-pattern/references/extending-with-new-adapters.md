# Extending With New Adapters

Adding support for a new adaptee — a different legacy class, a second third-party library offering
the same kind of capability — should never require touching the target interface, an existing
adapter, or any consumer. The pattern is designed for exactly this kind of extension.

## Adding a new adapter

1. Confirm the target interface already expresses everything the new adaptee needs to satisfy. If
   it does, the new adaptee needs nothing more than a new adapter class implementing that interface
   — no change to `IPaymentProcessor`, `IObjectStorage`, or whichever target interface is in play.
2. Write the new adapter class, following the same shape as existing adapters: hold the new
   adaptee by reference (or by whatever construction the adaptee requires), implement each target
   member by translating to and from the adaptee's own shapes and error conventions.
3. Wire up construction of the new adapter wherever the application selects which implementation of
   the target interface to use, without changing how any consumer obtains or calls that interface.

```csharp
// Existing.
public sealed class LegacyBillingGatewayAdapter : IPaymentProcessor { /* ... */ }

// New adaptee, new adapter, target interface and every consumer unchanged.
public sealed class ThirdPartyPaymentApiAdapter : IPaymentProcessor
{
    private readonly ThirdPartyPaymentClient _client;

    public ThirdPartyPaymentApiAdapter(ThirdPartyPaymentClient client) => _client = client;

    public PaymentResult Charge(decimal amount, string currencyCode)
    {
        var response = _client.CreateCharge(amount, currencyCode);
        return new PaymentResult(response.Succeeded, response.Id);
    }
}
```

## When the target interface itself needs to grow

If the new adaptee exposes a capability the target interface doesn't yet have a member for, adding
that member is a breaking change to the interface — every existing implementation, including
existing adapters, now has to satisfy it. Two ways to avoid forcing every existing adapter to change
for a capability only the new adaptee supports:

- **Add a second, narrower target interface** for the new capability, rather than growing the
  existing one. A consumer that needs the new capability depends on both interfaces; a consumer
  that doesn't stays untouched, and existing adapters that don't support the new capability simply
  don't implement the second interface.
- **Give the new member a default implementation** (a C# 8.0+ default interface member) on the
  target interface if the capability has a sensible fallback behavior for adapters that don't
  support it natively — existing adapters compile unchanged and inherit the default, while the new
  adapter overrides it with a real implementation.

Reach for a brand-new target interface method with no default only when every existing adapter can
genuinely support it; otherwise you've turned "add a new adapter" into "modify every adapter that
already exists," which is the exact coupling the pattern exists to avoid.

## Testing the extension in isolation

A new adapter's tests exercise only that adapter's translation logic against its own adaptee —
existing adapters and consumers need no new tests as a result of the addition, because nothing
about their behavior changed. See [testing-adapters.md](testing-adapters.md) for the concrete
technique.
