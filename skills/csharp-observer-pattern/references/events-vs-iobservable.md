# Events vs. IObservable&lt;T&gt;: The Tradeoffs

Both C# events and the `IObservable<T>`/`IObserver<T>` interface pair implement the Observer
pattern; choosing between them is a matter of subscription lifetime control and composability, not
of one being a strictly better version of the other.

## Subscription lifetime and disposal

An event subscription has no first-class handle — unsubscribing means calling `-=` with the exact
delegate instance used to subscribe, which the subscriber must have kept a reference to. There is no
way for a third party (something other than the original subscriber) to end that subscription, and
no way to express "this subscription is scoped to this object's lifetime" beyond manually calling
`-=` in a `Dispose` method or finalizer-equivalent cleanup path.

```csharp
public sealed class PriceLogger : IDisposable
{
    private readonly PriceTicker _ticker;
    private readonly EventHandler<PriceChangedEventArgs> _handler;

    public PriceLogger(PriceTicker ticker)
    {
        _ticker = ticker;
        _handler = (sender, e) => Console.WriteLine(e.Price);
        _ticker.PriceChanged += _handler;
    }

    public void Dispose() => _ticker.PriceChanged -= _handler;
}
```

`IObservable<T>.Subscribe` returns the subscription itself as an `IDisposable` — the caller
disposes exactly that handle to unsubscribe, with no need to keep a separate delegate reference
around or write a matching `-=` call:

```csharp
using var subscription = ticker.Subscribe(new PriceLogger(logger));
// disposing `subscription` unsubscribes — no delegate reference to manage separately
```

This makes `IObservable<T>` a more natural fit wherever subscription lifetime needs to compose with
other `IDisposable`-based lifetime management already in the codebase — a `using` block, a
`CompositeDisposable`-style aggregate, or a DI container's own disposal of scoped services.

## Composability

An event is a single notification source with no built-in way to combine it with another, filter
which raised values a subscriber actually receives, or transform the value before a subscriber sees
it — any of that has to be written by hand inside the subscriber's callback.

`IObservable<T>` is designed as a first-class value representing a stream of future values, which
supports operators that transform, filter, and combine streams without the subscriber's callback
needing to do that work itself — `Where`, `Select`, `Merge`, and similar sequence operators apply to
an `IObservable<T>` the same way LINQ's own operators apply to `IEnumerable<T>`, producing a new
`IObservable<T>` rather than requiring a hand-written filter inside every subscriber.

## Simplicity and familiarity

An event needs no interface implementation, no custom subject class beyond a field and an `Invoke`
call, and every C# developer already knows `+=`/`-=`. `IObservable<T>`/`IObserver<T>` needs an
explicit `Subscribe` implementation, an `IDisposable` unsubscribe handle, and — to get the
composability benefit at all — either a hand-rolled `IObservable<T>` operator implementation or a
reactive-extensions-style library providing them, which is a much larger investment than a plain
event for a type that only ever needs to raise one simple notification.

## Choosing between them

- **One type, one simple notification, callback-style subscribers with no need to compose or
  transform the stream** → an event. This covers the large majority of "notify me when X happens"
  cases in ordinary application code.
- **Subscription needs to compose with other `IDisposable`-scoped lifetimes**, or a third party
  (not the original subscriber) needs to be able to end a subscription it was handed → `IObservable<T>`.
- **The notification is genuinely a stream of values that benefits from being filtered, transformed,
  or combined with other streams before a subscriber ever sees it** → `IObservable<T>`, since that
  composability is the interface pair's actual reason to exist over a plain event.

Nothing prevents a type from exposing both — a plain event for simple subscribers and an
`IObservable<T>` wrapper (or vice versa, implementing one atop the other) when both audiences exist,
though most types only need to pick the one shape their actual subscribers need.
