# Delegates and Multicast Delegates (C# 1.0 / .NET Framework 1.0)

C# shipped with delegates from its first release (.NET Framework 1.0, January 2002) — there is no
earlier C#/.NET era to treat as a "before" for this feature; a `delegate` type is itself the
mechanism every later tier in this skill (anonymous methods, lambdas, `Func<>`/`Action<>`) extends.
Every `delegate` declaration compiles to a sealed class derived from `System.MulticastDelegate`,
and every delegate instance is multicast-capable by default — there is no non-multicast delegate to
opt out of.

## Syntax

```csharp
public delegate int Operation(int x, int y);
```

A `delegate` declaration defines a type: a reference type whose values are "a method with this
exact signature." Declare it at namespace, type, or nested scope, same as any other type.

## Basic use case

In C# 1.0, instantiate a delegate from a named method with `new` — there's no implicit method
group conversion yet (that arrives in C# 2.0):

```csharp
public class Calculator
{
    public static int Add(int x, int y) => x + y;
    public static int Multiply(int x, int y) => x * y;
}

Operation op = new Operation(Calculator.Add);
int result = op(3, 4); // 7 — invoking a delegate looks exactly like invoking a method
```

An instance method works the same way, capturing the target object alongside the method:

```csharp
public class Logger
{
    public void Write(string message) => Console.WriteLine(message);
}

public delegate void Writer(string message);

Logger logger = new Logger();
Writer write = new Writer(logger.Write); // captures `logger` as the invocation target
write("started");
```

## Advanced use case: multicast delegates

`+=`/`-=` (or `Delegate.Combine`/`Delegate.Remove`) build an **invocation list** — every delegate
is really a list of one or more method+target pairs, invoked in the order they were added:

```csharp
public delegate void Notify(string message);

Notify notify = new Notify(LogToConsole);
notify += new Notify(LogToFile);   // now a 2-entry invocation list
notify += new Notify(LogToConsole); // the same method can appear more than once

notify("build finished"); // LogToConsole, then LogToFile, then LogToConsole again

notify -= new Notify(LogToFile); // removes the first matching entry, not all matches
```

This is the mechanism `event` is built on (`event` adds subscription-safety rules on top of a
plain multicast delegate field — out of scope here, but every event handler chain is exactly this
invocation-list behavior underneath). Invocation-list ordering, what happens when one method in
the chain throws, and what `notify(...)` actually returns when there's more than one target are
covered in [specialized/multicast-delegate-invocation-semantics.md](../specialized/multicast-delegate-invocation-semantics.md).

## Requirements and restrictions

- A delegate type is *nominal*, not structural before C# 10 — two delegate types with identical
  signatures are still different types and not interchangeable without a cast/re-wrap, even though
  their invocation shape matches. (C# 10 gives lambdas and method groups a *natural* delegate type
  inferred from context, covered in
  [csharp10-natural-type-and-lambda-annotations.md](csharp10-natural-type-and-lambda-annotations.md);
  it doesn't change this rule for explicitly-declared named delegate types.)
- `Combine`/`+=` requires both delegate operands to be the same delegate type.
- A delegate with a non-`void` return type invoked through `+=` still compiles and runs every
  method in the invocation list, but only the *last* invoked method's return value is
  observable through the call expression — see the specialized file above.

## Fallback

This is the first tier — there is no earlier fallback. On any .NET Framework 1.0+ target, this
`new Operation(Method)` / `+=` syntax is all that's available; anonymous methods (C# 2.0), lambda
expressions (C# 3.0), and implicit method group conversion (C# 2.0, letting you write
`Operation op = Calculator.Add;` without `new`) are unavailable until their respective tiers.
