# Philosophy and Structure

The Adapter pattern has three roles:

- **Target** — the interface your consuming code is written against. It expresses what the
  consumer needs, in the consumer's own vocabulary.
- **Adaptee** — the existing class whose interface doesn't match the target. You don't modify it;
  it may not even be something you control the source of.
- **Adapter** — a class that implements the target interface and translates each call into one or
  more calls on the adaptee.

```text
Consumer ---> Target (interface) <--- Adapter ---> Adaptee (existing class)
```

The consumer only ever holds a reference to `Target`. It has no idea an adapter or an adaptee
exists on the other side of that interface.

## When you reach for it

Use an adapter when all three of these hold:

- You depend on a type whose method names, parameter shapes, return types, or error-signaling
  convention don't match what your code expects.
- You can't or don't want to change that type directly — it's owned by a library, generated code,
  or a part of the codebase you don't want coupled to your abstraction.
- You want your own code to depend on an interface you control, so you can swap the underlying
  implementation later without touching every call site.

## When you don't need it

If you own both sides — your consumer and the class it calls — change the class's interface
directly, or introduce the abstraction at the point you originally designed the class, rather than
adding a translation layer between two things you're both free to edit. An adapter earns its
existence specifically because one side is fixed.

## Choosing the target interface

Design the target interface around what your application needs, not around what the adaptee
happens to expose. A target interface that just mirrors the adaptee's method names with a thin
pass-through isn't adding value — it's the adaptee's shape dictating your abstraction instead of
the other way around. Name target members for what the consumer is trying to do
(`Send`, `Save`, `Lookup`) and let the adapter absorb the adaptee's actual method names,
parameter ordering, and return conventions.
