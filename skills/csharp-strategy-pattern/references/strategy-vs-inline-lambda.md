# Strategy vs. an Inline Lambda

Every Strategy implementation — interface-based or delegate-based — is optional structure wrapped
around what a single inline lambda at the call site could do instead. Reach for the structure only
when it earns its cost; otherwise the inline lambda is not a lesser version of the pattern, it is
the correct amount of code for the problem.

## When an inline lambda is enough

```csharp
var total = order.Items
    .Where(item => item.Category == "Electronics")
    .Sum(item => item.Price * item.Quantity);
```

Nothing here should become a named strategy. The lambda is used exactly once, has no independent
identity anyone needs to reference, is never swapped at runtime by a caller, and is easier to read
inline than through an indirection that sends the reader to another file.

## Signals that justify promoting a lambda to a Strategy

- **The same variant needs to be referenced from more than one call site**, and keeping them in sync
  by hand is a real risk. A named strategy — even a `static readonly Func<>` field — gives every
  call site the same behavior by construction instead of by discipline.
- **The choice of algorithm needs to be made by someone other than the code that runs it** — a
  caller passing in which variant to use, a DI container resolving one from configuration, a test
  substituting a fake. A lambda baked into the method body cannot be swapped from outside; a
  strategy accepted as a constructor or method parameter can.
- **The algorithm needs to be unit-tested on its own**, independent of the context that runs it. A
  private lambda embedded in a method has no seam a test can call directly; a named strategy class
  or a `static` method referenced by a `Func<>` does.
- **The variant carries its own state or dependencies** beyond what a closure can hold cleanly. Once
  a "strategy" needs a constructor-injected dependency, an interface implementation is the natural
  home for it — a lambda would need to be built inside a factory function just to close over that
  dependency, which is more indirection than the interface form, not less.
- **There is more than one related operation per variant.** A lambda is one function; a strategy
  interface with two or three members groups related behavior that must vary together, which no
  single delegate can express.

## The wrong reason to promote a lambda

Promoting a lambda to an interface-based Strategy "because it's more object-oriented," or because a
codebase has a habit of using the GoF form everywhere a decision point exists, adds an interface, an
implementing class, and a DI registration for a piece of logic that will only ever have the one
inline form it started with. The interface earns nothing here — nobody swaps it, nobody tests it in
isolation, nobody depends on it needing more than the one operation it already has. Judge each case
against the signals above rather than against a rule that Strategy is always the more "correct"
shape; a lambda that never needs to vary independently of its call site is already at its minimum
necessary complexity.
