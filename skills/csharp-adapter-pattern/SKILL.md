---
name: csharp-adapter-pattern
description: Reference for the Adapter design pattern in C# — wrapping an incompatible interface or class behind the target interface a consumer expects. Covers object adapters (composition, wrapping an instance — the idiomatic C# form), class adapters (inheritance-based, and why C#'s single-inheritance rule makes them uncommon), a generic adapter interface (IAdapter<TSource, TTarget>), adapting a third-party library's API shape to an application's own abstraction, extending an adapter without breaking existing callers, and testing code written against an adapter's target interface. Use when a consumer's interface doesn't match a type you don't control (a third-party client, a legacy class, a generated proxy), when deciding between an object adapter and a generic adapter interface, or when reviewing/writing a class named `*Adapter` or `*Wrapper`.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Adapter Pattern

The Adapter pattern converts the interface a class already has into the interface a consumer
expects, without modifying either side. You reach for it whenever you depend on a type whose shape
you don't control — a third-party client, a legacy class, a generated proxy — and that shape
doesn't match the abstraction your own code is written against.

## Quick start

```csharp
// The interface your application code depends on.
public interface INotificationSender
{
    void Send(string recipient, string message);
}

// A type you don't control, with an incompatible shape.
public sealed class LegacyMailerClient
{
    public int DispatchMail(string to, string subject, string body) => 0;
}

// The object adapter: wraps an instance, exposes the target interface.
public sealed class LegacyMailerAdapter : INotificationSender
{
    private readonly LegacyMailerClient _client;

    public LegacyMailerAdapter(LegacyMailerClient client) => _client = client;

    public void Send(string recipient, string message) =>
        _client.DispatchMail(recipient, subject: "Notification", body: message);
}
```

Application code depends only on `INotificationSender`. It never sees `LegacyMailerClient`, its
method names, or its return codes — the adapter absorbs all of that.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Learning the pattern's roles (target, adaptee, adapter) and when to reach for it | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| Writing the standard C# form — an adapter that wraps an instance via composition | [references/object-adapter.md](references/object-adapter.md) |
| Considering an inheritance-based adapter, or explaining why it's rare in C# | [references/class-adapter.md](references/class-adapter.md) |
| Writing a reusable `IAdapter<TSource, TTarget>` abstraction | [references/generic-adapter.md](references/generic-adapter.md) |
| Wrapping a third-party library's API shape behind your own abstraction | [references/adapting-third-party-apis.md](references/adapting-third-party-apis.md) |
| Adding a new adapter for a new adaptee without touching existing adapters or consumers | [references/extending-with-new-adapters.md](references/extending-with-new-adapters.md) |
| Testing code that depends on the target interface, or testing an adapter itself | [references/testing-adapters.md](references/testing-adapters.md) |

## Out of scope

- The Facade pattern's broader "simplify a subsystem" goal — an adapter's job is narrower and more
  precise: match one existing interface to another, not simplify a set of calls into fewer ones.
- Any specific third-party library's actual API. Third-party wrapping is described generically, by
  shape, so the guidance holds regardless of which library sits on the other side of the adapter.
