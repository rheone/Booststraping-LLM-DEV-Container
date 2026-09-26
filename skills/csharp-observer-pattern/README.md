# C# Observer Pattern

Reference for the Observer design pattern in C#: the classic subject/observer form using the BCL's
`IObservable<T>`/`IObserver<T>` interfaces, C# events and `EventHandler<T>`, the tradeoffs between
the two, and weak-reference observer patterns to avoid memory leaks. The routing table is in
[SKILL.md](SKILL.md).

**`references/`**

| File | Covers |
| --- | --- |
| `classic-isubject-iobserver.md` | implementing `IObservable<T>`/`IObserver<T>`; the `Subscribe`/`IDisposable` unsubscribe handle |
| `events-and-eventhandler.md` | declaring/raising events, `EventHandler<T>`, `+=`/`-=`, the null-conditional invocation pattern, `event` vs. a plain delegate field |
| `events-vs-iobservable.md` | subscription lifetime and disposal, composability, and when to choose each |
| `weak-reference-observers.md` | the lapsed-listener leak, disciplined unsubscription, and a weak-reference subject when discipline isn't reliable |
| `testing-observers.md` | testing delivery, unsubscription, terminal notifications, and collection behavior for weak-reference subjects |
| `extending-observers.md` | adding a new observer/subscriber, adding a genuinely new notification, and what would break existing observers |
