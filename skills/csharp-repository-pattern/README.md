# Repository Pattern

You put a collection-like interface between application/domain code and however data actually gets
stored and retrieved, so callers ask for an entity the same way they'd pull one out of an in-memory
list. It covers a generic `IRepository<T>` base, the generic-versus-per-aggregate question,
`IQueryable<T>` leaky-abstraction risk, and specification-based queries as an alternative to a
growing pile of narrow methods.

## When to reach for it

- You're shaping a data-access interface for domain or application code and want callers insulated
  from the actual storage technology.
- You're reviewing an `IRepository<T>` and suspect it's leaking ORM-specific query capability
  through a method that returns `IQueryable<T>`.
- Your repository interface has accumulated many single-purpose query methods and you're deciding
  whether to replace them with something more composable.
- You need a fake or in-memory repository so a unit test doesn't depend on real storage.

## Using it

This skill is model-invoked: it fires automatically when your prompt matches its situation, such as
designing a data-access interface or writing a fake repository for a test. You can also invoke it
directly as `/csharp-repository-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The pattern's shape and the generic `IRepository<T>` base | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| One generic repository vs. a repository per aggregate | [references/generic-vs-specific-repositories.md](references/generic-vs-specific-repositories.md) |
| `IQueryable<T>`-returning methods and leaky abstractions | [references/iqueryable-and-leaky-abstractions.md](references/iqueryable-and-leaky-abstractions.md) |
| Replacing narrow query methods with specification-based queries | [references/specification-based-queries.md](references/specification-based-queries.md) |
| Adding a repository for a new aggregate | [references/extending-with-new-repositories.md](references/extending-with-new-repositories.md) |
| Testing code that depends on a repository via a fake | [references/testing-with-fake-repositories.md](references/testing-with-fake-repositories.md) |

## Example prompts

- "How should I shape a repository interface for my `Order` aggregate so the domain layer doesn't
  know what's storing it?"
- "Is returning `IQueryable<Customer>` from this repository method a leaky abstraction?"
- "I need a fake `IRepository<Product>` for a unit test: what's the right shape for it?"
