---
name: csharp-observer-pattern
description: Reference for the Observer design pattern in C# — the classic subject/observer form using the BCL's own IObservable<T>/IObserver<T> interfaces, C# events and EventHandler<T> as the language's built-in lightweight observer mechanism, the tradeoffs between events and IObservable<T> (subscription lifetime and disposal, composability), and weak-reference observer patterns to avoid memory leaks when a long-lived subject holds references to shorter-lived observers. Use when a subject needs to notify a variable set of interested parties about changes, choosing between a plain event and IObservable<T>, implementing IDisposable-based unsubscription, or diagnosing/preventing a lapsed-listener memory leak.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Observer Pattern

Observer lets a subject notify any number of interested parties about changes without knowing who
they are — a subject holds a collection of observers behind a shared interface (or an event's
invocation list) and calls into each one when something happens.

## Quick start

```csharp
public class PriceTicker
{
    public event EventHandler<PriceChangedEventArgs>? PriceChanged;

    public void PublishPrice(decimal price) =>
        PriceChanged?.Invoke(this, new PriceChangedEventArgs(price));
}
```

```csharp
ticker.PriceChanged += (sender, e) => Console.WriteLine($"Price: {e.Price}");
```

For subscription lifetime managed through `IDisposable`, or a stream of values composed with
operators, use the BCL's `IObservable<T>`/`IObserver<T>` pair instead — see
[references/classic-isubject-iobserver.md](references/classic-isubject-iobserver.md).

## Pick your reference file

| Situation | Reference file |
| --- | --- |
| Implementing a subject/observer pair with `IObservable<T>`/`IObserver<T>` | [references/classic-isubject-iobserver.md](references/classic-isubject-iobserver.md) |
| Using a C# event as the notification mechanism | [references/events-and-eventhandler.md](references/events-and-eventhandler.md) |
| Deciding between an event and `IObservable<T>` | [references/events-vs-iobservable.md](references/events-vs-iobservable.md) |
| A long-lived subject risks leaking shorter-lived observers | [references/weak-reference-observers.md](references/weak-reference-observers.md) |
| Testing a subject's delivery, unsubscription, and terminal notifications | [references/testing-observers.md](references/testing-observers.md) |
| Adding a new observer or a new notification without breaking existing ones | [references/extending-observers.md](references/extending-observers.md) |
