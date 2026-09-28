---
name: csharp-null-object-pattern
description: Guidance on the Null Object design pattern in C# — providing a do-nothing/neutral implementation of an interface so call sites never need a null check for missing behavior, a shared singleton null-object instance versus constructing a fresh one per call, a generic/type-parameterized null-object base for reuse across interfaces, and how the pattern relates to nullable reference types — the null object pattern eliminates a null check for absent behavior at the call site, while nullable reference types make a missing null check into a compiler warning at the point a reference could be null, addressing a related but distinct problem. Use when a method or property can meaningfully have "no behavior" as an outcome, when callers keep repeating the same null check before invoking an optional collaborator, when designing a fallback/no-op implementation for logging, notification, caching, or similar optional-behavior interfaces, or when deciding whether a genuinely absent value calls for a null object versus a nullable reference. Explains nullable reference types' own role here without depending on any other skill.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Null Object Pattern

Null Object is a **design pattern**, not a package — there is nothing to install and no version to
pin. This skill documents the pattern itself: a do-nothing implementation of an interface that
stands in for "no behavior here" so calling code never branches on null to find out, singleton vs.
per-call null-object instances, a generic null-object base, and precisely how the pattern relates to
nullable reference types.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Understanding what problem Null Object solves and its basic shape | [references/core-concept-and-motivation.md](references/core-concept-and-motivation.md) |
| Deciding between a shared singleton null object and a fresh instance per call | [references/singleton-vs-per-call-instances.md](references/singleton-vs-per-call-instances.md) |
| Writing a reusable, type-parameterized null-object base or factory | [references/generic-null-object-base.md](references/generic-null-object-base.md) |
| Understanding how this pattern relates to nullable reference types | [references/nullable-reference-types-interaction.md](references/nullable-reference-types-interaction.md) |
| Testing code that depends on an interface with a null-object implementation | [references/testing.md](references/testing.md) |
| Adding a new no-op implementation for an existing interface, or a new interface member, without breaking callers | [references/extending.md](references/extending.md) |

## Quick start

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

A caller that might or might not have a real notifier configured uses `NullNotifier.Instance` as the
default instead of `null`, and every call site that invokes `Notify` stops needing a null check:

```csharp
public sealed class OrderService
{
    private readonly INotifier _notifier;

    public OrderService(INotifier? notifier) => _notifier = notifier ?? NullNotifier.Instance;

    public void PlaceOrder(Order order)
    {
        // ... place the order ...
        _notifier.Notify($"Order {order.Id} placed."); // no null check needed here, ever
    }
}
```

The null check happens exactly once, at construction, instead of at every call site that would
otherwise need `_notifier?.Notify(...)`. Start with
[references/core-concept-and-motivation.md](references/core-concept-and-motivation.md) for the full
reasoning, then
[references/singleton-vs-per-call-instances.md](references/singleton-vs-per-call-instances.md) for
when `Instance` above is the right choice.

## Out of scope

- General optional-value handling for data (a missing customer address, an absent configuration
  value) — this skill covers substituting for *behavior* (a method call with a meaningful no-op),
  not for a data value that's genuinely absent and needs representing as such.
- Dependency-injection container configuration mechanics (how a specific container registers a
  default implementation) — this skill covers the pattern's shape and where the fallback gets
  chosen, not any particular container's registration API.
