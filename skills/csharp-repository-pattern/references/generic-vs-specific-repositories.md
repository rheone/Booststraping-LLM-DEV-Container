# Generic vs. Specific Repositories

Two shapes compete for the same job: a single generic `IRepository<T>` reused across every
aggregate, or a distinct, hand-written repository interface per aggregate. Both are legitimate; the
right choice depends on how uniform your aggregates' actual access patterns are.

## The generic repository

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IReadOnlyList<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
}

IRepository<Customer> customers = /* ... */;
IRepository<Order> orders = /* ... */;
```

One interface, reused for every aggregate. Adding a new aggregate type needs no new interface at
all — just an implementation of the existing generic one.

**Strengths**: minimal boilerplate per aggregate; consistent CRUD-shaped access everywhere;
infrastructure that operates generically over "a repository of something" (a generic caching
decorator, a generic audit-logging wrapper) has one interface to target.

**Costs**: every aggregate is forced into the same operation set, whether or not it needs all of
them (an append-only aggregate that's never updated still has an `Update` method to either
implement meaninglessly or throw from); aggregate-specific queries (`GetOverdueInvoices`,
`GetCustomersInRegion`) have nowhere to live on the interface itself.

## The specific repository per aggregate

```csharp
public interface ICustomerRepository
{
    Customer? GetById(int id);
    IReadOnlyList<Customer> GetByRegion(string region);
    void Add(Customer customer);
}

public interface IInvoiceRepository
{
    Invoice? GetById(int id);
    IReadOnlyList<Invoice> GetOverdue(DateOnly asOf);
    void Add(Invoice invoice);
    void MarkPaid(Invoice invoice);
}
```

Each interface exposes exactly the operations that aggregate's consumers actually need, named for
what the domain asks of it — `GetOverdue`, `MarkPaid` — rather than forcing every aggregate through
an identical, generic operation set.

**Strengths**: each interface reflects that aggregate's real access patterns and domain vocabulary;
no unused or meaningless members; a consumer's dependency (`ICustomerRepository`) documents exactly
what it does with customer data, rather than the generic "some repository of `Customer`."

**Costs**: more interfaces to declare and keep in sync as aggregates evolve; generic
cross-cutting infrastructure (a caching or logging wrapper meant to apply uniformly) needs either a
distinct implementation per interface or a shared base the specific interfaces build on.

## A hybrid: specific interfaces built on a shared generic base

A common middle ground extends a generic base per aggregate, adding only the operations that
aggregate needs beyond it:

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Add(T entity);
}

public interface IInvoiceRepository : IRepository<Invoice>
{
    IReadOnlyList<Invoice> GetOverdue(DateOnly asOf);
    void MarkPaid(Invoice invoice);
}
```

This keeps the common CRUD-shaped operations from being redeclared on every aggregate's interface,
while still giving each aggregate a place for its own domain-specific operations — without forcing
every aggregate through operations it doesn't need beyond the shared base.

## Deciding

| Signal | Favor |
| --- | --- |
| Most aggregates genuinely share the same CRUD-shaped access pattern, with few or no aggregate-specific queries | A single generic repository |
| Aggregates have meaningfully different access patterns, or several need domain-specific query methods | A specific repository per aggregate, optionally on a shared generic base |
| Generic infrastructure (caching, logging, auditing) needs to wrap "any repository" uniformly | A generic repository, or specific interfaces that all extend a common generic base |
| A consumer's dependency should read as documentation of exactly what it does with an aggregate | A specific repository interface |
