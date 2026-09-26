# Adding a New Observer Without Breaking Existing Code

Observer's extension story is one-directional and simple: a subject that already supports multiple
subscribers accepts a new one exactly the way it accepted every previous one, with no change to the
subject's class or to any other already-subscribed observer.

## Adding a new IObserver&lt;T&gt; implementation

```csharp
public sealed class PriceAlertObserver : IObserver<decimal>
{
    private readonly decimal _threshold;
    private readonly IAlertService _alerts;

    public PriceAlertObserver(decimal threshold, IAlertService alerts)
    {
        _threshold = threshold;
        _alerts = alerts;
    }

    public void OnNext(decimal value)
    {
        if (value >= _threshold)
        {
            _alerts.Raise($"Price crossed {_threshold}: {value}");
        }
    }

    public void OnError(Exception error) => _alerts.Raise($"Price feed failed: {error.Message}");
    public void OnCompleted() { /* nothing to do */ }
}
```

```csharp
using var subscription = ticker.Subscribe(new PriceAlertObserver(threshold: 100m, alerts));
```

`PriceTicker` needs no change — its `Subscribe` method already accepts any `IObserver<decimal>`, and
its internal list already supports an arbitrary number of subscribers. `PriceLogger`, or any other
existing observer already subscribed, is entirely unaffected: each observer only ever sees its own
`OnNext`/`OnError`/`OnCompleted` calls, never anything about sibling observers.

## Adding a new event subscriber

Subscribing a new handler to an existing event needs no change to the type declaring the event —
`+=` on an existing public event is available to any code that can see it, and each subscriber is
independent:

```csharp
ticker.PriceChanged += (sender, e) => alertService.CheckThreshold(e.Price);
```

## Adding a genuinely new notification (not just a new subscriber)

Adding a *new kind* of notification — not a new subscriber to an existing one, but a new event or
observable stream a subject didn't previously expose — does require a change to the subject, since
the subject is the one declaring what it can notify about:

```csharp
public sealed class PriceTicker : IObservable<decimal>
{
    // existing price notification unchanged

    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged; // new notification

    public void PublishVolume(long volume) =>
        VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(volume));
}
```

This is additive to the subject's public surface — every existing subscriber to `PriceChanged` or
the `IObservable<decimal>` stream is unaffected, since neither their subscription nor their delivery
depends on whether `VolumeChanged` exists. This is the sense in which Observer's extension story
stays cheap even when growing the subject: a new notification is a new, independent event or
observable member, never a change to an existing one's signature or delivery order.

## What would break existing observers

- **Changing the type parameter of an existing `IObservable<T>`** (or the `EventArgs` type of an
  existing event) is a breaking change to every existing observer/handler, since their `OnNext`/
  handler signature no longer matches. Add a new, separately-typed notification instead of
  repurposing an existing one for a different payload shape.
- **Changing delivery order or synchronity** (switching a subject from synchronous, in-line delivery
  to asynchronous/background delivery) can break an observer that assumed its `OnNext` runs on the
  same thread or before the publishing call returns — treat this as a breaking change to the
  subject's contract, not a transparent implementation detail, and document the delivery guarantee
  the subject makes so both existing and future observers can rely on it.
