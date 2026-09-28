# Singleton vs. Per-Call Instances

## Why singleton is the default

A null object that holds no state has no reason to exist as more than one instance — every call to
`Notify` on `NullNotifier` behaves identically regardless of which instance made the call, so
creating a new one each time only spends an allocation for no behavioral difference. The standard
shape is a single, shared, statically held instance:

```csharp
public sealed class NullNotifier : INotifier
{
    public static readonly NullNotifier Instance = new();
    private NullNotifier() { } // private constructor forces use of Instance

    public void Notify(string message) { }
}
```

The private constructor is what makes this a true singleton rather than merely a convention — no
code outside the class can construct a second `NullNotifier`, so every caller that uses
`NullNotifier.Instance` is provably using the same object, which is useful for reference-equality
checks (see below) and rules out accidental per-call allocation entirely.

```csharp
var service = new OrderService(notifier: null); // falls back to NullNotifier.Instance
```

## When a fresh instance per call is warranted instead

A null object needs its own instance per use only when it carries per-call state that a shared
singleton couldn't hold safely — a null object that records what it was called with for later
inspection (common in test doubles, see [testing.md](testing.md)), or one that's meant to be
disposed independently of every other use. A stateless null object — the overwhelming majority of
real ones — never has this requirement; treat "does this null object hold any state at all" as the
deciding question, not habit or symmetry with the real implementation's own lifetime.

```csharp
// Stateful — cannot safely be a shared singleton:
public sealed class RecordingNullNotifier : INotifier
{
    public List<string> Messages { get; } = new();
    public void Notify(string message) => Messages.Add(message);
}
```

Each test or call site that needs its own independent record of what was passed to `Notify`
constructs its own `RecordingNullNotifier` — sharing one instance across unrelated tests would let
one test's recorded messages leak into another's assertions.

## Reference equality as a deliberate check

Because the stateless singleton form guarantees exactly one instance exists, code that specifically
needs to know "was the null object used here, or a real implementation" can check for it directly:

```csharp
if (ReferenceEquals(notifier, NullNotifier.Instance))
{
    // no real notifier was configured
}
```

This is occasionally useful for diagnostics or configuration validation, but reaching for it in
ordinary business logic is usually a sign the null object is being treated as a special case again
— exactly what the pattern exists to avoid. Prefer letting the null object's no-op behavior do the
work silently; reserve the reference-equality check for genuinely diagnostic code paths.
