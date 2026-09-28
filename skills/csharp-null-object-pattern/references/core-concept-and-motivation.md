# Core Concept and Motivation

## The problem

An optional collaborator — a logger nobody configured, a notifier with nothing to notify, a
discount rule that doesn't apply to a given customer — often gets represented as a reference that
can be null when there's nothing to do. Every call site that might invoke it then needs its own null
check:

```csharp
public sealed class OrderService
{
    private readonly INotifier? _notifier;

    public void PlaceOrder(Order order)
    {
        // ... place the order ...
        _notifier?.Notify($"Order {order.Id} placed.");
    }

    public void CancelOrder(Order order)
    {
        // ... cancel the order ...
        _notifier?.Notify($"Order {order.Id} cancelled.");
    }
}
```

Each `?.` is a correct, safe check — but it's the *same* check, repeated at every call site that
uses `_notifier`, forever. Forgetting one is a `NullReferenceException` waiting for the one code
path nobody thought to guard.

## The pattern

Instead of representing "nothing to do" as null, represent it as a real object that implements the
same interface and does nothing when called:

```csharp
public interface INotifier
{
    void Notify(string message);
}

public sealed class NullNotifier : INotifier
{
    public static readonly NullNotifier Instance = new();
    private NullNotifier() { }

    public void Notify(string message) { /* intentionally does nothing */ }
}
```

The field that used to be a nullable reference becomes a non-nullable one, defaulted to the null
object instead of to `null`:

```csharp
public sealed class OrderService
{
    private readonly INotifier _notifier;

    public OrderService(INotifier? notifier) => _notifier = notifier ?? NullNotifier.Instance;

    public void PlaceOrder(Order order)
    {
        // ... place the order ...
        _notifier.Notify($"Order {order.Id} placed."); // never null, never needs a check
    }

    public void CancelOrder(Order order)
    {
        // ... cancel the order ...
        _notifier.Notify($"Order {order.Id} cancelled."); // same here
    }
}
```

The null check moves from every call site down to exactly one place — the constructor, where the
real-or-null choice actually gets made. Every method that uses `_notifier` afterward can simply call
it.

## When it fits

The pattern fits when "do nothing" is itself a *valid, meaningful* outcome for the interface's
behavior — a notifier that notifies no one, a cache that caches nothing, a logger that logs nothing
are all coherent, harmless no-ops. It's the right tool specifically for **optional behavior**: a
collaborator a caller invokes for effect, not one it queries for an answer it depends on.

## When it doesn't fit

The pattern doesn't fit when "no value" and "a value that happens to do nothing" mean genuinely
different things to the caller — a repository's `FindById` returning a null object that silently
answers every property getter with a default value hides the fact that no matching record exists,
which is exactly the information a caller needs to branch on. Substituting a null object for a
missing *value* the caller must reason about, rather than missing *behavior* the caller merely
invokes, trades a clear absence signal for a data value that looks present but isn't, which is often
a worse failure mode than the null check it replaced. Reserve the pattern for behavior; represent a
genuinely absent value as itself, with whatever the calling code needs to detect that absence
explicitly (see
[nullable-reference-types-interaction.md](nullable-reference-types-interaction.md) for how nullable
reference types address that adjacent, distinct problem).

## The shape summarized

- An interface describing behavior a caller invokes without depending on its result.
- A do-nothing implementation of that interface, satisfying every member with a no-op.
- A single point (a constructor, a factory, a default parameter) where a real implementation or the
  null object gets chosen — never a null check scattered across every call site.
