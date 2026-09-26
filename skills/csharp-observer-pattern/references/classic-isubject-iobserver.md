# Classic Subject/Observer with IObservable&lt;T&gt;/IObserver&lt;T&gt;

The Observer pattern lets a subject notify a variable number of interested parties about changes
without knowing anything about who they are — the subject holds a collection of observers behind a
shared interface and calls into each one when something happens. The .NET base class library
standardizes this exact shape as `IObservable<T>` and `IObserver<T>`.

## The BCL interfaces

```csharp
public interface IObserver<in T>
{
    void OnNext(T value);
    void OnError(Exception error);
    void OnCompleted();
}

public interface IObservable<out T>
{
    IDisposable Subscribe(IObserver<T> observer);
}
```

`OnNext` delivers a value, `OnError` signals a terminal failure, and `OnCompleted` signals a
terminal, successful end of the sequence — a well-behaved subject calls at most one of `OnError` or
`OnCompleted`, and never calls `OnNext` again after either.

## Implementing a subject

```csharp
public sealed class PriceTicker : IObservable<decimal>
{
    private readonly List<IObserver<decimal>> _observers = new();

    public IDisposable Subscribe(IObserver<decimal> observer)
    {
        _observers.Add(observer);
        return new Unsubscriber(_observers, observer);
    }

    public void PublishPrice(decimal price)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnNext(price);
        }
    }

    public void Complete()
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnCompleted();
        }
        _observers.Clear();
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<IObserver<decimal>> _observers;
        private readonly IObserver<decimal> _observer;

        public Unsubscriber(List<IObserver<decimal>> observers, IObserver<decimal> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose() => _observers.Remove(_observer);
    }
}
```

`_observers.ToArray()` before iterating protects against an observer that unsubscribes (or a new
one that subscribes) from within its own `OnNext` callback, which would otherwise mutate
`_observers` while the `foreach` is iterating it and throw `InvalidOperationException`.

## Implementing an observer

```csharp
public sealed class PriceLogger : IObserver<decimal>
{
    private readonly ILogger _logger;

    public PriceLogger(ILogger logger) => _logger = logger;

    public void OnNext(decimal value) => _logger.LogInformation("Price: {Price}", value);
    public void OnError(Exception error) => _logger.LogError(error, "Price feed failed");
    public void OnCompleted() => _logger.LogInformation("Price feed ended");
}
```

```csharp
var ticker = new PriceTicker();
using var subscription = ticker.Subscribe(new PriceLogger(logger));

ticker.PublishPrice(101.50m);
ticker.PublishPrice(102.75m);
```

## Why `Subscribe` returns `IDisposable`

The subscription's lifetime is explicit and caller-controlled: disposing the returned handle
unsubscribes, and the subject never needs a separate `Unsubscribe` method with its own signature to
match. This is the mechanism this interface pair uses for the lifetime/disposal tradeoff discussed
in [events-vs-iobservable.md](events-vs-iobservable.md) — a C# event has no equivalent built-in
handle, so unsubscribing from an event requires the subscriber to keep its own reference to the
exact delegate it subscribed with.
