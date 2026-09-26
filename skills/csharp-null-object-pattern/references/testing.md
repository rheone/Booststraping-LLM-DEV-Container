# Testing With and Around Null Objects

## How to test it

A null object is one of the simplest test doubles available, precisely because it's designed to do
nothing — that makes it useful in two distinct testing roles: as production code under test, and as
a stand-in dependency for testing something else.

- **Testing the null object itself is minimal by design.** Its entire contract is "calling any
  member has no observable effect and never throws" — a test that calls each member and asserts
  nothing blew up (and, for a member with a return value, that it returns the documented neutral
  value) is complete. There's no branching logic inside a proper null object to cover more deeply
  than that; a null object with enough internal logic to need more thorough testing has likely
  stopped being a null object and started being a real, alternate implementation.
- **Use the null object as a trivial dependency fake in tests of other code.** Any test that needs
  an `INotifier` but doesn't care what happens to notifications can pass `NullNotifier.Instance`
  instead of constructing a mock — it's already exactly what a "don't care, just don't throw" fake
  would look like, with no mocking framework required.
- **Test the fallback logic where the null object gets chosen.** The one place null-object-related
  behavior is worth asserting on directly is the constructor or factory that decides between a real
  implementation and the null object — verify both branches (a real dependency passed through
  unchanged; a null dependency resulting in the null object) rather than assuming the `??` operator
  is self-evidently correct.
- **A stateful null object (see [singleton-vs-per-call-instances.md](singleton-vs-per-call-instances.md))
  doubles as a spy.** A `RecordingNullNotifier` that records what it was called with lets a test
  assert on *what* would have been communicated, without a full mocking framework.

## Most likely scenarios

**1. Verifying the fallback logic picks the null object when nothing is configured**

```csharp
[Fact]
public void OrderService_WithNoNotifier_FallsBackToNullNotifier()
{
    var service = new OrderService(notifier: null);

    // no exception on any code path that calls _notifier.Notify(...)
    var exception = Record.Exception(() => service.PlaceOrder(new Order(id: 1)));

    Assert.Null(exception);
}
```

**2. Using the null object as a throwaway dependency in an unrelated test**

```csharp
[Fact]
public void PlaceOrder_WithSufficientStock_ReducesInventory()
{
    var service = new OrderService(NullNotifier.Instance, new FakeInventory(stock: 5));

    service.PlaceOrder(new Order(id: 1, quantity: 2));

    // the test only cares about inventory, so a real INotifier assertion would be noise here
    Assert.Equal(3, service.RemainingStock);
}
```

**3. Using a recording null object as a spy to assert on communicated content**

```csharp
[Fact]
public void PlaceOrder_NotifiesWithTheOrderId()
{
    var notifier = new RecordingNullNotifier();
    var service = new OrderService(notifier);

    service.PlaceOrder(new Order(id: 42));

    Assert.Contains("42", Assert.Single(notifier.Messages));
}
```
