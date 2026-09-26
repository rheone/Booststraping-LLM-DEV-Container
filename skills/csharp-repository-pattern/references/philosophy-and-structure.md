# Philosophy and Structure

The Repository pattern presents data access as a collection your code reads from and writes to,
rather than as a set of calls to whatever technology actually stores the data. The interface speaks
in domain terms — "get the customer with this ID," "add this order" — never in terms of connections,
commands, or query builders.

## Roles

- **Aggregate/entity type (`T`)** — the domain object the repository manages. A repository is
  scoped to a single aggregate root, not to a database table or a whole schema.
- **Repository interface** — the contract application/domain code depends on. It exposes only the
  operations that code actually needs, in vocabulary the domain understands.
- **Repository implementation** — the class that satisfies the interface using a specific
  data-access technology. This is the only piece of the pattern that knows how data is actually
  stored.

```text
Consumer ---> IRepository<T> (interface) <--- ConcreteRepository ---> actual storage
```

## The generic base

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IReadOnlyList<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
}
```

This shape mirrors a basic collection: something to look one item up by identity, something to
enumerate everything, and the three write operations every mutable collection of entities needs.

## An async form

For any data-access technology that performs I/O, prefer asynchronous members so a repository call
never blocks a thread waiting on storage:

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
```

## Why put this abstraction in front of data access at all

- **Domain/application code stays technology-agnostic.** Business logic reasons about entities and
  operations on them, never about the specifics of how those operations reach storage.
- **The storage technology becomes swappable in one place.** Changing how `Customer` entities are
  persisted means changing `CustomerRepository`'s implementation, not every place in the codebase
  that reads or writes a customer.
- **Tests get a narrow seam to fake.** A test exercising business logic can substitute a repository
  interface implementation with no real storage behind it at all — see
  [testing-with-fake-repositories.md](testing-with-fake-repositories.md).

## Where the abstraction stops paying for itself

A repository interface that just mirrors its implementation's native query capability one-to-one —
every method a thin pass-through with no translation, and the interface only ever satisfied by one
implementation that will never change — adds a layer with nothing underneath it to actually swap.
The pattern earns its cost when the interface expresses genuinely domain-shaped operations distinct
from the storage technology's native shape, when more than one implementation plausibly exists (a
real one and a fake for tests, at minimum), or when isolating domain code from the storage
technology is a real, stated goal for this part of the codebase.
