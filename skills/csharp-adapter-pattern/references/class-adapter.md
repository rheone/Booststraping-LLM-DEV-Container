# Class Adapter

The class adapter form implements the target interface by inheriting from the adaptee, rather than
holding a reference to an instance of it. It relies on overriding or exposing the adaptee's members
directly through the subclass.

```csharp
public class LegacyStack
{
    public void PushItem(object value) { /* ... */ }
    public object PopItem() => new object();
}

public interface IStack<T>
{
    void Push(T value);
    T Pop();
}

// Class adapter: inherits the adaptee, implements the target on top of it.
public sealed class LegacyStackAdapter<T> : LegacyStack, IStack<T>
{
    public void Push(T value) => PushItem(value!);
    public T Pop() => (T)PopItem();
}
```

## Why this form is uncommon in C#

C# allows a class to inherit from exactly one base class. Choosing a class adapter spends that one
inheritance slot on the adaptee, which rules out the adapter itself ever needing a different base
class for its own reasons — a base that supplies shared adapter infrastructure, for instance, or a
framework base class the hosting application requires. An object adapter spends no inheritance
slot at all, since it reaches the adaptee through a held reference instead.

A class adapter also exposes every `public` and `protected` member the adaptee declares, not just
the ones the target interface needs, unless you deliberately hide them (which C# lets you do only
via `new`-shadowing, not true suppression). Consumers who hold a reference typed as the adapter
class itself, rather than as the target interface, can see straight through to the adaptee's own
API — the exact leak an adapter exists to prevent.

## When it still applies

A class adapter is worth considering only when both hold:

- The adaptee exposes `protected` members the adapter genuinely needs and that no `public` member
  exposes — inheritance is the only way to reach them.
- The adaptee is not sealed, and giving up the adapter's own inheritance slot has no other cost in
  this codebase.

Outside that narrow case, prefer the object adapter: it works against sealed adaptees, adaptees
that already have their own base class, and adaptees you'd rather not couple your adapter's type
hierarchy to.
