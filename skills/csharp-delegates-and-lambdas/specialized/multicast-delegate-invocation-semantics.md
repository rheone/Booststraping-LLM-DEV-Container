# Multicast Delegate Invocation Semantics

Every delegate is a `System.MulticastDelegate` with an **invocation list** — one or more entries,
each a method plus (for instance methods) a target object — established in
[references/csharp1-delegates-and-multicast.md](../references/csharp1-delegates-and-multicast.md).
This file works through what actually happens when a multi-entry delegate is invoked: the order
entries run in, what a thrown exception does to the rest of the chain, and what the call expression
evaluates to.

## Basic: invocation order matches combination order

```csharp
Action<string> pipeline = message => Console.WriteLine($"[audit] {message}");
pipeline += message => Console.WriteLine($"[notify] {message}");
pipeline += message => Console.WriteLine($"[archive] {message}");

pipeline("order placed");
// [audit] order placed
// [notify] order placed
// [archive] order placed
```

Entries run strictly in the order they were added via `+=`/`Delegate.Combine`, synchronously, one
after another on the calling thread — there's no parallelism and no reordering, regardless of how
many entries are in the list or what generic delegate type carries them (`Action<T>` here, but the
same ordering rule applies to any multicast delegate, generic or not).

## Basic: only the last entry's return value is observable

```csharp
Func<int, int> chain = n => n + 1;
chain += n => n * 10;
chain += n => n - 3;

int result = chain(5); // 2 — ONLY the last entry's return value is kept
```

Every entry in the invocation list receives the **same original arguments** — a multicast delegate
does not pipe one entry's return value into the next entry's parameters, it calls every entry
independently with `5`. Concretely: entry 1 computes `5 + 1 = 6`, entry 2 computes `5 * 10 = 50`,
entry 3 computes `5 - 3 = 2`; `chain(5)` evaluates to `2`, and `6` and `50` are computed and thrown
away. This is almost never what's wanted for a non-`void` multicast delegate — a multi-target
`Func<>`/generic delegate with a meaningful return value is a sign the code should iterate
`GetInvocationList()` explicitly instead of relying on `+=` semantics:

```csharp
foreach (Delegate handler in chain.GetInvocationList())
{
    int oneResult = ((Func<int, int>)handler)(5);
    Console.WriteLine(oneResult); // 6, then 50, then 2 — every entry's own result, visible
}
```

## Advanced: an exception in one entry stops every later entry

```csharp
Action<Order> pipeline = order => Validate(order);
pipeline += order => Charge(order);      // never runs if Validate throws
pipeline += order => SendConfirmation(order); // never runs either

try
{
    pipeline(order);
}
catch (ValidationException)
{
    // Charge and SendConfirmation were both skipped — the exception propagated out of the
    // WHOLE call, mid-invocation-list, exactly like an exception partway through a for loop
}
```

The invocation list isn't wrapped in any per-entry try/catch — an unhandled exception from entry N
aborts entries N+1 onward and propagates to the caller of `pipeline(...)` exactly as if the whole
call were one method body. `event` handler chains (built on this same mechanism) inherit this
behavior, which is why one misbehaving subscriber can silently prevent every subscriber registered
after it from ever running. Where every entry must run regardless of earlier failures,
`GetInvocationList()` plus a per-entry try/catch is the explicit way to get that:

```csharp
foreach (Delegate handler in pipeline.GetInvocationList())
{
    try
    {
        ((Action<Order>)handler)(order);
    }
    catch (Exception ex)
    {
        LogFailedHandler(ex);
    }
}
```

## Requirements and restrictions

- `Delegate.GetInvocationList()` returns a plain `Delegate[]` — cast each entry back to the specific
  delegate type (or the generic `Func<>`/`Action<>` instantiation) to invoke it directly.
- `-=`/`Delegate.Remove` removes the *last* matching entry from the list (matching by
  method+target equality), not every match — combining the same lambda expression twice creates two
  separately-removable entries only if they're the same delegate instance; two *separately created*
  lambdas with identical bodies are never equal for this purpose.
- A single-entry delegate (the common case) skips all of this — `GetInvocationList()` on it returns
  a one-element array, and the "only the last result" rule is moot because there's only one result.

## Fallback

This is C# 1.0-era `MulticastDelegate` behavior, unchanged by every later tier in this skill —
generic delegate types (`Action<T>`/`Func<T,TResult>`, C# 2.0+/C# 3.0+) and lambda syntax change
only how an invocation-list *entry* is written, never how the list itself is invoked, ordered, or
how exceptions propagate through it.
