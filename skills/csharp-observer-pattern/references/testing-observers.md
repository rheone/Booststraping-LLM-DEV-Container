# Testing Code Built on Observer

Testing an Observer-based subject means asserting on two things: that subscribing and publishing
actually deliver values to the right observers, and that unsubscribing actually stops delivery. Both
are plain, deterministic assertions once you have a hand-written test observer to record what it
received.

## Testing an IObservable&lt;T&gt; subject

```csharp
public class PriceTickerTests
{
    [Fact]
    public void PublishPrice_DeliversValueToSubscribedObserver()
    {
        var ticker = new PriceTicker();
        var observer = new RecordingObserver<decimal>();
        using var subscription = ticker.Subscribe(observer);

        ticker.PublishPrice(101.50m);

        Assert.Equal(new[] { 101.50m }, observer.ReceivedValues);
    }

    [Fact]
    public void Dispose_StopsDeliveryToThatObserver()
    {
        var ticker = new PriceTicker();
        var observer = new RecordingObserver<decimal>();
        var subscription = ticker.Subscribe(observer);

        subscription.Dispose();
        ticker.PublishPrice(101.50m);

        Assert.Empty(observer.ReceivedValues);
    }

    [Fact]
    public void PublishPrice_DeliversToMultipleObserversIndependently()
    {
        var ticker = new PriceTicker();
        var first = new RecordingObserver<decimal>();
        var second = new RecordingObserver<decimal>();
        using var firstSubscription = ticker.Subscribe(first);
        using var secondSubscription = ticker.Subscribe(second);

        ticker.PublishPrice(101.50m);

        Assert.Equal(new[] { 101.50m }, first.ReceivedValues);
        Assert.Equal(new[] { 101.50m }, second.ReceivedValues);
    }

    private sealed class RecordingObserver<T> : IObserver<T>
    {
        public List<T> ReceivedValues { get; } = new();
        public Exception? ReceivedError { get; private set; }
        public bool Completed { get; private set; }

        public void OnNext(T value) => ReceivedValues.Add(value);
        public void OnError(Exception error) => ReceivedError = error;
        public void OnCompleted() => Completed = true;
    }
}
```

`RecordingObserver<T>` is a plain hand-written fake — testing Observer rarely needs a mocking
framework, since the assertion is almost always "what values arrived, in what order," which a list
captures directly.

## Testing terminal notifications

```csharp
[Fact]
public void Complete_CallsOnCompletedOnEverySubscribedObserver()
{
    var ticker = new PriceTicker();
    var observer = new RecordingObserver<decimal>();
    using var subscription = ticker.Subscribe(observer);

    ticker.Complete();

    Assert.True(observer.Completed);
}
```

Cover `OnError` the same way for a subject that can signal failure — construct the failure condition
that triggers it, and assert the recording observer's `ReceivedError` is the expected exception, not
just that *some* exception arrived.

## Testing an event-based subject

```csharp
public class EventBasedPriceTickerTests
{
    [Fact]
    public void PublishPrice_RaisesPriceChangedWithCorrectValue()
    {
        var ticker = new PriceTicker();
        PriceChangedEventArgs? received = null;
        ticker.PriceChanged += (sender, e) => received = e;

        ticker.PublishPrice(101.50m);

        Assert.NotNull(received);
        Assert.Equal(101.50m, received!.Price);
    }

    [Fact]
    public void PublishPrice_WithNoSubscribers_DoesNotThrow()
    {
        var ticker = new PriceTicker();

        var exception = Record.Exception(() => ticker.PublishPrice(101.50m));

        Assert.Null(exception);
    }
}
```

The no-subscribers case is worth its own test specifically — it's the case the
`?.Invoke`/null-conditional pattern exists to handle, and a subject that raises its event with a
plain non-null-checked call throws `NullReferenceException` exactly when no test happens to subscribe
first, which is easy to miss if every other test in the suite subscribes before publishing.

## Testing a weak-reference subject's collection behavior

Testing that an observer is actually eligible for collection once nothing else references it
requires forcing a collection and asserting the subject prunes the dead entry — this is one of the
few cases in observer testing worth an explicit `GC.Collect()`/`GC.WaitForPendingFinalizers()` call,
since the behavior under test is specifically about garbage collection interacting with the weak
reference, not something a hand-written fake can substitute for.

```csharp
[Fact]
public void Publish_PrunesObserverThatHasBeenCollected()
{
    var subject = new WeakEventSubject<decimal>();
    SubscribeAndDiscardObserver(subject); // observer goes out of scope with no other references

    GC.Collect();
    GC.WaitForPendingFinalizers();

    var exception = Record.Exception(() => subject.Publish(1m));
    Assert.Null(exception); // pruning the dead entry should not throw

    static void SubscribeAndDiscardObserver(WeakEventSubject<decimal> subject)
    {
        var observer = new RecordingObserver<decimal>();
        subject.Subscribe(observer);
    }
}
```

Keeping the subscribe call in a separate, non-inlined local function (`SubscribeAndDiscardObserver`)
matters here — if the observer variable stays reachable from a local in the test method itself, the
JIT or debugger may keep it alive longer than expected, and the collection this test depends on may
not actually happen.
