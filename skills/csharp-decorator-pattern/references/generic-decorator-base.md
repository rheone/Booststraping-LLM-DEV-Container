# Generic Decorator Base for Wide Interfaces

An interface with many members turns every decorator into a wall of one-line forwarding methods
before you even reach the one or two members the decorator actually cares about. A generic decorator
base class implements every member as a plain forward to the wrapped instance, so a concrete
decorator only overrides the members it actually changes.

## The boilerplate problem

```csharp
public interface IRepository<T>
{
    T? GetById(int id);
    IReadOnlyList<T> GetAll();
    void Add(T item);
    void Update(T item);
    void Delete(int id);
    int Count { get; }
}
```

A decorator that only wants to add logging around `Add` still has to implement all six members if it
implements `IRepository<T>` directly — five of them pure forwarding, one with the actual logic.

## The generic base

```csharp
public abstract class RepositoryDecorator<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;

    protected RepositoryDecorator(IRepository<T> inner) => _inner = inner;

    public virtual T? GetById(int id) => _inner.GetById(id);
    public virtual IReadOnlyList<T> GetAll() => _inner.GetAll();
    public virtual void Add(T item) => _inner.Add(item);
    public virtual void Update(T item) => _inner.Update(item);
    public virtual void Delete(int id) => _inner.Delete(id);
    public virtual int Count => _inner.Count;
}
```

A concrete decorator derives from this base and overrides only what it changes:

```csharp
public sealed class LoggingRepositoryDecorator<T> : RepositoryDecorator<T>
{
    private readonly ILogger _logger;

    public LoggingRepositoryDecorator(IRepository<T> inner, ILogger logger) : base(inner)
    {
        _logger = logger;
    }

    public override void Add(T item)
    {
        _logger.LogInformation("Adding {Type}", typeof(T).Name);
        base.Add(item);
    }
}
```

`LoggingRepositoryDecorator<T>` has exactly one method body beyond its constructor — everything else
is inherited, forwarding, `virtual` behavior from `RepositoryDecorator<T>`.

## Why every forwarding member must be `virtual`

If `RepositoryDecorator<T>`'s members are not `virtual`, a derived decorator's "override" is
actually member hiding (`new`), which only takes effect when the caller's static type is the derived
class — calling through the `IRepository<T>` interface reference (the normal case, since consumers
depend on the interface, not the concrete decorator) would silently skip the override and run the
base class's forwarding implementation instead. Every member the base class forwards must be
`virtual` (or the base class itself must be an abstract class with only some members implemented,
leaving the rest genuinely abstract) for this pattern to be safe to build on.

## When the generic base is worth it

Build a generic decorator base when the interface has enough members that hand-writing full
forwarding in every decorator becomes repetitive and error-prone — a forgotten forwarding call in a
hand-written decorator silently breaks the member it forgot to forward, and that bug is easy to miss
in code review since the broken method still compiles. For an interface with one or two members, a
hand-written decorator implementing the interface directly is simpler and doesn't need a base class
at all; the generic base earns its cost specifically as member count grows.

## Sealing the concrete decorators

Mark concrete decorators (`LoggingRepositoryDecorator<T>`) `sealed` even though the base class
exposes `virtual` members — a decorator is meant to be composed with other decorators through the
interface, not subclassed further. If a genuinely new decorator behavior is needed, write a new
class deriving from the generic base directly rather than deriving from an existing concrete
decorator.
