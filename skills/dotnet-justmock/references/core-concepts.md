# Core Concepts

JustMock's API centers on `Mock.Create<T>()` to produce a mock instance and a fluent
Arrange/Act/Assert vocabulary layered directly onto ordinary C# lambda expressions targeting the
mocked member.

## Creating a mock

```csharp
using Telerik.JustMock;

public interface IPaymentGateway
{
    bool Charge(decimal amount);
}

var gateway = Mock.Create<IPaymentGateway>();
```

`Mock.Create<T>()` works against interfaces, and against classes with virtual/abstract members
(non-virtual members on a class target require Elevated Mocking — see
[elevated-mocking-setup.md](elevated-mocking-setup.md)).

## Arrange: defining behavior

```csharp
Mock.Arrange(() => gateway.Charge(Arg.IsAny<decimal>())).Returns(true);
```

- `Mock.Arrange` takes a lambda invoking the member being configured — the lambda expression itself
  is never actually executed against a real implementation; JustMock intercepts the call.
- `Arg.IsAny<T>()`, `Arg.Matches<T>(predicate)`, and literal argument values are all valid inside the
  arranged lambda's argument list, following the same matcher vocabulary most .NET mocking
  frameworks use.
- `.Returns(value)` sets a fixed return value; `.Returns(() => computedValue)` (a delegate) computes
  it lazily per call, useful when the return value depends on call count or external state.
- `.Throws<TException>()` / `.Throws(new TException(...))` arranges the member to throw instead of
  return.
- `.OccursOnce()`, `.OccursNever()`, `.Occurs(n)` can be chained directly onto an arrangement to
  assert call-count expectations without a separate `Mock.Assert` call, though the separate
  Assert-phase form (below) is more common for readability in the Arrange/Act/Assert structure.

## Act: exercising the system under test

The "Act" phase is ordinary code — call the method under test, passing the mock in via constructor
injection, a setter, or however the production code receives its dependency:

```csharp
var processor = new PaymentProcessor(gateway);
bool result = processor.ProcessPayment(49.99m);
```

## Assert: verifying interaction

```csharp
Mock.Assert(() => gateway.Charge(49.99m), Occurs.Once());
```

- `Mock.Assert` verifies that a call matching the given lambda happened the specified number of
  times (`Occurs.Once()`, `Occurs.Never()`, `Occurs.AtLeast(n)`, `Occurs.Exactly(n)`).
- Reserve `Mock.Assert` for interaction verification (did the code call the dependency the right way)
  — verifying the *outcome* of the Act phase (`result` above) uses the test framework's own
  assertion library (`Assert.True`, FluentAssertions, etc.), not JustMock.

## Arranging a property

```csharp
Mock.Arrange(() => gateway.IsConnected).Returns(true);
```

Properties arrange the same way as methods — target the property access expression directly rather
than a getter/setter method name.

## Arranging a sequence of return values

```csharp
Mock.Arrange(() => gateway.Charge(Arg.IsAny<decimal>()))
    .Returns(true)
    .InSequence();

Mock.Arrange(() => gateway.Charge(Arg.IsAny<decimal>()))
    .Returns(false)
    .InSequence();
```

`.InSequence()` on successive arrangements for the same member/argument pattern makes each
successive call to the mock consume the next arrangement in order — the first call returns `true`,
the second returns `false` — useful for testing retry logic against a dependency that fails then
succeeds (or vice versa).

## Common pitfall

An un-arranged member on a mock returns the type's default (`null`, `0`, `false`, or an empty
collection where JustMock can infer one) rather than throwing — a test that forgets to arrange a
dependency the system under test actually calls fails downstream with a confusing null-reference or
wrong-value symptom instead of an obvious "unexpected call" error. When a test's behavior looks
wrong in a way that traces back to a dependency, check first whether the call was arranged at all.
