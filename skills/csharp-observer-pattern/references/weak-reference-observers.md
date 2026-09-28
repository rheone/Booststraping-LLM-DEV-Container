# Avoiding Memory Leaks with Weak-Reference Observers

A subject holding a strong reference to every subscribed observer — whether through an event's
invocation list or a hand-rolled `List<IObserver<T>>` — keeps every one of those observers alive for
as long as the subject itself is alive. When the subject outlives individual observers that were
meant to be shorter-lived (a long-lived application-level service notifying short-lived UI elements
or per-request objects that forget to unsubscribe), this becomes a memory leak: the observer, and
everything it references, stays reachable through the subject long after it should have been
collected.

## Why this happens with events specifically

```csharp
public sealed class AppWidePriceTicker
{
    public event EventHandler<PriceChangedEventArgs>? PriceChanged;
    // ...
}
```

```csharp
public sealed class ShortLivedPriceWidget
{
    public ShortLivedPriceWidget(AppWidePriceTicker ticker)
    {
        ticker.PriceChanged += OnPriceChanged; // subscribes
        // if this widget is discarded without ever calling -=, it leaks
    }

    private void OnPriceChanged(object? sender, PriceChangedEventArgs e) { /* ... */ }
}
```

If `AppWidePriceTicker` lives for the application's whole lifetime and `ShortLivedPriceWidget`
instances come and go (one per screen shown, one per request handled) without every single one
reliably calling `-= OnPriceChanged` before being discarded, each abandoned widget stays alive
through the ticker's invocation list indefinitely. This is the standard "lapsed listener" leak.

## The disciplined fix: always unsubscribe

The direct fix needs no weak references at all: give the short-lived subscriber a deterministic
cleanup path (`IDisposable`, a lifecycle hook the hosting framework guarantees to call) and always
unsubscribe there.

```csharp
public sealed class ShortLivedPriceWidget : IDisposable
{
    private readonly AppWidePriceTicker _ticker;

    public ShortLivedPriceWidget(AppWidePriceTicker ticker)
    {
        _ticker = ticker;
        _ticker.PriceChanged += OnPriceChanged;
    }

    private void OnPriceChanged(object? sender, PriceChangedEventArgs e) { /* ... */ }

    public void Dispose() => _ticker.PriceChanged -= OnPriceChanged;
}
```

Prefer this over a weak-reference approach whenever the subscriber's lifetime is something the
codebase can actually guarantee cleanup for — it is simpler, has no extra runtime cost, and has no
failure mode beyond "someone forgets to call `Dispose`," which is the same discipline every other
`IDisposable` consumer in the codebase already needs.

## When disciplined unsubscription isn't reliable

Some subscribers genuinely can't guarantee a deterministic unsubscribe call — a UI element that can
be discarded by a framework without a lifecycle hook firing, or a plugin-style architecture where
the subject cannot trust every subscriber to clean up after itself. For those cases, hold observers
through a weak reference instead of a direct strong one, so an abandoned observer becomes eligible
for garbage collection even though the subject never explicitly removed it:

```csharp
public sealed class WeakEventSubject<T>
{
    private readonly List<WeakReference<IObserver<T>>> _observers = new();

    public IDisposable Subscribe(IObserver<T> observer)
    {
        var weakRef = new WeakReference<IObserver<T>>(observer);
        _observers.Add(weakRef);
        return new Unsubscriber(_observers, weakRef);
    }

    public void Publish(T value)
    {
        foreach (var weakRef in _observers.ToArray())
        {
            if (weakRef.TryGetTarget(out var observer))
            {
                observer.OnNext(value);
            }
            else
            {
                _observers.Remove(weakRef); // collected — prune the dead entry
            }
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<WeakReference<IObserver<T>>> _observers;
        private readonly WeakReference<IObserver<T>> _weakRef;

        public Unsubscriber(List<WeakReference<IObserver<T>>> observers, WeakReference<IObserver<T>> weakRef)
        {
            _observers = observers;
            _weakRef = weakRef;
        }

        public void Dispose() => _observers.Remove(_weakRef);
    }
}
```

## The cost of the weak-reference approach

- **An observer can be collected while still logically "subscribed"** if nothing else in the
  application holds a strong reference to it — this is often exactly the desired outcome (the leak
  this file exists to prevent), but it also means an observer whose only reference was the
  subscription itself can silently stop receiving notifications the moment the garbage collector
  runs, which is a harder-to-debug failure mode than a leak: nothing crashes, and no exception
  points at the cause. Keep an explicit strong reference to any observer that must keep receiving
  notifications for as long as its owner cares about it, and rely on `WeakReference` only for
  observers that are genuinely fine disappearing once nothing else needs them.
- **Iterating and pruning dead entries has a real cost** on every publish, proportional to how many
  collected-but-not-yet-pruned entries have accumulated — for a subject that publishes very
  frequently with many long-since-collected observers still in the list, this cost is worth
  measuring rather than assumed negligible.
- **This is a targeted fix for a specific leak shape**, not a default choice — reach for it only
  after confirming that disciplined `Dispose`-based unsubscription genuinely cannot be guaranteed for
  this subject's observers, not as a substitute for writing that cleanup path in the first place.
