# C# Events and EventHandler&lt;T&gt;

C# events are the language's own built-in observer mechanism: a type declares an event, other code
subscribes a method to it with `+=`, and the declaring type raises the event by invoking it, calling
every subscribed method in turn. This is a lighter-weight alternative to implementing
`IObservable<T>`/`IObserver<T>` by hand for the common case of "notify subscribers when something
happens," without a custom subject class or a custom observer interface.

## Declaring and raising an event

```csharp
public class PriceTicker
{
    public event EventHandler<PriceChangedEventArgs>? PriceChanged;

    public void PublishPrice(decimal price)
    {
        PriceChanged?.Invoke(this, new PriceChangedEventArgs(price));
    }
}

public sealed class PriceChangedEventArgs : EventArgs
{
    public decimal Price { get; }
    public PriceChangedEventArgs(decimal price) => Price = price;
}
```

`EventHandler<TEventArgs>` is the BCL's standard event-delegate shape: `void
Handler(object? sender, TEventArgs e)`. Using it (rather than a bespoke delegate type) means every
subscriber sees the same familiar signature regardless of which type raises the event, and gets the
raising object for free through `sender`.

## Subscribing and unsubscribing

```csharp
var ticker = new PriceTicker();

void OnPriceChanged(object? sender, PriceChangedEventArgs e) =>
    Console.WriteLine($"Price: {e.Price}");

ticker.PriceChanged += OnPriceChanged;
ticker.PublishPrice(101.50m);

ticker.PriceChanged -= OnPriceChanged;
```

Unsubscribing requires the exact same delegate reference used to subscribe — `-=
OnPriceChanged` works because `OnPriceChanged` is a named method group that resolves to an
equivalent delegate instance; unsubscribing a lambda requires keeping a reference to the exact
lambda instance passed to `+=`, since a second, separately-created lambda with identical code is not
the same delegate instance and `-=` with it is silently a no-op.

```csharp
EventHandler<PriceChangedEventArgs> handler = (sender, e) => Console.WriteLine(e.Price);
ticker.PriceChanged += handler;
ticker.PriceChanged -= handler; // works — same delegate instance kept in `handler`
```

## The null-conditional invocation pattern

`PriceChanged?.Invoke(this, args)` guards against the case where no subscriber has ever attached,
in which case the event field is `null` and a direct `PriceChanged(this, args)` call would throw
`NullReferenceException`. Reading the event field once into a local before checking and invoking it
avoids a race in multithreaded code where a subscriber unsubscribes between the null check and the
invocation:

```csharp
var handler = PriceChanged;
handler?.Invoke(this, new PriceChangedEventArgs(price));
```

`?.Invoke` on the field directly already reads the field once as part of evaluating the
null-conditional operator, so it has the same safety property in modern C# — both forms are
accepted; the explicit local read makes the single-read guarantee visible to a reader unfamiliar
with the compiler's evaluation order for `?.`.

## `event` vs. a plain public delegate field

Declaring `public event EventHandler<T>? Changed;` (with the `event` keyword) restricts consumers
outside the declaring class to only `+=`/`-=` — they cannot call `Changed.Invoke(...)` directly, nor
reassign it with `=` and wipe out every other subscriber. Dropping the `event` keyword and exposing a
plain public delegate field removes both protections, letting any external code raise the
notification itself or clear every existing subscription by assignment. Always use `event` for a
notification meant to be raised only by the declaring type.

## When an event is enough

An event is the right tool when a single type raises a single, well-known notification and
subscribers are simple method callbacks with no need to compose multiple notification sources, apply
LINQ-style operators to the stream of raised values, or control subscription lifetime through
anything more structured than `+=`/`-=`. Reach for `IObservable<T>` instead when composability,
explicit disposal-based subscription lifetime, or treating the notification as a first-class stream
of values matters — the tradeoffs are covered fully in
[events-vs-iobservable.md](events-vs-iobservable.md).
