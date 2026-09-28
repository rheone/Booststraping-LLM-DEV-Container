# Observer Pattern

You let a subject notify any number of interested parties about a change without knowing who they
are ahead of time. It covers the classic `IObservable<T>`/`IObserver<T>` form, C# events and
`EventHandler<T>` as the language's own lightweight mechanism, the tradeoffs between the two, and
weak-reference observers for avoiding memory leaks.

## When to reach for it

- A subject needs to notify a variable, possibly-empty set of listeners when something happens,
  and you're choosing how to wire that notification up.
- You're deciding between a plain C# event and `IObservable<T>` for a given subscription.
- A long-lived subject holds references to shorter-lived observers, and you're worried about (or
  diagnosing) a memory leak from listeners that never unsubscribed.

## Using it

This skill is model-invoked: it fires automatically when your prompt matches its situation, such as
implementing a subscribe/notify mechanism or debugging a lapsed-listener leak. You can also invoke
it directly as `/csharp-observer-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| Implementing `IObservable<T>`/`IObserver<T>` | [references/classic-isubject-iobserver.md](references/classic-isubject-iobserver.md) |
| Declaring and raising events with `EventHandler<T>` | [references/events-and-eventhandler.md](references/events-and-eventhandler.md) |
| Choosing between an event and `IObservable<T>` | [references/events-vs-iobservable.md](references/events-vs-iobservable.md) |
| Avoiding lapsed-listener leaks with weak references | [references/weak-reference-observers.md](references/weak-reference-observers.md) |
| Testing delivery, unsubscription, and terminal notifications | [references/testing-observers.md](references/testing-observers.md) |
| Adding a new observer or notification | [references/extending-observers.md](references/extending-observers.md) |

## Example prompts

- "How do I raise an event when the price changes and let multiple listeners subscribe to it?"
- "Should this be a plain C# event or should I implement `IObservable<T>`?"
- "My subject keeps a list of observers that never get removed. How do I stop that from leaking
  memory?"
