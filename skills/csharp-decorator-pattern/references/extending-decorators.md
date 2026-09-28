# Adding a New Decorator Without Breaking Existing Code

Adding a new cross-cutting behavior to a decorated interface means writing one new class that wraps
the interface — no existing decorator, the interface itself, or the base implementation needs to
change.

## Adding a decorator: the steps

1. **Implement the interface, holding a reference to the same interface as your "inner" instance.**

   ```csharp
   public sealed class MetricsReportGenerator : IReportGenerator
   {
       private readonly IReportGenerator _inner;
       private readonly IMetricsRecorder _metrics;

       public MetricsReportGenerator(IReportGenerator inner, IMetricsRecorder metrics)
       {
           _inner = inner;
           _metrics = metrics;
       }

       public string Generate(ReportData data)
       {
           var stopwatch = Stopwatch.StartNew();
           var result = _inner.Generate(data);
           _metrics.RecordDuration("report.generate", stopwatch.Elapsed);
           return result;
       }
   }
   ```

   If the interface is wide enough to use a generic decorator base (see
   [generic-decorator-base.md](generic-decorator-base.md)), derive from that base instead and
   override only the members this decorator changes.

2. **Decide where it belongs in the chain** using the reasoning in
   [chaining-decorators.md](chaining-decorators.md) — does this behavior need to see every attempt
   an inner layer makes, or only the final outcome reaching it?

3. **Insert it into the composition** at the chosen position — a single line in whatever code
   assembles the chain (a composition root, a factory method, a DI registration). No existing
   decorator class changes; only the assembly code that lists which decorators apply, and in what
   order, changes.

4. **Test the new decorator in isolation** against a fake inner instance (see
   [testing-decorators.md](testing-decorators.md)) — existing decorator tests need no changes, since
   no existing decorator's behavior changed.

## What must stay true for this to keep working

- **Every decorator implements the full interface**, even the members it doesn't change (via direct
  forwarding, or via a generic decorator base) — a decorator that only partially implements the
  interface isn't substitutable for it and breaks the composition.
- **No decorator casts its inner reference to a concrete type** to reach behavior outside the shared
  interface. A decorator that needs `((BasicReportGenerator)_inner).SomeInternalMethod()` has broken
  the abstraction — any future re-composition that puts a different decorator in that inner slot
  fails at runtime with a cast exception. If a decorator genuinely needs a capability the shared
  interface doesn't expose, that capability belongs on the interface itself (implemented as a plain
  forward by every other decorator/base), not behind a downcast.
- **Decorators don't assume a specific position in the chain.** A decorator whose correctness
  depends on being outermost or innermost (rather than merely being *placed* there deliberately by
  whoever assembles the chain) is fragile — document the assumption where the chain is assembled, not
  by having the decorator itself detect or enforce its position.

## Removing a decorator

Deleting a decorator from the chain is symmetric to adding one: remove its line from the composition
code, delete its class and tests. No other decorator or the interface itself needs to change, since
each decorator only ever knows about the shared interface — never about which other concrete
decorators happen to be adjacent to it in a given composition.
