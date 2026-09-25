# Closures and Variable Capture

A lambda or anonymous method that references a local variable, parameter, or `this` from its
enclosing scope **closes over** that variable rather than copying its value — the compiler lifts the
captured variable into a compiler-generated class instance shared between the enclosing code and
every closure that references it. This is the mechanism behind every closure example in
[references/csharp2-anonymous-methods-and-generic-delegates.md](../references/csharp2-anonymous-methods-and-generic-delegates.md)
onward; this file works through the pitfalls it creates and how `static` lambdas
([references/csharp9-static-lambdas-and-discard-parameters.md](../references/csharp9-static-lambdas-and-discard-parameters.md))
opt out of them entirely.

## Basic: capturing by reference, not by value

```csharp
int counter = 0;
Action increment = () => counter++;

increment();
increment();
Console.WriteLine(counter); // 3, not 1 — every call mutates the SAME captured `counter`
```

The lambda doesn't capture `counter`'s value at the point the lambda is created — it captures the
*variable itself*. Every closure over the same enclosing scope shares the same captured storage,
including the enclosing method's own code after the lambda is defined.

## Basic: capturing a loop variable — `foreach` is safe, `for` is not

```csharp
// foreach: safe on every C# version — the loop variable is scoped fresh per iteration since C# 5.0
var handlers = new List<Action>();
foreach (string name in new[] { "alpha", "beta", "gamma" })
{
    handlers.Add(() => Console.WriteLine(name)); // each closure captures ITS OWN `name`
}
foreach (Action handler in handlers) handler(); // alpha, beta, gamma
```

```csharp
// for: the counter is ONE variable mutated across the whole loop, on every C# version
var brokenHandlers = new List<Action>();
for (int i = 0; i < 3; i++)
{
    brokenHandlers.Add(() => Console.WriteLine(i)); // every closure captures the SAME `i`
}
foreach (Action handler in brokenHandlers) handler(); // 3, 3, 3 — not 0, 1, 2
```

`foreach`'s per-iteration variable scoping is a C# 5.0 language fix (older code compiled against
C# 4.0 or earlier had the same "all closures see the final value" bug for `foreach` that `for`
still has on every version). Fix the `for` case by copying into a fresh local inside the loop body:

```csharp
for (int i = 0; i < 3; i++)
{
    int captured = i; // one new variable per iteration — safe to capture
    brokenHandlers.Add(() => Console.WriteLine(captured));
}
```

## Advanced: capturing mutable state through a generic factory

```csharp
public static Func<T> MakeSnapshotter<T>(T initial) where T : struct
{
    T current = initial;
    return () => current; // captures the local `current`, not a copy of `initial`
}

Func<int> snapshot = MakeSnapshotter(10);
Console.WriteLine(snapshot()); // 10
```

The captured variable `current` lives exactly as long as something can still reach it through a
delegate — here, the returned `Func<T>` — which is longer than the `MakeSnapshotter<T>` call itself.
This is ordinary closure lifetime, not special generic behavior, but it's easy to assume a generic
factory method's locals go out of scope on return the way a non-capturing method's would.

## Advanced: capturing `this` implicitly keeps the whole instance alive

```csharp
public class ReportGenerator
{
    private readonly byte[] _largeBuffer = new byte[10_000_000];

    public Action MakeCallback() => () => Console.WriteLine(_largeBuffer.Length);
    // captures `this` implicitly (an instance field reference needs the instance),
    // so the callback keeps the ENTIRE ReportGenerator — including _largeBuffer — alive
    // for as long as the callback itself is reachable
}
```

A lambda that references an instance field or calls an instance method captures `this`, not just
that one member — the whole object graph reachable from `this` stays alive for the closure's
lifetime. A `static` lambda (C# 9.0+) makes this impossible by construction, since it can't
reference `this` or any instance member at all; extracting the needed value into a local before
the lambda, then capturing only that local, has the same effect on earlier targets.

## Fallback

Everything above is C# 2.0-era closure behavior — the one version-specific detail is `foreach`'s
per-iteration variable scoping, called out inline as a C# 5.0 fix; every version from C# 2.0 onward
otherwise captures variables identically, anonymous methods and lambdas alike. Lambda syntax itself
needs C# 3.0 fallback per
[references/csharp3-lambdas-and-func-action.md](../references/csharp3-lambdas-and-func-action.md);
this file's capture semantics apply unchanged to the anonymous-method equivalents on older targets.
