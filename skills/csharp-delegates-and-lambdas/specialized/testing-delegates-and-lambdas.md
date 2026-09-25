# Delegates and Lambdas as a Test-Authoring Tool

Delegates and lambdas aren't just something tests exercise incidentally — `Func<T, bool>`
predicates, `Action`-based setup/teardown hooks, and delegate-based fakes are themselves the tools a
test author reaches for to keep test code declarative and reusable. This file covers that direction:
using delegates and lambdas *to write tests*, not testing this skill's own delegate/lambda examples.

## Basic: `Func<T, bool>` as a reusable assertion predicate

```csharp
public static class OrderAssertions
{
    public static void ShouldSatisfy<T>(this T actual, Func<T, bool> predicate, string because = "")
    {
        Assert.True(predicate(actual), because);
    }
}

[Fact]
public void NewOrder_Test_IsPendingAndUnbilled()
{
    Order order = Order.CreateDraft();

    order.ShouldSatisfy(o => o.Status == OrderStatus.Pending, "a new order starts pending");
    order.ShouldSatisfy(o => o.Total == 0m, "a draft order has no charges yet");
}
```

A generic `Func<T, bool>`-based extension method reads like a fluent assertion without pulling in an
assertion library — `T` is inferred from `actual`, and the predicate itself documents exactly what's
being checked instead of a comparison the reader has to reverse-engineer from an expected value.

## Basic: `Action`-based setup/teardown composed per test

```csharp
public sealed class TempDirectoryFixture : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory().FullName;

    private readonly List<Action> _teardownActions = new();

    public void OnDispose(Action teardown) => _teardownActions.Add(teardown);

    public void Dispose()
    {
        foreach (Action teardown in _teardownActions)
        {
            teardown(); // each registered Action runs during cleanup, in registration order
        }
        Directory.Delete(Path, recursive: true);
    }
}

[Fact]
public void FileProcessor_Test_ArchivesInputFile()
{
    using var fixture = new TempDirectoryFixture();
    string inputPath = Path.Combine(fixture.Path, "input.csv");
    File.WriteAllText(inputPath, "id,name\n1,test");
    fixture.OnDispose(() => Console.WriteLine($"cleaned up {inputPath}"));

    FileProcessor.Archive(inputPath);

    Assert.True(File.Exists(Path.Combine(fixture.Path, "archive", "input.csv")));
}
```

Registering `Action` teardown steps as the test runs (rather than one fixed `Dispose` body) lets
each test contribute exactly the cleanup its own setup needs, in the order it made sense to add
them — the same multi-entry-delegate ordering guarantee covered in
[multicast-delegate-invocation-semantics.md](multicast-delegate-invocation-semantics.md), used
deliberately here instead of being a pitfall.

## Advanced: a delegate-based fake instead of a mocking library

```csharp
public interface IPaymentGateway
{
    bool TryCharge(decimal amount, out string transactionId);
}

public sealed class FakePaymentGateway : IPaymentGateway
{
    public Func<decimal, (bool Success, string TransactionId)> ChargeHandler { get; set; }
        = amount => (true, Guid.NewGuid().ToString());

    public bool TryCharge(decimal amount, out string transactionId)
    {
        (bool success, transactionId) = ChargeHandler(amount);
        return success;
    }
}

[Fact]
public void CheckoutService_Test_RollsBackOrderWhenChargeDeclined()
{
    var gateway = new FakePaymentGateway
    {
        ChargeHandler = amount => (Success: false, TransactionId: "")
    };
    var checkout = new CheckoutService(gateway);

    CheckoutResult result = checkout.Complete(new Order { Total = 50m });

    Assert.Equal(CheckoutStatus.RolledBack, result.Status);
}
```

A settable `Func<>` property on a hand-written fake gives each test its own behavior for exactly
the one interaction it cares about, without a mocking framework's setup/verify ceremony — the fake
class itself stays trivial and reusable across many tests, each supplying a different `ChargeHandler`
lambda.

## Advanced: callback verification via a capturing lambda

```csharp
[Fact]
public void NotificationService_Test_InvokesCallbackWithSentMessage()
{
    string? capturedMessage = null;
    var service = new NotificationService(onSent: message => capturedMessage = message);

    service.Notify("order shipped");

    Assert.Equal("order shipped", capturedMessage);
}
```

Capturing into a local from inside the callback lambda — the same closure mechanism covered in
[closures-and-variable-capture.md](closures-and-variable-capture.md) — is often a lighter-weight
substitute for a mocking library's call-verification API when the thing under test accepts a
delegate directly rather than an interface: no mock object needed, just a local variable the
lambda writes into and the test reads back afterward.

## Fallback

`Func<T, bool>` assertion helpers and delegate-based fakes work from C# 2.0-era generics and
anonymous methods onward — swap every lambda above for the `delegate(...) { ... }` form from
[references/csharp2-anonymous-methods-and-generic-delegates.md](../references/csharp2-anonymous-methods-and-generic-delegates.md)
on a pre-C#3.0 target; the underlying techniques (a generic predicate, a settable `Func<>` property,
capturing into a local) are unchanged.
