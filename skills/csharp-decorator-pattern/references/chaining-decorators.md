# Chaining Multiple Decorators

Decorators compose by construction: each one wraps the next, and the outermost one is the only
reference calling code ever holds. This file covers building and ordering a chain deliberately,
since order changes observable behavior.

## Building a chain

```csharp
IReportGenerator generator = new BasicReportGenerator();
generator = new CachingReportGenerator(generator);
generator = new RetryReportGenerator(generator, maxAttempts: 3);
generator = new LoggingReportGenerator(generator, logger);
```

Reading the assembly top to bottom shows the innermost layer first and the outermost layer last —
`LoggingReportGenerator` is what calling code actually holds and calls `Generate` on; a call flows
`Logging → Retry → Caching → Basic` and each layer's own work happens both before and after it
forwards to the next.

## Order changes behavior, not just structure

- **Logging outside retry** (as above) logs once per outer call regardless of how many retry
  attempts happened inside — appropriate when you want one log entry per logical operation.
  **Logging inside retry** (`Retry(Logging(Basic))`) logs once per attempt — appropriate when you
  need visibility into each individual retry.
- **Caching outside retry** (`Caching(Retry(Basic))`) never retries a cache hit, since a hit never
  reaches the retry layer at all. **Caching inside retry** (`Retry(Caching(Basic))`) would retry
  the whole cache-then-fetch operation on failure, which is rarely what you want — a cache lookup
  failing isn't the kind of transient failure retry exists for.
- **Validation should typically be outermost.** A validation decorator that rejects bad input before
  any other layer runs avoids caching a result for invalid input, retrying a call that was never
  going to succeed, or logging a "success" that was actually skipped work.

There is no universally correct order — only an order that matches what each decorator is actually
protecting against. Decide order by asking, for each pair of adjacent layers, "does the outer layer
need to see every attempt the inner layer makes, or only the final outcome?"

## Composing a chain from configuration

When the exact set of decorators active for a given deployment or feature flag varies, build the
chain with a loop or a small builder instead of hand-nesting constructor calls:

```csharp
public static IReportGenerator BuildReportGenerator(
    IReportGenerator core,
    IEnumerable<Func<IReportGenerator, IReportGenerator>> decoratorFactories)
{
    return decoratorFactories.Aggregate(core, (generator, decorate) => decorate(generator));
}
```

```csharp
var decorators = new List<Func<IReportGenerator, IReportGenerator>>
{
    inner => new CachingReportGenerator(inner),
};

if (retryEnabled)
{
    decorators.Add(inner => new RetryReportGenerator(inner, maxAttempts: 3));
}

decorators.Add(inner => new LoggingReportGenerator(inner, logger));

IReportGenerator generator = BuildReportGenerator(new BasicReportGenerator(), decorators);
```

`Aggregate` applies each factory to the result of the previous one, in the list's order — the same
ordering rules above still apply; the list's order *is* the wrapping order, first element innermost.

## A chain is still just one object graph

However many decorators wrap `BasicReportGenerator`, calling code holds exactly one
`IReportGenerator` reference and calls `Generate` exactly once per logical operation — the chain's
existence is entirely invisible to any code that only depends on the interface, which is what lets
decorators be added, removed, or reordered without touching that calling code at all.
