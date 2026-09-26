# Interaction with Nullable Reference Types

## Two different problems that both involve null

Nullable reference types are a compiler feature: with the feature enabled (`#nullable enable`, or
project-wide via `<Nullable>enable</Nullable>`), every reference type is treated as non-nullable by
default, and a type must be explicitly annotated with `?` (`string?`, `INotifier?`) to be allowed to
hold null. The compiler then performs static flow analysis and emits a warning wherever code
dereferences a possibly-null reference without a check the compiler can see, or assigns null to a
reference the annotations say shouldn't hold it.

```csharp
public sealed class OrderService
{
    private readonly INotifier? _notifier; // annotated: this field is allowed to be null

    public void PlaceOrder(Order order)
    {
        _notifier.Notify($"Order {order.Id} placed."); // CS8602: dereference of a possibly null reference
    }
}
```

That warning is the feature doing its job: it caught, at compile time, a call site that would have
thrown `NullReferenceException` at run time if `_notifier` were ever actually null. Nullable
reference types solve **"the compiler flags a missing null check"** — they make an unguarded
dereference visible before the code ships, instead of leaving it to be discovered by a crash.

The null object pattern solves a different problem: **"avoid needing a null check at all for
missing behavior."** It doesn't help the compiler catch anything — it removes the scenario the
compiler would otherwise need to flag, by ensuring the reference in question is simply never null in
the first place.

## What changes when nullable reference types are enabled

With the feature on, the field that holds a null object is declared non-nullable, and the compiler
confirms there's no path to it holding null:

```csharp
public sealed class OrderService
{
    private readonly INotifier _notifier; // non-nullable: this field is never null

    public OrderService(INotifier? notifier) => _notifier = notifier ?? NullNotifier.Instance;

    public void PlaceOrder(Order order)
    {
        _notifier.Notify($"Order {order.Id} placed."); // no warning: the compiler can see _notifier is never null
    }
}
```

The constructor parameter `notifier` stays nullable, because a caller genuinely might not have a
real notifier to pass — that's an accurate, useful annotation of a value that really can be null at
that one point. The field it feeds, after the `??` fallback, is accurately non-nullable, because
after that line executes, it never holds null again for the rest of the object's lifetime. The
annotations end up *more* precise with the null object pattern in place, not less — the compiler's
model of "can this be null" now matches the code's actual, enforced behavior.

## Why one doesn't replace the other

Enabling nullable reference types without introducing a null object still leaves every call site
needing its own `?.` or an explicit null check — the compiler will insist on one, but "insist on a
null check" and "eliminate the need for one" are not the same outcome:

```csharp
public sealed class OrderService
{
    private readonly INotifier? _notifier;

    public void PlaceOrder(Order order)
    {
        _notifier?.Notify($"Order {order.Id} placed."); // compiler-satisfied, but still a per-call-site check
    }
}
```

This compiles cleanly under nullable reference types and is completely correct — but every method
that uses `_notifier` still carries its own `?.`, which is exactly the repetition the null object
pattern exists to remove. Conversely, introducing a null object in a codebase that doesn't enable
nullable reference types at all still gets the pattern's full benefit (no null check needed at any
call site) even though the compiler never verifies that the field can't be null — the guarantee
comes from the constructor's own logic, not from compiler-enforced annotations. The two mechanisms
address adjacent problems and compose cleanly together: the null object pattern removes the need for
a null check at behavior call sites, and nullable reference types verify, at compile time, that any
null check the codebase does still need has actually been written wherever the compiler can't prove
otherwise.
