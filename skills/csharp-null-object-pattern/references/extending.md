# Extending Null Objects

## Adding a new no-op implementation for an existing interface

A second null object for the same interface — perhaps one that logs that it was called for
diagnostic visibility, versus a silent one for production — is an ordinary additional implementation
of the interface, chosen at the same fallback point as before:

```csharp
public sealed class LoggingNullNotifier : INotifier
{
    private readonly ILogger _logger;
    public LoggingNullNotifier(ILogger logger) => _logger = logger;

    public void Notify(string message) => _logger.LogDebug("Suppressed notification: {Message}", message);
}
```

No existing caller of `INotifier.Notify` changes — the interface itself hasn't changed, only which
concrete no-op implementation gets selected in a given environment.

## Adding a member to the interface

Adding a new method or property to an interface that already has a null-object implementation means
every existing implementation of that interface — the null object included — needs the new member
added, since a class implementing an interface must implement every member the interface declares:

```csharp
public interface INotifier
{
    void Notify(string message);
    void NotifyUrgent(string message); // new member
}

public sealed class NullNotifier : INotifier
{
    public static readonly NullNotifier Instance = new();
    private NullNotifier() { }

    public void Notify(string message) { }
    public void NotifyUrgent(string message) { } // added alongside every other implementation
}
```

This is the one genuinely breaking step in extending a null-object setup — it's breaking for every
implementer of the interface, not specific to the null object, and needs the same review any
interface-member addition would get regardless of whether a null object exists.

## Adding a return value that needs a neutral default

A method that returns a value (rather than only producing a side effect) needs its null object to
return a value that's genuinely neutral for every caller — not merely `default`, if `default` would
read as a real, meaningful result rather than an absence:

```csharp
public interface IDiscountPolicy
{
    decimal GetDiscount(Order order);
}

public sealed class NullDiscountPolicy : IDiscountPolicy
{
    public static readonly NullDiscountPolicy Instance = new();
    private NullDiscountPolicy() { }

    public decimal GetDiscount(Order order) => 0m; // 0 reads unambiguously as "no discount", not as a real computed value
}
```

`0m` is the correct neutral value here because "no discount" and "a discount of zero" are the same
thing to every caller of `GetDiscount` — there's no ambiguity to introduce. A method whose neutral
return value could be confused with a real, meaningful result (returning `null` from a method whose
callers already treat null specially, for instance) is a signal the interface itself may need a more
expressive return type before a null object can implement it without introducing exactly the
ambiguity the pattern exists to remove.

## What doesn't break existing code

Adding a brand-new interface with its own null object, adding a second alternative null-object
implementation for an existing interface, and changing which concrete implementation the fallback
point selects are all additive or localized changes. Adding a member to an existing interface is the
one change that ripples to every implementer, null object included, and deserves the same scrutiny
any interface change would get on its own terms.
