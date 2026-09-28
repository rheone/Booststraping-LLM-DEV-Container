---
name: csharp-repository-pattern
description: Reference for the Repository design pattern in C# — abstracting data access behind an interface so application/domain code depends on a collection-like contract instead of a specific data-access technology. Covers a generic IRepository<T> base (Add/Get/Update/Delete/Query), the generic-repository-versus-specific-repository-per-aggregate debate, IQueryable<T>-returning methods and the leaky-abstraction risk of letting ORM-specific query capability leak through the interface, a specification-based query approach as an alternative to accumulating many narrow repository methods, extending the repository set with a new aggregate's repository without breaking existing ones, and testing code that depends on a repository via an in-memory/fake implementation. Use when deciding how to shape data-access interfaces for domain/application code, reviewing an IRepository<T> for leaky abstractions, choosing between a generic repository and one repository per aggregate, or writing a fake repository for unit tests.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Repository Pattern

The Repository pattern puts a collection-like interface between application/domain code and however
data actually gets stored and retrieved. Code that needs an entity asks the repository for it —
`Get`, `Add`, `Query` — the same way it would pull an item out of an in-memory collection, with no
visibility into what's behind that interface.

## Quick start

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IReadOnlyList<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
}

public sealed class Customer
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public sealed class CustomerRepository : IRepository<Customer>
{
    private readonly List<Customer> _customers = new();

    public Customer? GetById(int id) => _customers.FirstOrDefault(c => c.Id == id);
    public IReadOnlyList<Customer> GetAll() => _customers.AsReadOnly();
    public void Add(Customer entity) => _customers.Add(entity);
    public void Update(Customer entity) { /* replace stored entity */ }
    public void Delete(Customer entity) => _customers.Remove(entity);
}
```

Application code depends on `IRepository<Customer>`, never on `CustomerRepository`'s actual storage
mechanism — a real implementation might back this with an ORM, a document store, or a plain SQL
client, and the consumer's code doesn't change either way.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Learning the pattern's shape and the generic `IRepository<T>` base | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| Deciding between one generic repository and a specific repository per aggregate | [references/generic-vs-specific-repositories.md](references/generic-vs-specific-repositories.md) |
| Reviewing or writing a method that returns `IQueryable<T>` | [references/iqueryable-and-leaky-abstractions.md](references/iqueryable-and-leaky-abstractions.md) |
| Replacing a growing list of narrow query methods with a specification-based approach | [references/specification-based-queries.md](references/specification-based-queries.md) |
| Adding a repository for a new aggregate without touching existing repositories or consumers | [references/extending-with-new-repositories.md](references/extending-with-new-repositories.md) |
| Testing code that depends on a repository, using an in-memory/fake implementation | [references/testing-with-fake-repositories.md](references/testing-with-fake-repositories.md) |

## Out of scope

- Any specific ORM's or data-access library's actual API. The pattern is described generically — an
  `IRepository<T>` implementation can sit on top of whatever data-access technology a given project
  uses, without changing the interface consumers depend on.
- Transaction and unit-of-work coordination across multiple repositories. This skill covers shaping
  a single repository's own interface and query surface, not how multiple repositories' writes are
  grouped into one transaction.
