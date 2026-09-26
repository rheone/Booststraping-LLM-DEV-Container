# Generic Null Object Base

A project with several optional-behavior interfaces that follow the same shape — a no-op
implementation, exposed as a shared singleton — can end up writing the same boilerplate (private
constructor, static `Instance` field) once per interface. A small generic helper removes the
repetition without changing the pattern's structure at any call site.

## A generic factory for stateless null objects

For an interface with a parameterless implementation achievable through a lambda-backed proxy, a
generic factory avoids hand-writing a dedicated class per interface entirely — practical for small,
single-method interfaces:

```csharp
public interface ICacheInvalidator
{
    void Invalidate(string key);
}

public static class NullObject
{
    public static T Create<T>() where T : class => DispatchProxy.Create<T, NoOpDispatcher>();
}

internal sealed class NoOpDispatcher : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
}
```

```csharp
ICacheInvalidator nullInvalidator = NullObject.Create<ICacheInvalidator>();
```

This trades an explicit, readable class per interface for a single reusable mechanism, at the cost
of `DispatchProxy`'s runtime proxy-generation overhead and a less discoverable implementation (a
reader following `nullInvalidator` to its definition finds a generic proxy, not a purpose-named
`NullCacheInvalidator` class). Reach for it only when the number of trivial single-method interfaces
needing a null object is large enough that the boilerplate reduction outweighs that discoverability
cost — for one or two interfaces, the explicit class shown in
[core-concept-and-motivation.md](core-concept-and-motivation.md) stays clearer.

## A generic base class for null objects sharing common infrastructure

When several null objects share more structure than "do nothing" — logging that they were invoked,
for instance — a generic base class captures that shared behavior once:

```csharp
public abstract class NullObjectBase<TSelf> where TSelf : NullObjectBase<TSelf>, new()
{
    private static readonly Lazy<TSelf> LazyInstance = new(() => new TSelf());
    public static TSelf Instance => LazyInstance.Value;
}

public sealed class NullNotifier : NullObjectBase<NullNotifier>, INotifier
{
    public void Notify(string message) { }
}
```

```csharp
INotifier notifier = NullNotifier.Instance;
```

`NullObjectBase<TSelf>` implements the singleton-instance mechanics exactly once (see
[singleton-vs-per-call-instances.md](singleton-vs-per-call-instances.md) for why singleton is the
default shape), and every concrete null object built on it only needs to implement the interface's
members as no-ops — it inherits `Instance` rather than redeclaring the private-constructor-plus-
static-field pattern each time. The `where TSelf : NullObjectBase<TSelf>, new()` constraint is the
same self-typed (CRTP) shape used anywhere a generic base needs to construct and return the derived
type from a base-class member.

## Choosing between an explicit class and a generic mechanism

Write an explicit, purpose-named null-object class (`NullNotifier`, `NullLogger`) as the default —
it's the most readable and the most discoverable in a debugger or an IDE's "find implementations."
Reach for the generic base or factory shown here only once the number of near-identical null objects
in a project is large enough that the shared mechanics are worth extracting, and even then keep each
concrete null object as its own named type built on the shared base, rather than fully anonymizing
every null object behind the fully generic `DispatchProxy` factory.
