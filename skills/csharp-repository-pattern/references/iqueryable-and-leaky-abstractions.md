# IQueryable&lt;T&gt; and Leaky Abstractions

A repository method that returns `IQueryable<T>` hands the caller an unexecuted query expression,
not a materialized result — the caller can keep composing `.Where()`, `.OrderBy()`, `.Include()`,
and similar operators before the query actually runs against storage. This is convenient, and it's
also the single most common way a repository interface stops abstracting anything at all.

## What the leak looks like

```csharp
public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
}

// Consumer code:
var customers = repository.Query()
    .Where(c => c.Region == "West")
    .OrderBy(c => c.Name)
    .Take(20)
    .ToList();
```

This compiles and works, but notice what the consumer is actually doing: composing a query using
whatever query-provider capability sits behind `IQueryable<T>` — capability that varies by provider,
that can silently fall back to in-memory evaluation for expressions a given provider doesn't
support, and that ties the consumer's code to exactly which operators that specific provider
translates well. The repository interface no longer hides the storage technology; it hands the
consumer a live handle into it.

## Why this matters beyond style

- **Provider-specific behavior leaks through.** Two different `IQueryable<T>` providers behind the
  "same" interface can translate an identical LINQ expression differently — or fail to translate it
  at all and silently pull the whole table into memory to evaluate it there. A consumer composing
  arbitrary queries has no way to know which provider it's ultimately running against, or what that
  provider actually supports.
- **A fake implementation can't fully replicate provider behavior.** An in-memory fake for testing
  can offer `IQueryable<T>` easily (`List<T>.AsQueryable()`), but it will happily execute expressions
  a real provider would reject or translate differently — a test built on `IQueryable<T>` queries
  can pass against the fake and still fail against real storage. See
  [testing-with-fake-repositories.md](testing-with-fake-repositories.md) for the consequence this
  has on what a repository test can actually prove.
- **The interface no longer states what queries are actually supported.** `IRepository<T>.Query()`
  says nothing about which filters, sorts, or projections are valid to compose — that information
  only exists in whatever the current provider happens to support, discoverable only by trying it.

## The alternative: named, intention-revealing methods

Replace an open-ended `Query()` with specific methods that state exactly what they retrieve:

```csharp
public interface ICustomerRepository
{
    IReadOnlyList<Customer> GetByRegion(string region);
    IReadOnlyList<Customer> GetTopByRegion(string region, int count);
}
```

Each method returns a materialized, already-executed result (`IReadOnlyList<T>`, not
`IQueryable<T>`), and its name documents exactly what query it runs. A consumer can't compose an
unsupported combination of filters, because there's no query-building surface to compose against —
only the operations the interface actually declares.

## When IQueryable&lt;T&gt; is still the right call

Returning `IQueryable<T>` is defensible in a narrow case: the repository sits at a layer that is
itself explicitly a thin data-access layer (not a domain abstraction meant to hide the storage
technology), and every consumer is trusted to understand the specific provider behind it — for
example, an internal reporting layer built directly against one known provider, where the
composability is the actual point and there's no intent to swap storage technology underneath it.
Outside that case, prefer named methods that return materialized results, and reach for a
specification-based approach (see
[specification-based-queries.md](specification-based-queries.md)) once the number of narrow query
methods starts to grow unmanageably.
