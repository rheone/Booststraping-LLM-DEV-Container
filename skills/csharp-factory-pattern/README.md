# C# Factory Pattern

Reference for the Factory Method and Abstract Factory design patterns in C#: a single
creation-point abstraction, families of related objects created consistently together, a generic
`IFactory<T>` interface, DI-resolved factory delegates, and when a factory is warranted versus `new`
or constructor injection alone. The routing table is in [SKILL.md](SKILL.md).

**`references/`**

| File | Covers |
| --- | --- |
| `factory-method.md` | an interface `Create` method and a static factory method; why not just call `new` everywhere |
| `abstract-factory.md` | creating families of related objects that must stay mutually consistent; vs. several separate factory methods |
| `generic-factory-interface.md` | `IFactory<T>`/`IFactory<TArg, T>`; generic factory vs. `Func<TArg, T>` |
| `factory-registration-via-di.md` | registering a factory delegate or interface; resolving a fresh transient from a singleton; the service-locator mistake to avoid |
| `factory-vs-new-vs-di.md` | the full decision list for when a factory earns its cost |
| `testing-factories.md` | asserting on a factory's produced type, testing Abstract Factory family consistency, testing a consumer with a fake factory |
| `extending-factories.md` | adding a Factory Method case, adding a new family vs. a new product to Abstract Factory, registering new factory implementations |
